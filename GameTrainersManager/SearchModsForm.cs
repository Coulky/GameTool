using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameTrainersManager
{
    public partial class SearchModsForm : Form
    {
        private static readonly HttpClient DownloadClient = new HttpClient();
        public List<FlingtrainerMod> existingMods = new List<FlingtrainerMod>();
        private List<FlingtrainerMod> searchResults = new List<FlingtrainerMod>();
        private Index? ownerIndex;
        private Label lblStatus;

        public SearchModsForm()
        {
            Logger.WriteLine("========== SearchModsForm 构造函数 ==========");
            InitializeComponent();
            Logger.WriteLine("InitializeComponent 完成");
            InitializeStatusLabel();
            Logger.WriteLine("InitializeStatusLabel 完成");
            
            // 确保事件绑定
            if (listView1 != null)
            {
                listView1.MouseClick += listView1_MouseClick;
                listView1.MouseDown += listView1_MouseDown;
                Logger.WriteLine("MouseClick 事件绑定完成");
                Logger.WriteLine("MouseDown 事件绑定完成");
            }
            else
            {
                Logger.WriteLine("listView1 为 null");
            }
        }

        private void InitializeStatusLabel()
        {
            lblStatus = new Label();
            lblStatus.Location = new Point(14, 250);
            lblStatus.Size = new Size(385, 30);
            lblStatus.Text = "就绪";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblStatus);
            this.ClientSize = new Size(410, 290);
        }

        public void SetOwnerIndex(Index index)
        {
            ownerIndex = index;
            // 订阅下载完成事件
            if (index != null)
            {
                index.DownloadCompleted += OnDownloadCompleted;
            }
        }
        
        // 下载完成事件处理
        private void OnDownloadCompleted(object? sender, FlingtrainerMod mod)
        {
            Logger.WriteLine("========== SearchModsForm 收到下载完成事件 ==========");
            // 延迟一点关闭，确保下载流程完全结束
            Task.Delay(500).ContinueWith(_ =>
            {
                Logger.WriteLine("========== SearchModsForm 开始关闭 ==========");
                this.Close();
                Logger.WriteLine("========== SearchModsForm 已关闭 ==========");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void btnSearch_Click(object? sender, EventArgs e)
        {
            Logger.WriteLine("========== SearchModsForm 搜索按钮点击事件触发 ==========");

            if (txtSearch != null && listView1 != null && btnSearch != null)
            {
                var searchTerm = txtSearch.Text.Trim();
                Logger.WriteLine($"搜索关键词: '{searchTerm}'");

                if (string.IsNullOrEmpty(searchTerm))
                {
                    MessageBox.Show("请输入搜索关键词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                btnSearch.Enabled = false;
                btnSearch.Text = "搜索中...";

                try
                {
                    string searchUrl = ConstructSearchUrl(searchTerm);
                    Logger.WriteLine($"搜索URL: {searchUrl}");

                    var searchResults = SearchModsFromUrlAsync(searchUrl);

                    listView1.Items.Clear();
                    int count = 0;

                    foreach (var mod in searchResults)
                    {
                        var item = new ListViewItem(mod.Game ?? "");
                        item.SubItems.Add("下载");
                        listView1.Items.Add(item);
                        count++;
                    }

                    if (count == 0)
                    {
                        MessageBox.Show("没有找到匹配的修改器", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    // 保存搜索结果供下载使用
                    this.searchResults = searchResults;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"搜索失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logger.WriteLine($"搜索失败: {ex.Message}");
                }
                finally
                {
                    if (btnSearch != null)
                    {
                        btnSearch.Enabled = true;
                        btnSearch.Text = "查找";
                    }
                }
            }
        }

        private string ConstructSearchUrl(string searchTerm)
        {
            string processedTerm = searchTerm.ToLower().Trim();
            string encodedTerm = Uri.EscapeDataString(processedTerm);
            return $"https://flingtrainer.com/?s={encodedTerm}";
        }

        private List<FlingtrainerMod> SearchModsFromUrlAsync(string searchUrl)
        {
            var results = new List<FlingtrainerMod>();

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = client.GetAsync(searchUrl).Result;
                response.EnsureSuccessStatusCode();

                var htmlContent = response.Content.ReadAsStringAsync().Result;

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(htmlContent);

                var contentDiv = doc.DocumentNode.SelectSingleNode("//div[@class='content']");
                if (contentDiv != null)
                {
                    var articleNodes = contentDiv.SelectNodes(".//article[contains(@id, 'post-')]");
                    if (articleNodes != null)
                    {
                        foreach (var article in articleNodes)
                        {
                            var postContentDiv = article.SelectSingleNode(".//div[@class='post-content']");
                            if (postContentDiv != null)
                            {
                                var titleNode = postContentDiv.SelectSingleNode(".//h2[@class='post-title']/a");
                                if (titleNode != null)
                                {
                                    var title = titleNode.InnerText.Trim();
                                    var url = titleNode.GetAttributeValue("href", "");

                                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(url) && url.Contains("/trainer/"))
                                    {
                                        var gameName = title.Replace(" Trainer", "").Replace("Trainer", "").Trim();
                                        results.Add(new FlingtrainerMod
                                        {
                                            Name = title,
                                            Game = gameName,
                                            Url = url
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                if (results.Count == 0)
                {
                    var fallbackNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/trainer/')]");
                    if (fallbackNodes != null)
                    {
                        foreach (var node in fallbackNodes)
                        {
                            var href = node.GetAttributeValue("href", "");
                            var title = node.InnerText.Trim();
                            if (!string.IsNullOrEmpty(href) && href.Contains("/trainer/") && !href.Contains("flingtrainer.com/trainer"))
                            {
                                results.Add(new FlingtrainerMod
                                {
                                    Name = title,
                                    Game = title.Replace(" Trainer", "").Trim(),
                                    Url = href.StartsWith("http") ? href : "https://flingtrainer.com" + href
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"解析搜索结果失败: {ex.Message}");
            }

            return results;
        }

        private void txtSearch_Click(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_MouseClick(object? sender, MouseEventArgs e)
        {
            Logger.WriteLine("========== SearchModsForm 下载按钮点击事件触发 ==========");
            HandleDownloadClick(e);
        }

        private void listView1_MouseDown(object? sender, MouseEventArgs e)
        {
            Logger.WriteLine("========== SearchModsForm 下载按钮 MouseDown 事件触发 ==========");
            HandleDownloadClick(e);
        }

        private void HandleDownloadClick(MouseEventArgs e)
        {
            if (listView1 == null || searchResults.Count == 0)
            {
                Logger.WriteLine("listView1 为 null 或搜索结果为空");
                return;
            }

            var hitTest = listView1.HitTest(e.Location);
            if (hitTest.Item != null)
            {
                Logger.WriteLine($"点击了项: {hitTest.Item.Text}");
                
                // 获取点击的列索引
                int columnIndex = -1;
                int x = 0;
                for (int i = 0; i < listView1.Columns.Count; i++)
                {
                    x += listView1.Columns[i].Width;
                    if (e.X < x)
                    {
                        columnIndex = i;
                        break;
                    }
                }
                
                Logger.WriteLine($"点击的列索引: {columnIndex}");

                // 处理下载按钮点击（第二列）
                if (columnIndex == 1)
                {
                    int modIndex = listView1.Items.IndexOf(hitTest.Item);
                    if (modIndex >= 0 && modIndex < searchResults.Count)
                    {
                        var mod = searchResults[modIndex];
                        Logger.WriteLine($"开始下载: {mod.Game} - {mod.Name}");
                        _ = DownloadModAsync(mod);
                    }
                    else
                    {
                        Logger.WriteLine($"无效的修改器索引: {modIndex}");
                    }
                }
                else
                {
                    Logger.WriteLine($"点击的不是下载列，列索引: {columnIndex}");
                }
            }
            else
            {
                Logger.WriteLine("点击的位置不是有效项");
            }
        }

        private async Task DownloadModAsync(FlingtrainerMod mod)
        {
            if (lblStatus != null)
            {
                lblStatus.Text = $"正在获取下载链接: {mod.Game}...";
            }

            try
            {
                // 使用 Index 中的下载方法
                if (ownerIndex != null)
                {
                    await ownerIndex.DownloadModAsync(mod, lblStatus);
                }
                else
                {
                    if (lblStatus != null)
                    {
                        lblStatus.Text = "无法获取下载链接";
                    }
                    MessageBox.Show("无法连接到主窗口", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"下载失败: {ex.Message}");
                if (lblStatus != null)
                {
                    lblStatus.Text = $"下载失败: {ex.Message}";
                }
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}