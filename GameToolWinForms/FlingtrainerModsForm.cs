using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using HtmlAgilityPack;

namespace GameToolWinForms;

public partial class FlingtrainerModsForm : Form
{
    private List<FlingtrainerMod> flingtrainerMods;
    private List<ModItem> existingMods;
    private Form1 mainForm;

    public FlingtrainerModsForm(List<FlingtrainerMod> mods, List<ModItem> existing, Form1 form)
    {
        InitializeComponent();
        // 不再使用预加载的修改器列表，使用空列表
        flingtrainerMods = new List<FlingtrainerMod>();
        existingMods = existing;
        mainForm = form;
        // 不再预加载列表，等待用户搜索
        Console.WriteLine("修改器库窗口已初始化，等待用户搜索");
    }

    private void InitializeComponent()
    {
        this.label1 = new System.Windows.Forms.Label();
        this.txtSearch = new System.Windows.Forms.TextBox();
        this.btnSearch = new System.Windows.Forms.Button();
        this.listView1 = new System.Windows.Forms.ListView();
        this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
        this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
        this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
        this.btnAddSelected = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(12, 10);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(59, 15);
        this.label1.TabIndex = 1;
        this.label1.Text = "搜索关键词";
        // 
        // txtSearch
        // 
        this.txtSearch.Location = new System.Drawing.Point(80, 7);
        this.txtSearch.Name = "txtSearch";
        this.txtSearch.Size = new System.Drawing.Size(300, 23);
        this.txtSearch.TabIndex = 2;
        // 
        // btnSearch
        // 
        this.btnSearch.Location = new System.Drawing.Point(390, 7);
        this.btnSearch.Name = "btnSearch";
        this.btnSearch.Size = new System.Drawing.Size(75, 23);
        this.btnSearch.TabIndex = 3;
        this.btnSearch.Text = "搜索";
        this.btnSearch.UseVisualStyleBackColor = true;
        this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
        // 
        // listView1
        // 
        this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.columnHeader1,
        this.columnHeader2,
        this.columnHeader3});
        this.listView1.FullRowSelect = true;
        this.listView1.Location = new System.Drawing.Point(12, 40);
        this.listView1.Name = "listView1";
        this.listView1.Size = new System.Drawing.Size(453, 300);
        this.listView1.TabIndex = 4;
        this.listView1.UseCompatibleStateImageBehavior = false;
        this.listView1.View = System.Windows.Forms.View.Details;
        // 
        // columnHeader1
        // 
        this.columnHeader1.Text = "名称";
        this.columnHeader1.Width = 250;
        // 
        // columnHeader2
        // 
        this.columnHeader2.Text = "游戏";
        this.columnHeader2.Width = 150;
        // 
        // columnHeader3
        // 
        this.columnHeader3.Text = "状态";
        this.columnHeader3.Width = 50;
        // 
        // btnAddSelected
        // 
        this.btnAddSelected.Location = new System.Drawing.Point(390, 350);
        this.btnAddSelected.Name = "btnAddSelected";
        this.btnAddSelected.Size = new System.Drawing.Size(75, 23);
        this.btnAddSelected.TabIndex = 5;
        this.btnAddSelected.Text = "添加选中";
        this.btnAddSelected.UseVisualStyleBackColor = true;
        this.btnAddSelected.Click += new System.EventHandler(this.btnAddSelected_Click);
        // 
        // FlingtrainerModsForm
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(477, 385);
        this.Controls.Add(this.btnAddSelected);
        this.Controls.Add(this.listView1);
        this.Controls.Add(this.btnSearch);
        this.Controls.Add(this.txtSearch);
        this.Controls.Add(this.label1);
        this.Name = "FlingtrainerModsForm";
        this.Text = "修改器库";
        this.Load += new System.EventHandler(this.FlingtrainerModsForm_Load);
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    private Label? label1;
    private TextBox? txtSearch;
    private Button? btnSearch;
    private ListView? listView1;
    private ColumnHeader? columnHeader1;
    private ColumnHeader? columnHeader2;
    private ColumnHeader? columnHeader3;
    private Button? btnAddSelected;

    private void FlingtrainerModsForm_Load(object? sender, EventArgs e)
    {
        Debug.WriteLine("修改器库窗口已初始化，等待用户搜索");
    }

    private async void btnSearch_Click(object? sender, EventArgs e)
    {
        Debug.WriteLine("搜索按钮点击事件触发");
        
        if (txtSearch != null && listView1 != null && btnSearch != null)
        {
            Debug.WriteLine("所有控件都已初始化");
            
            var searchTerm = txtSearch.Text.Trim();
            Debug.WriteLine($"搜索关键词: '{searchTerm}'");
            
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("请输入搜索关键词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // 显示搜索中状态
            btnSearch.Enabled = false;
            btnSearch.Text = "搜索中...";
            
            try
            {
                // 构造搜索URL
                string searchUrl = ConstructSearchUrl(searchTerm);
                Debug.WriteLine($"搜索URL: {searchUrl}");
                
                // 通过网络搜索获取修改器列表
                var searchResults = await SearchModsFromUrlAsync(searchUrl);
                
                // 更新列表显示
                listView1.Items.Clear();
                int count = 0;
                
                foreach (var mod in searchResults)
                {
                    var item = new ListViewItem(mod.Name);
                    item.SubItems.Add(mod.Game);
                    // 检查是否已存在
                    var exists = existingMods.Any(m => m.Url == mod.Url);
                    item.SubItems.Add(exists ? "已添加" : "未添加");
                    listView1.Items.Add(item);
                    count++;
                }
                
                if (count == 0)
                {
                    MessageBox.Show("没有找到匹配的修改器", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"找到 {count} 个匹配的修改器", "搜索结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"搜索失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"搜索失败: {ex.Message}");
            }
            finally
            {
                // 恢复按钮状态
                if (btnSearch != null)
                {
                    btnSearch.Enabled = true;
                    btnSearch.Text = "搜索";
                }
            }
        }
        else
        {
            string errorMsg = $"界面控件未正确初始化: txtSearch={txtSearch != null}, listView1={listView1 != null}, btnSearch={btnSearch != null}";
            MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Debug.WriteLine(errorMsg);
        }
    }
    
    private string ConstructSearchUrl(string searchTerm)
    {
        // 处理搜索关键词：转换为小写，URL编码
        string processedTerm = searchTerm
            .ToLower()
            .Trim();
        
        // URL编码关键词
        string encodedTerm = Uri.EscapeDataString(processedTerm);
        
        // 构造搜索URL：使用 ?s=关键词 格式
        return $"https://flingtrainer.com/?s={encodedTerm}";
    }
    
    private async Task<List<FlingtrainerMod>> SearchModsFromUrlAsync(string searchUrl)
    {
        var searchResults = new List<FlingtrainerMod>();
        
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            
            // 设置User-Agent模拟浏览器
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var response = await client.GetAsync(searchUrl);
            response.EnsureSuccessStatusCode();
            
            var htmlContent = await response.Content.ReadAsStringAsync();
            
            // 解析搜索结果页面
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);
            
            // 查找搜索结果区域：<div class="content">
                var contentDiv = doc.DocumentNode.SelectSingleNode("//div[@class='content']");
                if (contentDiv != null)
                {
                    Debug.WriteLine("找到content区域");
                    
                    // 在content区域内查找article标签（包含id="post-数字"）
                    var articleNodes = contentDiv.SelectNodes(".//article[contains(@id, 'post-')]");
                    if (articleNodes != null)
                    {
                        Debug.WriteLine($"找到 {articleNodes.Count} 个article标签");
                        
                        foreach (var article in articleNodes)
                        {
                            // 查找<div class="post-content">
                            var postContentDiv = article.SelectSingleNode(".//div[@class='post-content']");
                            if (postContentDiv != null)
                            {
                                // 查找<h2 class="post-title">里面的a标签
                                var titleNode = postContentDiv.SelectSingleNode(".//h2[@class='post-title']/a");
                                if (titleNode != null)
                                {
                                    var title = titleNode.InnerText.Trim();
                                    var url = titleNode.GetAttributeValue("href", "");
                                    
                                    Debug.WriteLine($"找到修改器: {title} -> {url}");
                                    
                                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(url) && url.Contains("/trainer/"))
                                    {
                                        // 提取游戏名称（去除"Trainer"后缀）
                                        var gameName = title.Replace(" Trainer", "").Replace("Trainer", "").Trim();
                                        
                                        var mod = new FlingtrainerMod
                                        {
                                            Name = title,
                                            Game = gameName,
                                            Url = url
                                        };
                                        
                                        searchResults.Add(mod);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // 如果没有找到特定结构，尝试后备方案
                if (searchResults.Count == 0)
                {
                    Debug.WriteLine("使用后备方案搜索修改器链接");
                    
                    // 后备方案1：查找所有包含/trainer/的链接
                    var linkNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/trainer/')]");
                    if (linkNodes != null)
                    {
                        foreach (var link in linkNodes)
                        {
                            var title = link.InnerText.Trim();
                            var url = link.GetAttributeValue("href", "");
                            
                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(url) && title.Length > 0)
                            {
                                var gameName = title.Replace(" Trainer", "").Replace("Trainer", "").Trim();
                                
                                var mod = new FlingtrainerMod
                                {
                                    Name = title,
                                    Game = gameName,
                                    Url = url
                                };
                                
                                // 去重
                                if (!searchResults.Any(m => m.Url == mod.Url))
                                {
                                    searchResults.Add(mod);
                                }
                            }
                        }
                    }
                }
                
                Debug.WriteLine($"总共找到 {searchResults.Count} 个修改器");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"搜索修改器失败: {ex.Message}");
            throw;
        }
        
        return searchResults;
    }

    private void btnAddSelected_Click(object? sender, EventArgs e)
    {
        if (listView1 != null && listView1.SelectedItems.Count > 0)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                var modName = item.SubItems[0].Text;
                var gameName = item.SubItems[1].Text;
                var status = item.SubItems[2].Text;
                if (status != "已添加")
                {
                    var mod = flingtrainerMods.FirstOrDefault(m => m.Name == modName && m.Game == gameName);
                    if (mod != null && mod.Name != null && mod.Game != null && mod.Url != null)
                    {
                        mainForm.AddModFromLibrary(mod);
                        item.SubItems[2].Text = "已添加";
                    }
                }
            }
            MessageBox.Show("已添加选中的修改器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}