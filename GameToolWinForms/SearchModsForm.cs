using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameToolWinForms
{
    public partial class SearchModsForm : Form
    {
        private static readonly HttpClient DownloadClient = new HttpClient();
        public List<FlingtrainerMod> existingMods = new List<FlingtrainerMod>();
        private Index? ownerIndex;

        public SearchModsForm()
        {
            InitializeComponent();
        }

        public void SetOwnerIndex(Index index)
        {
            ownerIndex = index;
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
                        item.SubItems.Add(mod.Name ?? "");
                        item.SubItems.Add("下载");
                        listView1.Items.Add(item);
                        count++;
                    }

                    if (count == 0)
                    {
                        MessageBox.Show("没有找到匹配的修改器", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        if (ownerIndex != null)
                        {
                            ownerIndex.ShowSearchResultsDialog(searchResults, this);
                        }
                    }
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
    }
}