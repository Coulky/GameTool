using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using GameToolWinForms.Services;
// 明确使用 HtmlAgilityPack 的 HtmlDocument
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GameToolWinForms;

public partial class Form1 : Form
{
    private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    private string? modsDir;
    private readonly string configFile;
    private List<ModItem>? modsList;
    private List<FlingtrainerMod>? allModsList; // 存储所有修改器库
    private const string defaultSource = "https://flingtrainer.com/";
    private PythonService? pythonService;
    private bool isShowingLibrary = false; // 当前是否显示修改器库
    private List<FlingtrainerMod>? filteredModsList; // 过滤后的修改器列表

    // 控件声明
    private System.Windows.Forms.ToolStrip? toolStrip1;

    private System.Windows.Forms.ToolStripButton? ListModsButton;
    private System.Windows.Forms.ToolStripButton? SearchModLibraryButton;
    private System.Windows.Forms.ListView? listView1;
    private System.Windows.Forms.ColumnHeader? columnHeader2;
    private System.Windows.Forms.ColumnHeader? columnHeader5;
    private System.Windows.Forms.ColumnHeader? columnHeader8;
    private System.Windows.Forms.ColumnHeader? columnHeader6;
    private System.Windows.Forms.ColumnHeader? columnHeader7;
    private System.Windows.Forms.StatusStrip? statusStrip1;
    private System.Windows.Forms.ToolStripStatusLabel? ModCountTextBlock;
    private System.Windows.Forms.ToolStripButton? OpenDownloadDirButton;
    private System.Windows.Forms.Panel? searchPanel;
    private System.Windows.Forms.Button? btnSearch;
    private System.Windows.Forms.TextBox? txtSearch;
    private System.Windows.Forms.FlowLayoutPanel? alphabetPanel;
    
    // WebView2控件声明（隐藏使用）
    private WebView2? webView;

    public Form1()
    {
        InitializeComponent();
        // 设置窗体图标
        try
        {
            string iconPath = Path.Combine(baseDir, "logo.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new System.Drawing.Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            // 图标加载失败时不影响程序运行
            Console.WriteLine($"图标加载失败: {ex.Message}");
        }
        
        // 初始化WebView2（隐藏使用）
        InitializeWebView();
        
        // 默认下载路径为 library 目录
        modsDir = Path.Combine(baseDir, "library");
        configFile = Path.Combine(baseDir, "config.json");
        pythonService = new PythonService();
        _setup(); // 调用同步方法
    }
    
    private async void InitializeWebView()
    {
        try
        {
            // 创建WebView2实例
            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false // 隐藏浏览器控件
            };
            
            // 将WebView添加到窗体中（但不可见）
            this.Controls.Add(webView);
            
            // 配置WebView2环境
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(baseDir, "WebView2Cache"), new CoreWebView2EnvironmentOptions
            {
                // 启用JavaScript
                AdditionalBrowserArguments = "--enable-javascript --disable-popup-blocking"
            });
            
            // 初始化WebView2环境
            await webView.EnsureCoreWebView2Async(env);
            
            // 配置WebView2设置
            if (webView.CoreWebView2 != null)
            {
                // 设置User-Agent
                webView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
                
                // 清除缓存，确保每次都是新会话
                try
                {
                    await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清除缓存失败: {ex.Message}");
                }
            }
            
            Console.WriteLine("WebView2初始化成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebView2初始化失败: {ex.Message}");
            MessageBox.Show($"WebView2初始化失败: {ex.Message}\n\n请确保已安装WebView2运行时。\nhttps://developer.microsoft.com/microsoft-edge/webview2/", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void _setup()
    {
        if (!string.IsNullOrEmpty(modsDir) && !Directory.Exists(modsDir))
        {
            Directory.CreateDirectory(modsDir);
        }

        if (!File.Exists(configFile))
        {
            var defaultConfig = new Config
            {
                LastUpdated = DateTime.Now.ToString("o"),
                Mods = new List<ModItem>(),
                DownloadDirectory = modsDir
            };
            File.WriteAllText(configFile, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
            modsList = new List<ModItem>();
            allModsList = new List<FlingtrainerMod>(); // 初始化所有修改器库列表
        }
        else
        {
            _loadConfig();
        }

        // 扫描下载目录中的修改器文件
        ScanModsDirectory();

        UpdateModsListView();
    }

    private void ScanModsDirectory()
    {
        try
        {
            // 获取下载目录中的所有可执行文件
            if (!string.IsNullOrEmpty(modsDir))
            {
                var modFiles = Directory.GetFiles(modsDir, "*.exe");
                
                foreach (var filePath in modFiles)
                {
                    var fileName = Path.GetFileName(filePath);
                    // 检查该文件是否已经在 modsList 中
                    if (modsList != null)
                    {
                        bool exists = modsList.Any(mod => mod != null && (!string.IsNullOrEmpty(mod.FilePath) && mod.FilePath == filePath || !string.IsNullOrEmpty(mod.Name) && mod.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)));
                        
                        if (!exists)
                        {
                            // 创建新的 ModItem
                            var newMod = new ModItem
                            {
                                Id = modsList.Count + 1,
                                Name = fileName,
                                FilePath = filePath,
                                Downloaded = true,
                                DownloadDate = DateTime.Now.ToString("o")
                            };
                            modsList.Add(newMod);
                        }
                    }
                }
                
                // 重新编号
                if (modsList != null)
                {
                    for (int i = 0; i < modsList.Count; i++)
                    {
                        modsList[i].Id = i + 1;
                    }
                    
                    // 保存配置
                    _saveConfig();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"扫描修改器目录失败: {ex.Message}");
        }
    }

    private void _loadConfig()
    {
        try
        {
            var configContent = File.ReadAllText(configFile);
            var config = JsonConvert.DeserializeObject<Config>(configContent);
            if (config != null)
            {
                modsList = config.Mods ?? new List<ModItem>();
                modsDir = config.DownloadDirectory ?? modsDir;
            }
            else
            {
                modsList = new List<ModItem>();
            }
            allModsList = new List<FlingtrainerMod>(); // 初始化所有修改器库列表
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置文件失败: {ex.Message}");
            modsList = new List<ModItem>();
            allModsList = new List<FlingtrainerMod>();
        }
    }

    private void _saveConfig()
    {
        try
        {
            var config = new Config
            {
                LastUpdated = DateTime.Now.ToString("o"),
                Mods = modsList,
                DownloadDirectory = modsDir
            };
            File.WriteAllText(configFile, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置文件失败: {ex.Message}");
        }
    }

    private void UpdateModsListView()
    {
        // 设置我的修改器视图的列
        SetupMyModsColumns();
        
        if (listView1 != null)
        {
            listView1.Items.Clear();

            if (modsList != null)
            {
                foreach (var mod in modsList)
                {
                    if (mod != null)
                    {
                        // 优先显示配置中的游戏名字，如果没有才会显示修改器文件名字
                        string displayName = !string.IsNullOrEmpty(mod.Game) && !string.IsNullOrEmpty(mod.Name) ? $"{mod.Game} - {mod.Name}" : (mod.Name ?? "");
                        var item = new ListViewItem(displayName);
                        if (!string.IsNullOrEmpty(mod.AddedDate))
                        {
                            item.SubItems.Add(mod.AddedDate.Substring(0, 10));
                        }
                        else
                        {
                            item.SubItems.Add("");
                        }
                        item.SubItems.Add("启动");
                        item.SubItems.Add("更新");
                        item.SubItems.Add("删除");
                        listView1.Items.Add(item);
                    }
                }

                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"修改器数量: {modsList.Count}";
                }
            }
        }
    }

    private async Task GetDownloadLinkAndStartDownloadAsync(FlingtrainerMod mod)
    {
        try
        {
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = $"正在获取下载链接: {mod.Name}...";
            }
            
            if (string.IsNullOrEmpty(mod.Url))
            {
                MessageBox.Show($"修改器链接无效: {mod.Name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            var (downloadLink, uploadDate, htmlContent) = await GetDownloadLinkAsync(mod.Url);
            
            if (downloadLink != "未知")
            {
                // 直接开始下载，不需要确认
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"正在下载: {mod.Name}...";
                }
                
                await DownloadFileAsync(mod, downloadLink, uploadDate);
            }
            else
            {
                MessageBox.Show($"无法获取下载链接，请稍后重试。\n\n错误: 下载链接为未知", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = "获取下载链接失败";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取下载链接失败: {ex.Message}");
            MessageBox.Show($"获取下载链接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "获取下载链接失败";
            }
        }
    }
    
    // 静态HttpClientHandler和HttpClient，用于管理Cookie
    private static readonly HttpClientHandler DownloadHandler = new HttpClientHandler
    {
        CookieContainer = new System.Net.CookieContainer(), // 必须开启Cookie存储
        UseCookies = true,
        AllowAutoRedirect = true // 允许自动重定向
    };
    
    private static readonly HttpClient DownloadClient = new HttpClient(DownloadHandler)
    {
        Timeout = TimeSpan.FromMinutes(10)
    };
    
    // 在初始化时设置默认的User-Agent
    static Form1()
    {
        DownloadClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }
    
    private async Task DownloadFileAsync(FlingtrainerMod mod, string downloadLink, string uploadDate)
    {
        try
        {
            if (webView == null || webView.CoreWebView2 == null)
            {
                MessageBox.Show("WebView2未初始化，请重启程序。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // 设置下载完成标志
            var downloadCompleted = new TaskCompletionSource<bool>();
            string? downloadedFilePath = null;
            
            // 确保下载目录存在
            if (!string.IsNullOrEmpty(modsDir))
            {
                if (!Directory.Exists(modsDir))
                {
                    Directory.CreateDirectory(modsDir);
                }
            }
            
            // 添加下载事件处理
            EventHandler<CoreWebView2DownloadStartingEventArgs>? downloadHandler = null;
            downloadHandler = (sender, e) =>
            {
                Console.WriteLine($"下载开始: {e.ResultFilePath}");
                
                // 设置下载路径
                if (!string.IsNullOrEmpty(modsDir))
                {
                    var fileName = Path.GetFileName(e.ResultFilePath);
                    string savePath = Path.Combine(modsDir!, fileName);
                    e.ResultFilePath = savePath;
                    downloadedFilePath = savePath;
                    Console.WriteLine($"文件将保存到: {savePath}");
                }
                
                // 监听下载完成事件
                e.DownloadOperation.StateChanged += (s, args) =>
                {
                    Console.WriteLine($"下载状态: {e.DownloadOperation.State}");
                    if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                    {
                        Console.WriteLine($"下载完成: {e.DownloadOperation.ResultFilePath}");
                        downloadCompleted.SetResult(true);
                        // 移除事件处理
                        if (webView.CoreWebView2 != null)
                        {
                            webView.CoreWebView2.DownloadStarting -= downloadHandler;
                        }
                    }
                    else if (e.DownloadOperation.State == CoreWebView2DownloadState.Interrupted)
                    {
                        Console.WriteLine($"下载中断: {e.DownloadOperation.InterruptReason}");
                        downloadCompleted.SetResult(false);
                        // 移除事件处理
                        if (webView.CoreWebView2 != null)
                        {
                            webView.CoreWebView2.DownloadStarting -= downloadHandler;
                        }
                    }
                };
            };
            webView.CoreWebView2.DownloadStarting += downloadHandler;
            
            // 第一步：访问训练器主页
            if (!string.IsNullOrEmpty(mod.Url))
            {
                Console.WriteLine($"1. 正在访问主页 {mod.Url}...");
                var tcs1 = new TaskCompletionSource<bool>();
                
                // 添加导航完成事件
                EventHandler<CoreWebView2NavigationCompletedEventArgs>? homeNavHandler = null;
                homeNavHandler = (sender, e) =>
                {
                    webView.NavigationCompleted -= homeNavHandler;
                    tcs1.SetResult(e.IsSuccess);
                };
                webView.NavigationCompleted += homeNavHandler;
                
                webView.CoreWebView2.Navigate(mod.Url);
                var success1 = await tcs1.Task;
                
                if (!success1)
                {
                    Console.WriteLine($"访问主页失败: {mod.Url}");
                }
                else
                {
                    Console.WriteLine($"   主页访问成功");
                }
                
                // 等待页面完全加载
                await Task.Delay(5000);
            }
            
            // 第二步：模拟点击下载按钮
            Console.WriteLine($"2. 正在模拟点击下载按钮...");
            
            // 等待页面完全加载后，模拟点击下载按钮
            await Task.Delay(2000);
            
            // 尝试多种点击方式
            var clickScripts = new string[]
            {
                // 方式1：点击包含下载链接的a标签
                "var links = document.querySelectorAll('a[href*=\"download-trainer.php\"]'); if(links.length > 0) links[0].click();",
                
                // 方式2：点击包含下载链接的按钮或div
                "var elements = document.querySelectorAll('[onclick*=\"download-trainer.php\"], .download-btn, .download-button, .btn-download'); if(elements.length > 0) elements[0].click();",
                
                // 方式3：点击包含download关键词的元素
                "var elements = document.querySelectorAll('*'); for(var i=0; i<elements.length; i++) { if(elements[i].textContent && elements[i].textContent.toLowerCase().includes('download')) { elements[i].click(); break; } }"
            };
            
            bool clickSuccess = false;
            foreach (var script in clickScripts)
            {
                try
                {
                    Console.WriteLine($"尝试执行点击脚本: {script.Substring(0, Math.Min(50, script.Length))}...");
                    await webView.CoreWebView2.ExecuteScriptAsync(script);
                    
                    // 等待点击后的反应
                    await Task.Delay(3000);
                    
                    // 检查是否触发了下载
                    if (downloadCompleted.Task.IsCompleted)
                    {
                        clickSuccess = true;
                        Console.WriteLine("点击成功，下载已开始");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"点击脚本执行失败: {ex.Message}");
                }
            }
            
            // 如果模拟点击失败，使用直接导航方式
            if (!clickSuccess)
            {
                Console.WriteLine($"3. 模拟点击失败，使用直接下载链接: {downloadLink}");
                
                // 添加导航完成事件
                EventHandler<CoreWebView2NavigationCompletedEventArgs>? navHandler = null;
                navHandler = (sender, e) =>
                {
                    webView.NavigationCompleted -= navHandler;
                    Console.WriteLine($"导航完成: {e.IsSuccess}");
                };
                webView.NavigationCompleted += navHandler;
                
                // 直接导航到下载链接
                webView.CoreWebView2.Navigate(downloadLink);
            }
            
            // 等待下载完成（最多等待120秒）
            var timeoutTask = Task.Delay(120000);
            var completedTask = await Task.WhenAny(downloadCompleted.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                Console.WriteLine("下载超时");
                throw new Exception("下载超时");
            }
            
            var downloadSuccess = await downloadCompleted.Task;
            if (!downloadSuccess)
            {
                Console.WriteLine("下载失败或被中断");
                throw new Exception("下载失败或被中断");
            }
            
            // 验证文件是否存在且大小大于0
            if (string.IsNullOrEmpty(downloadedFilePath) || !File.Exists(downloadedFilePath) || new FileInfo(downloadedFilePath).Length == 0)
            {
                Console.WriteLine("下载的文件不存在或为空");
                throw new Exception("下载的文件不存在或为空");
            }
            
            Console.WriteLine($"下载完成: {downloadedFilePath}");
            
            // 更新 mod 对象
            mod.DownloadUrl = downloadLink;
            mod.UploadDate = uploadDate;
            mod.IsDownloaded = true;
            
            // 添加到已下载列表
            if (modsList != null)
            {
                var newMod = new ModItem
                {
                    Id = modsList.Count + 1,
                    Name = mod.Name,
                    Url = mod.Url,
                    Game = mod.Game,
                    AddedDate = DateTime.Now.ToString("o"),
                    Downloaded = true,
                    DownloadDate = DateTime.Now.ToString("yyyy-MM-dd")
                };
                modsList.Add(newMod);
                _saveConfig();
                
                UpdateLibraryListView();
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"修改器库: {(allModsList != null ? allModsList.Count : 0)} 个修改器";
                }
                MessageBox.Show($"下载完成: {mod.Name}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载文件失败: {ex.Message}");
            MessageBox.Show($"下载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "下载失败";
            }
        }
    }

    private void UpdateLibraryListView()
    {
        // 设置修改器库视图的列
        SetupLibraryColumns();
        
        if (listView1 != null)
        {
            listView1.Items.Clear();

            // 使用过滤后的列表
            var displayList = (filteredModsList != null && filteredModsList.Count > 0) ? filteredModsList : allModsList;

            if (displayList != null && displayList.Count > 0)
            {
                foreach (var mod in displayList)
                {
                    if (mod != null)
                    {
                        var item = new ListViewItem(mod.Game ?? ""); // 显示游戏名字
                        item.SubItems.Add(mod.IsDownloaded ? "已下载" : "未下载");
                        item.SubItems.Add("下载");
                        listView1.Items.Add(item);
                    }
                }
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"修改器库: {displayList.Count} 个修改器";
                }
            }
            else
            {
                // 没有找到修改器，在列表中显示提示信息
                var item = new ListViewItem("未找到修改器");
                item.SubItems.Add("");
                item.SubItems.Add("");
                listView1.Items.Add(item);
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = "修改器库: 0 个修改器";
                }
            }
        }

    }

    private void SetupMyModsColumns()
    {
        if (listView1 != null)
        {
            listView1.Columns.Clear();
            if (columnHeader2 != null)
            {
                listView1.Columns.Add(columnHeader2); // 名称
                columnHeader2.Text = "名称";
                columnHeader2.Width = 400;
            }
            if (columnHeader5 != null)
            {
                listView1.Columns.Add(columnHeader5); // 添加日期
                columnHeader5.Text = "添加日期";
                columnHeader5.Width = 100;
            }
            if (columnHeader8 != null)
            {
                listView1.Columns.Add(columnHeader8); // 启动
                columnHeader8.Text = "启动";
                columnHeader8.Width = 80;
            }
            if (columnHeader6 != null)
            {
                listView1.Columns.Add(columnHeader6); // 更新
                columnHeader6.Text = "更新";
                columnHeader6.Width = 80;
            }
            if (columnHeader7 != null)
            {
                listView1.Columns.Add(columnHeader7); // 删除
                columnHeader7.Text = "删除";
                columnHeader7.Width = 80;
            }
        }
    }

    private void SetupLibraryColumns()
    {
        if (listView1 != null)
        {
            listView1.Columns.Clear();
            if (columnHeader2 != null)
            {
                listView1.Columns.Add(columnHeader2); // 游戏
                columnHeader2.Text = "游戏";
                columnHeader2.Width = 400;
            }
            if (columnHeader7 != null)
            {
                listView1.Columns.Add(columnHeader7); // 状态
                columnHeader7.Text = "状态";
                columnHeader7.Width = 80;
            }
            if (columnHeader8 != null)
            {
                listView1.Columns.Add(columnHeader8); // 操作
                columnHeader8.Text = "操作";
                columnHeader8.Width = 80;
            }
        }
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        // 表单加载时的初始化操作
        // 添加列表视图的点击事件处理
        if (listView1 != null)
        {
            listView1.MouseClick += ListView1_MouseClick;
        }
        
        // 初始化字母按钮
        InitializeAlphabetButtons();
        
        // 添加搜索按钮的点击事件处理
        if (btnSearch != null)
        {
            btnSearch.Click += BtnSearch_Click;
        }
        
        // 初始化过滤列表
        filteredModsList = new List<FlingtrainerMod>();
    }
    
    private void InitializeAlphabetButtons()
    {
        // 清空字母面板
        if (alphabetPanel != null)
        {
            alphabetPanel.Controls.Clear();
        
            // 添加字母按钮
            for (char c = 'A'; c <= 'Z'; c++)
            {
                Button btn = new Button();
                btn.Text = c.ToString();
                btn.Size = new Size(30, 25);
                btn.Click += AlphabetButton_Click;
                alphabetPanel.Controls.Add(btn);
            }
            
            // 添加"全部"按钮
            Button allBtn = new Button();
            allBtn.Text = "全部";
            allBtn.Size = new Size(60, 25);
            allBtn.Click += AllButton_Click;
            alphabetPanel.Controls.Add(allBtn);
        }
    }
    
    private void AlphabetButton_Click(object? sender, EventArgs e)
    {
        Button? btn = sender as Button;
        if (btn != null && isShowingLibrary)
        {
            string letter = btn.Text;
            FilterModsByLetter(letter);
        }
    }
    
    private void AllButton_Click(object? sender, EventArgs e)
    {
        if (isShowingLibrary)
        {
            filteredModsList = allModsList;
            UpdateLibraryListView();
        }
    }
    
    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        if (txtSearch != null)
        {
            string searchText = txtSearch.Text.Trim().ToLower();
            
            if (isShowingLibrary)
            {
                // 修改器库页面 - 搜索 allModsList
                if (!string.IsNullOrEmpty(searchText) && allModsList != null)
                {
                    filteredModsList = allModsList.Where(mod => 
                        mod.Name != null && mod.Game != null && (mod.Name.ToLower().Contains(searchText) || 
                        mod.Game.ToLower().Contains(searchText))
                    ).ToList();
                }
                else
                {
                    filteredModsList = allModsList;
                }
                UpdateLibraryListView();
            }
            else
            {
                // 我的修改器页面 - 搜索 modsList
                if (listView1 != null)
                {
                    listView1.Items.Clear();
                    
                    if (modsList != null)
                    {
                        var filteredList = modsList.Where(mod => 
                            mod.Name != null && mod.Game != null && (mod.Name.ToLower().Contains(searchText) || 
                            mod.Game.ToLower().Contains(searchText))
                        ).ToList();
                        
                        if (filteredList != null)
                        {
                            foreach (var mod in filteredList)
                            {
                                string displayName = !string.IsNullOrEmpty(mod.Game) && !string.IsNullOrEmpty(mod.Name) ? $"{mod.Game} - {mod.Name}" : (mod.Name ?? "");
                                var item = new ListViewItem(displayName);
                                item.SubItems.Add(mod.AddedDate?.Substring(0, 10) ?? "");
                                item.SubItems.Add("启动");
                                item.SubItems.Add("更新");
                                item.SubItems.Add("删除");
                                listView1.Items.Add(item);
                            }
                            
                            if (ModCountTextBlock != null)
                            {
                                ModCountTextBlock.Text = $"修改器数量: {filteredList.Count}";
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void FilterModsByLetter(string letter)
    {
        if (allModsList != null)
        {
            filteredModsList = allModsList.Where(mod => 
                (mod.Name != null && mod.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) ||
                (mod.Game != null && mod.Game.StartsWith(letter, StringComparison.OrdinalIgnoreCase))
            ).ToList();
            UpdateLibraryListView();
        }
    }

    private void ListView1_MouseClick(object? sender, MouseEventArgs e)
    {
        // 获取点击的位置
        if (listView1 != null)
        {
            var hitTest = listView1.HitTest(e.Location);
            if (hitTest.Item != null)
            {
                // 获取点击的列索引
                int columnIndex = -1;
                int x = 0;
                foreach (ColumnHeader column in listView1.Columns)
                {
                    x += column.Width;
                    if (e.X < x)
                    {
                        columnIndex = listView1.Columns.IndexOf(column);
                        break;
                    }
                }

                // 根据当前视图模式处理不同的按钮点击
                if (isShowingLibrary)
                {
                    // 修改器库视图 - 处理下载按钮点击
                    if (columnIndex == 2) // 下载按钮列 (索引2是第3列)
                    {
                        int modIndex = listView1.Items.IndexOf(hitTest.Item);
                        // 使用过滤后的列表
                        var displayList = filteredModsList != null && filteredModsList.Count > 0 ? filteredModsList : allModsList;
                        if (displayList != null && modIndex >= 0 && modIndex < displayList.Count)
                        {
                            var mod = displayList[modIndex];
                            if (!mod.IsDownloaded)
                            {
                                // 先获取真实的下载链接
                                _ = GetDownloadLinkAndStartDownloadAsync(mod);
                            }
                            else
                            {
                                MessageBox.Show("该修改器已经下载过了", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                else
                {
                    // 我的修改器视图 - 处理启动、更新、删除按钮点击
                    // 处理启动按钮点击
                    if (columnIndex == 2) // 启动按钮列
                        {
                            int modIndex = listView1.Items.IndexOf(hitTest.Item);
                            if (modsList != null && modIndex >= 0 && modIndex < modsList.Count)
                            {
                                var mod = modsList[modIndex];
                                if (mod.Downloaded && !string.IsNullOrEmpty(mod.FilePath))
                                {
                                    try
                                    {
                                        // 记录原始路径
                                        string originalPath = mod.FilePath;
                                        Console.WriteLine($"原始路径: {originalPath}");
                                        
                                        // 确保文件路径是有效的 - 使用 Path.Combine 重新构建路径
                                        string fileName = Path.GetFileName(originalPath);
                                        string sanitizedPath = originalPath;
                                        
                                        if (modsDir != null)
                                        {
                                            sanitizedPath = Path.Combine(modsDir, fileName);
                                            
                                            // 如果原始路径不同，尝试使用原始路径
                                            if (File.Exists(originalPath))
                                            {
                                                sanitizedPath = originalPath;
                                            }
                                            
                                            // 规范化路径
                                            sanitizedPath = Path.GetFullPath(sanitizedPath);
                                            Console.WriteLine($"规范化路径: {sanitizedPath}");
                                            
                                            // 验证文件是否存在
                                            if (File.Exists(sanitizedPath))
                                            {
                                                // 以管理员权限启动修改器
                                                var processInfo = new System.Diagnostics.ProcessStartInfo(sanitizedPath);
                                                processInfo.Verb = "runas"; // 以管理员权限运行
                                                processInfo.WorkingDirectory = Path.GetDirectoryName(sanitizedPath);
                                                
                                                try
                                                {
                                                    System.Diagnostics.Process.Start(processInfo);
                                                    MessageBox.Show($"已启动修改器: {mod.Name}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                }
                                                catch (System.ComponentModel.Win32Exception ex)
                                                {
                                                    // 用户取消了管理员权限请求
                                                    if (ex.NativeErrorCode == 1223)
                                                    {
                                                        MessageBox.Show("您取消了管理员权限请求，修改器可能无法正常运行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"启动修改器失败: {ex.Message}");
                                                        MessageBox.Show($"启动修改器失败: {ex.Message}\n路径: {sanitizedPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                MessageBox.Show($"修改器文件不存在: {sanitizedPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show($"修改器文件不存在: {sanitizedPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"启动修改器失败: {ex.Message}");
                                        MessageBox.Show($"启动修改器失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("修改器文件不存在，无法启动", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                    // 处理更新按钮点击
                    else if (columnIndex == 3) // 更新按钮列
                    {
                        int modIndex = listView1.Items.IndexOf(hitTest.Item);
                        if (modsList != null && modIndex >= 0 && modIndex < modsList.Count)
                        {
                            var mod = modsList[modIndex];
                            // 这里可以添加更新修改器的逻辑
                            MessageBox.Show($"更新修改器: {mod.Name}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    // 处理删除按钮点击
                    else if (columnIndex == 4) // 删除按钮列
                    {
                        int modIndex = listView1.Items.IndexOf(hitTest.Item);
                        if (modsList != null && modIndex >= 0 && modIndex < modsList.Count)
                        {
                            var mod = modsList[modIndex];
                            // 确认删除
                            if (MessageBox.Show($"确定要删除修改器 '{mod.Name}' 吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                // 删除文件
                                if (mod.Downloaded && !string.IsNullOrEmpty(mod.FilePath) && File.Exists(mod.FilePath))
                                {
                                    try
                                    {
                                        File.Delete(mod.FilePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"删除文件失败: {ex.Message}");
                                    }
                                }
                                // 从列表中移除
                                modsList.Remove(mod);
                                // 重新编号
                                for (int i = 0; i < modsList.Count; i++)
                                {
                                    modsList[i].Id = i + 1;
                                }
                                // 保存配置
                                _saveConfig();
                                // 更新列表
                                UpdateModsListView();
                                MessageBox.Show("修改器已删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
        }
    }

    private void ListModsButton_Click(object? sender, EventArgs e)
    {
        // 切换回我的修改器视图
        isShowingLibrary = false;
        if (ListModsButton != null)
        {
            ListModsButton.Checked = true;
        }
        if (SearchModLibraryButton != null)
        {
            SearchModLibraryButton.Checked = false;
        }
        
        // 显示我的修改器列表
        UpdateModsListView();
    }

    private async void SearchModLibraryButton_Click(object? sender, EventArgs e)
    {
        // 切换到修改器库视图
        isShowingLibrary = true;
        if (ListModsButton != null)
        {
            ListModsButton.Checked = false;
        }
        if (SearchModLibraryButton != null)
        {
            SearchModLibraryButton.Checked = true;
        }
        
        // 显示加载状态
        if (ModCountTextBlock != null)
        {
            ModCountTextBlock.Text = "正在加载修改器库...";
        }
        if (btnSearch != null)
        {
            btnSearch.Enabled = false;
        }
        
        // 清空当前列表，显示加载提示
        if (listView1 != null)
        {
            listView1.Items.Clear();
            var loadingItem = new ListViewItem("正在加载修改器库，请稍候...");
            loadingItem.SubItems.Add("");
            loadingItem.SubItems.Add("");
            listView1.Items.Add(loadingItem);
        }
        
        // 获取修改器库
        await GetFlingtrainerModsAsync();
        
        // 恢复按钮状态
        if (btnSearch != null)
        {
            btnSearch.Enabled = true;
        }
        
        // 初始化过滤列表
        filteredModsList = allModsList;
        
        // 在当前列表中显示修改器库
        UpdateLibraryListView();
    }

    private void OpenDownloadDirButton_Click(object sender, EventArgs e)
    {
        // 打开修改器所在的文件夹
        if (Directory.Exists(modsDir))
        {
            System.Diagnostics.Process.Start("explorer.exe", modsDir);
        }
        else
        {
            MessageBox.Show("修改器文件夹不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async Task DownloadModAsync(ModItem mod)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var response = await client.GetAsync(mod.Url);
            response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(mod.Url);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"{mod.Name}.exe";
            }

            if (!string.IsNullOrEmpty(modsDir))
            {
                var filePath = Path.Combine(modsDir, fileName);
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create);
                await stream.CopyToAsync(fileStream);

                mod.Downloaded = true;
                mod.FilePath = filePath;
                mod.DownloadDate = DateTime.Now.ToString("o");
                _saveConfig();
                UpdateModsListView();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败: {ex.Message}");
        }
    }

    private async Task GetFlingtrainerModsAsync()
    {
        try
        {
            // 暂时注释掉all-trainers接口调用
            // allModsList = await GetModsFromFlingtrainerCSharpAsync();
            
            // 使用空列表，等待查询时动态搜索
            allModsList = new List<FlingtrainerMod>();
            Console.WriteLine("已禁用all-trainers接口，将使用动态搜索功能");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取修改器库失败: {ex.Message}");
            // 发生错误时，将 allModsList 设为空列表
            allModsList = new List<FlingtrainerMod>();
        }
    }

    private async Task<List<FlingtrainerMod>> GetModsFromFlingtrainerCSharpAsync()
    {
        var mods = new List<FlingtrainerMod>();
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var response = await client.GetAsync("https://flingtrainer.com/all-trainers/");
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 先找到 class="items-outer" 的 div
            var itemsOuter = doc.DocumentNode.SelectSingleNode("//div[@class='items-outer']");
            if (itemsOuter != null)
            {
                // 只在这个div内部查找 li 标签下的 a 标签
                var liTags = itemsOuter.SelectNodes(".//li/a[contains(@href, '/trainer/')]");
                if (liTags != null)
                {
                    int count = 0;
                    foreach (var link in liTags)
                    {
                        var href = link.GetAttributeValue("href", "");
                        // 确保href是有效的修改器链接
                        if (!string.IsNullOrEmpty(href) && href.Contains("/trainer/"))
                        {
                            // 构建完整的URL
                            var url = href.StartsWith("http") ? href : "https://flingtrainer.com" + href;
                            
                            // 获取游戏名称 - 从链接文本中提取
                            var name = link.InnerText.Trim();
                            
                            // 清理名称 - 移除可能存在的多余字符
                            name = name.Replace("\n", " ").Replace("\r", "").Replace("\t", " ");
                            // 移除多余的空格
                            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
                            
                            // 移除 "Trainer" 后缀作为游戏名称
                            var gameName = name.Replace("Trainer", "").Trim();
                            
                            if (!string.IsNullOrEmpty(name) && name.Length > 2 && name != "Trainer")
                            {
                                var mod = new FlingtrainerMod
                                {
                                    Name = name,
                                    Url = url,
                                    Game = gameName
                                };
                                
                                mods.Add(mod);
                                
                                // 每处理1个修改器更新一次状态
                                count++;
                                if (ModCountTextBlock != null)
                                {
                                    ModCountTextBlock.Text = $"正在加载修改器库... ({count})";
                                }
                                Application.DoEvents(); // 刷新UI
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("未找到 class='items-outer' 的 div");
            }
            
            Console.WriteLine($"成功解析到 {mods.Count} 个修改器");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"C# 实现获取修改器失败: {ex.Message}");
        }
        return mods;
    }
    
    private async Task<string> GetArticlePublishDateAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 查找文章发布时间 - 优先从meta标签获取
            var publishDate = "";
            
            // 方式1：从meta标签获取article:published_time
            var metaPublishedTime = doc.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']");
            if (metaPublishedTime != null)
            {
                var datetime = metaPublishedTime.GetAttributeValue("content", "");
                if (!string.IsNullOrEmpty(datetime))
                {
                    // 解析ISO格式的时间，如 "2020-05-02T22:40:54+00:00"
                    if (DateTime.TryParse(datetime, out var dateTime))
                    {
                        publishDate = dateTime.ToString("yyyy-MM");
                        Console.WriteLine($"从meta标签获取到发布时间: {publishDate} (原始值: {datetime})");
                        return publishDate;
                    }
                }
            }
            
            // 方式2：从meta标签获取og:published_time
            var metaOgPublishedTime = doc.DocumentNode.SelectSingleNode("//meta[@property='og:published_time']");
            if (metaOgPublishedTime != null)
            {
                var datetime = metaOgPublishedTime.GetAttributeValue("content", "");
                if (!string.IsNullOrEmpty(datetime))
                {
                    if (DateTime.TryParse(datetime, out var dateTime))
                    {
                        publishDate = dateTime.ToString("yyyy-MM");
                        Console.WriteLine($"从og:published_time获取到发布时间: {publishDate} (原始值: {datetime})");
                        return publishDate;
                    }
                }
            }
            
            // 方式3：从meta标签获取其他可能的日期属性
            var dateMetaTags = doc.DocumentNode.SelectNodes("//meta[contains(@property, 'date') or contains(@property, 'time') or contains(@name, 'date') or contains(@name, 'time')]");
            if (dateMetaTags != null)
            {
                foreach (var metaTag in dateMetaTags)
                {
                    var content = metaTag.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(content) && content.Contains("202"))
                    {
                        if (DateTime.TryParse(content, out var dateTime))
                        {
                            publishDate = dateTime.ToString("yyyy-MM");
                            Console.WriteLine($"从meta标签获取到发布时间: {publishDate} (属性: {metaTag.GetAttributeValue("property", metaTag.GetAttributeValue("name", "unknown"))}, 原始值: {content})");
                            return publishDate;
                        }
                    }
                }
            }
            
            // 方式4：查找 <time> 标签
            var timeElement = doc.DocumentNode.SelectSingleNode("//time[@datetime]");
            if (timeElement != null)
            {
                var datetime = timeElement.GetAttributeValue("datetime", "");
                if (!string.IsNullOrEmpty(datetime))
                {
                    if (DateTime.TryParse(datetime, out var dateTime))
                    {
                        publishDate = dateTime.ToString("yyyy-MM");
                        Console.WriteLine($"从time标签获取到发布时间: {publishDate} (原始值: {datetime})");
                        return publishDate;
                    }
                }
            }
            
            Console.WriteLine("无法从meta标签获取文章发布时间，使用当前时间");
            return DateTime.Now.ToString("yyyy-MM");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取文章发布时间失败: {ex.Message}");
            return DateTime.Now.ToString("yyyy-MM");
        }
    }
    
    private async Task<(string downloadLink, string uploadDate, string htmlContent)> GetDownloadLinkAsync(string url)
    {
        try
        {
            // 使用静态的DownloadClient，确保Cookie共享
            var response = await DownloadClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            Console.WriteLine($"开始解析下载页面: {url}");
            
            // 查找 class="download-attachments style-table" 的 div
            var downloadDiv = doc.DocumentNode.SelectSingleNode("//div[@class='download-attachments style-table']");
            Console.WriteLine($"找到 download-attachments: {downloadDiv != null}");
            
            if (downloadDiv != null)
            {
                // 在这个div内部查找 class="da-attachments-table" 的 table
                var tableDiv = downloadDiv.SelectSingleNode(".//table[@class='da-attachments-table']");
                Console.WriteLine($"找到 da-attachments-table: {tableDiv != null}");
                
                if (tableDiv != null)
                {
                    // 先查找 tbody
                    var tbody = tableDiv.SelectSingleNode(".//tbody");
                    Console.WriteLine($"找到 tbody: {tbody != null}");
                    
                    if (tbody != null)
                    {
                        // 首先查找 class="exe autoupdate" 的 tr
                        var exeDiv = tbody.SelectSingleNode(".//tr[@class='exe autoupdate']");
                        Console.WriteLine($"找到 exe autoupdate: {exeDiv != null}");
                        
                        // 如果不存在 exe autoupdate，查找 class="zip" 的 tr 的第一个
                        if (exeDiv == null)
                        {
                            exeDiv = tbody.SelectSingleNode(".//tr[@class='zip'][1]");
                            Console.WriteLine($"找到第一个 zip tr: {exeDiv != null}");
                        }
                        
                        // 如果仍然不存在，尝试查找任何 tr
                        if (exeDiv == null)
                        {
                            exeDiv = tbody.SelectSingleNode(".//tr[1]");
                            Console.WriteLine($"找到第一个 tr: {exeDiv != null}");
                        }
                        
                        if (exeDiv != null)
                        {
                            // 获取 class="attachment-title" 里面的下载地址 - 直接选择子td
                            var titleDiv = exeDiv.SelectSingleNode("./td[@class='attachment-title']");
                            var dateDiv = exeDiv.SelectSingleNode("./td[@class='attachment-date']");
                            Console.WriteLine($"找到 attachment-title: {titleDiv != null}");
                            Console.WriteLine($"找到 attachment-date: {dateDiv != null}");
                    
                        if (titleDiv != null)
                        {
                            // 查找 titleDiv 中的 a 标签，获取下载链接 - 直接选择子a标签
                            var downloadLink = "";
                            var aTag = titleDiv.SelectSingleNode("./a");
                            Console.WriteLine($"找到 a 标签: {aTag != null}");
                            
                            // 新方法：直接构建下载链接（最稳定）
                            if (aTag != null)
                            {
                                // 从a标签的title属性获取文件名
                                string fileName = aTag.GetAttributeValue("title", "");
                                Console.WriteLine($"获取到文件名: {fileName}");
                                
                                // 从td标签获取日期
                                string rawDate = dateDiv != null ? dateDiv.InnerText.Trim() : "";
                                Console.WriteLine($"获取到原始日期: {rawDate}");
                                
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    try
                                    {
                                        // 获取文章发布时间
                                        string articlePublishDate = await GetArticlePublishDateAsync(url);
                                        Console.WriteLine($"获取到文章发布时间: {articlePublishDate}");
                                        
                                        // 解析日期获取年份和月份
                                        string[] parts = articlePublishDate.Split('-');
                                        if (parts.Length >= 2)
                                        {
                                            string year = parts[0]; // "2021"
                                            string month = parts[1]; // "10"
                                            
                                            // 拼接path参数 - 添加WordPress默认上传路径前缀和.zip后缀
                                            string pathParam = $"/wp-content/uploads/{year}/{month}/{fileName}.zip";
                                            
                                            // 构建最终URL
                                            string finalPath = System.Web.HttpUtility.UrlEncode(pathParam);
                                            // 将小写的%xx转换为大写的%XX
                                            finalPath = System.Text.RegularExpressions.Regex.Replace(finalPath, @"%([0-9a-f]{2})", m => $"%{m.Groups[1].Value.ToUpper()}");
                                            downloadLink = $"https://flingtrainer.com/download-trainer.php?path={finalPath}";
                                            Console.WriteLine($"构建的真实下载链接: {downloadLink}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"构建下载链接失败: {ex.Message}");
                                    }
                                }
                            }
                            
                            // 后备方法1：从a标签获取跳转链接
                            if (string.IsNullOrEmpty(downloadLink) && aTag != null)
                            {
                                var redirectLink = aTag.GetAttributeValue("href", "");
                                Console.WriteLine($"获取到跳转链接: {redirectLink}");
                                downloadLink = redirectLink;
                            }
                            
                            // 后备方法2：直接在网页源码中搜索真实的PHP下载链接
                            if (string.IsNullOrEmpty(downloadLink))
                            {
                                try
                                {
                                    // 使用正则表达式搜索真实的PHP下载链接
                                    string pattern = @"(https?://flingtrainer\.com/download-trainer\.php\?path=[^""')\s]+)";
                                    var match = System.Text.RegularExpressions.Regex.Match(htmlContent, pattern);
                                    
                                    if (match.Success)
                                    {
                                        downloadLink = match.Groups[1].Value;
                                        // 清理链接中的转义字符
                                        downloadLink = downloadLink.Replace("&amp;", "&");
                                        Console.WriteLine($"从源码中找到真实下载链接: {downloadLink}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"搜索真实下载链接失败: {ex.Message}");
                                }
                            }
                            
                            var uploadDate = dateDiv != null ? dateDiv.InnerText.Trim() : DateTime.Now.ToString("yyyy-MM-dd");
                            Console.WriteLine($"获取到上传时间: {uploadDate}");
                            
                            if (!string.IsNullOrEmpty(downloadLink))
                            {
                                return (downloadLink, uploadDate, htmlContent);
                            }
                        }
                    }
                }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取下载链接失败: {ex.Message}");
        }
        return ("未知", "未知", "");
    }

    private async void DownloadModAsync(FlingtrainerMod mod)
    {
        try
        {
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = $"正在获取下载链接: {mod.Name}...";
            }
            
            // 使用静态的DownloadClient，确保Cookie共享
            var response = await DownloadClient.GetAsync(mod.Url);
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            Console.WriteLine($"开始解析页面: {mod.Url}");
            
            // 查找 class="download-attachments style-table" 的 div
            var downloadDiv = doc.DocumentNode.SelectSingleNode("//div[@class='download-attachments style-table']");
            Console.WriteLine($"找到 download-attachments: {downloadDiv != null}");
            
            if (downloadDiv != null)
            {
                // 在这个div内部查找 class="da-attachments-table" 的 table
                var tableDiv = downloadDiv.SelectSingleNode(".//table[@class='da-attachments-table']");
                Console.WriteLine($"找到 da-attachments-table: {tableDiv != null}");
                
                if (tableDiv != null)
                {
                    // 先查找 tbody
                    var tbody = tableDiv.SelectSingleNode(".//tbody");
                    Console.WriteLine($"找到 tbody: {tbody != null}");
                    
                    if (tbody != null)
                    {
                        // 首先查找 class="exe autoupdate" 的 tr
                        var exeDiv = tbody.SelectSingleNode(".//tr[@class='exe autoupdate']");
                        Console.WriteLine($"找到 exe autoupdate: {exeDiv != null}");
                        
                        // 如果不存在 exe autoupdate，查找 class="zip" 的 tr 的第一个
                        if (exeDiv == null)
                        {
                            exeDiv = tbody.SelectSingleNode(".//tr[@class='zip'][1]");
                            Console.WriteLine($"找到第一个 zip tr: {exeDiv != null}");
                        }
                        
                        // 如果仍然不存在，尝试查找任何 tr
                        if (exeDiv == null)
                        {
                            exeDiv = tbody.SelectSingleNode(".//tr[1]");
                            Console.WriteLine($"找到第一个 tr: {exeDiv != null}");
                        }
                        
                        if (exeDiv != null)
                        {
                            // 获取 class="attachment-title" 里面的下载地址 - 直接选择子td
                                var titleDiv = exeDiv.SelectSingleNode("./td[@class='attachment-title']");
                                var dateDiv = exeDiv.SelectSingleNode("./td[@class='attachment-date']");
                                Console.WriteLine($"找到 attachment-title: {titleDiv != null}");
                            Console.WriteLine($"找到 attachment-date: {dateDiv != null}");
                        
                        if (titleDiv != null)
                        {
                            // 查找 titleDiv 中的 a 标签，获取下载链接 - 直接选择子a标签
                            var downloadLink = "";
                            var aTag = titleDiv.SelectSingleNode("./a");
                            Console.WriteLine($"找到 a 标签: {aTag != null}");
                            
                            // 新方法：直接构建下载链接（最稳定）
                            if (aTag != null)
                            {
                                // 从a标签的title属性获取文件名
                                string fileName = aTag.GetAttributeValue("title", "");
                                Console.WriteLine($"获取到文件名: {fileName}");
                                
                                // 从td标签获取日期
                                string rawDate = dateDiv != null ? dateDiv.InnerText.Trim() : "";
                                Console.WriteLine($"获取到原始日期: {rawDate}");
                                
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    try
                                    {
                                        // 获取文章发布时间
                                        string articlePublishDate = await GetArticlePublishDateAsync(mod.Url);
                                        Console.WriteLine($"获取到文章发布时间: {articlePublishDate}");
                                        
                                        // 解析日期获取年份和月份
                                        string[] parts = articlePublishDate.Split('-');
                                        if (parts.Length >= 2)
                                        {
                                            string year = parts[0]; // "2021"
                                            string month = parts[1]; // "10"
                                            
                                            // 拼接path参数 - 添加WordPress默认上传路径前缀和.zip后缀
                                            string pathParam = $"/wp-content/uploads/{year}/{month}/{fileName}.zip";
                                            
                                            // 构建最终URL
                                            string finalPath = System.Web.HttpUtility.UrlEncode(pathParam);
                                            // 将小写的%xx转换为大写的%XX
                                            finalPath = System.Text.RegularExpressions.Regex.Replace(finalPath, @"%([0-9a-f]{2})", m => $"%{m.Groups[1].Value.ToUpper()}");
                                            downloadLink = $"https://flingtrainer.com/download-trainer.php?path={finalPath}";
                                            Console.WriteLine($"构建的真实下载链接: {downloadLink}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"构建下载链接失败: {ex.Message}");
                                    }
                                }
                            }
                            
                            // 后备方法1：从a标签获取跳转链接
                            if (string.IsNullOrEmpty(downloadLink) && aTag != null)
                            {
                                var redirectLink = aTag.GetAttributeValue("href", "");
                                Console.WriteLine($"获取到跳转链接: {redirectLink}");
                                downloadLink = redirectLink;
                            }
                            
                            // 后备方法2：直接在网页源码中搜索真实的PHP下载链接
                            if (string.IsNullOrEmpty(downloadLink))
                            {
                                try
                                {
                                    // 使用正则表达式搜索真实的PHP下载链接
                                    string pattern = @"(https?://flingtrainer\.com/download-trainer\.php\?path=[^""')\s]+)";
                                    var match = System.Text.RegularExpressions.Regex.Match(htmlContent, pattern);
                                    
                                    if (match.Success)
                                    {
                                        downloadLink = match.Groups[1].Value;
                                        // 清理链接中的转义字符
                                        downloadLink = downloadLink.Replace("&amp;", "&");
                                        Console.WriteLine($"从源码中找到真实下载链接: {downloadLink}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"搜索真实下载链接失败: {ex.Message}");
                                }
                            }
                            
                            var uploadDate = dateDiv != null ? dateDiv.InnerText.Trim() : DateTime.Now.ToString("yyyy-MM-dd");
                            Console.WriteLine($"获取到上传时间: {uploadDate}");
                            
                            // 显示下载链接和更新时间
                            MessageBox.Show($"下载链接: {downloadLink}\n更新时间: {uploadDate}", "下载信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                            Console.WriteLine($"下载链接: {downloadLink}");
                            Console.WriteLine($"更新时间: {uploadDate}");
                            
                            // 下载文件
                            if (!string.IsNullOrEmpty(downloadLink))
                            {
                                try
                                {
                                    // 从URL中提取文件名（从path参数）
                                    var uri = new Uri(downloadLink);
                                    var fileName = "Unknown.zip";
                                    
                                    // 手动解析查询参数
                                    var queryString = uri.Query;
                                    if (!string.IsNullOrEmpty(queryString))
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(queryString, @"path=([^&]+)");
                                        if (match.Success)
                                        {
                                            var pathValue = match.Groups[1].Value;
                                            // URL解码
                                            pathValue = Uri.UnescapeDataString(pathValue);
                                            fileName = Path.GetFileName(pathValue);
                                        }
                                    }
                                    
                                    Console.WriteLine($"提取的文件名: {fileName}");
                                    
                                    using (var downloadClient = new HttpClient())
                                    {
                                        downloadClient.Timeout = TimeSpan.FromMinutes(10);
                                        var downloadResponse = await downloadClient.GetAsync(downloadLink);
                                        downloadResponse.EnsureSuccessStatusCode();
                                        
                                        if (!string.IsNullOrEmpty(modsDir) && !string.IsNullOrEmpty(fileName))
                                        {
                                            var filePath = Path.Combine(modsDir, fileName);
                                        
                                            // 下载文件
                                            using (var stream = await downloadResponse.Content.ReadAsStreamAsync())
                                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                                            {
                                                await stream.CopyToAsync(fileStream);
                                            }
                                            
                                            // 检查是否为 zip 文件
                                            if (Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                                            {
                                                // 自动解压
                                                var extractPath = Path.Combine(modsDir, Path.GetFileNameWithoutExtension(fileName));
                                                if (!Directory.Exists(extractPath))
                                                {
                                                    Directory.CreateDirectory(extractPath);
                                                }
                                                
                                                // 解压文件
                                                System.IO.Compression.ZipFile.ExtractToDirectory(filePath, extractPath);
                                                
                                                // 删除压缩包
                                                File.Delete(filePath);
                                                
                                                Console.WriteLine($"已解压并删除压缩包: {fileName}");
                                            }
                                            
                                            // 更新 mod 对象
                                            mod.DownloadUrl = downloadLink;
                                            mod.UploadDate = uploadDate;
                                            mod.IsDownloaded = true;
                                            
                                            // 添加到已下载列表
                                            if (modsList != null)
                                            {
                                                var newMod = new ModItem
                                                {
                                                    Id = modsList.Count + 1,
                                                    Name = mod.Name,
                                                    Url = mod.Url,
                                                    Game = mod.Game,
                                                    AddedDate = DateTime.Now.ToString("o"),
                                                    Downloaded = true,
                                                    DownloadDate = DateTime.Now.ToString("yyyy-MM-dd")
                                                };
                                                modsList.Add(newMod);
                                                _saveConfig();
                                                
                                                UpdateLibraryListView();
                                                if (ModCountTextBlock != null)
                                                {
                                                    ModCountTextBlock.Text = $"修改器库: {(filteredModsList != null && filteredModsList.Count > 0 ? filteredModsList.Count : (allModsList != null ? allModsList.Count : 0))} 个修改器";
                                                }
                                                MessageBox.Show($"下载完成: {mod.Name}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"下载文件失败: {ex.Message}");
                                    MessageBox.Show($"下载文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                Console.WriteLine("下载链接为空");
                                MessageBox.Show($"下载链接为空: {mod.Name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            Console.WriteLine("未找到 attachment-title 元素");
                            MessageBox.Show($"未能找到下载链接: {mod.Name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    }
                    else
                    {
                        Console.WriteLine("未找到 exe autoupdate 或 zip alt 元素");
                        MessageBox.Show($"未能找到下载链接: {mod.Name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    Console.WriteLine("未找到 da-attachments-table 元素");
                    MessageBox.Show($"未能找到下载链接: {mod.Name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Console.WriteLine("未找到下载链接容器");
                MessageBox.Show($"未能找到下载链接: {mod.Name}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败: {ex.Message}");
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "下载失败";
            }
            MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void AddModFromLibrary(FlingtrainerMod mod)
    {
        if (modsList != null)
        {
            var newMod = new ModItem
            {
                Id = modsList.Count + 1,
                Name = mod.Name,
                Url = mod.Url,
                Game = mod.Game,
                AddedDate = DateTime.Now.ToString("o"),
                Downloaded = false
            };
            modsList.Add(newMod);
            _saveConfig();
            UpdateModsListView();
        }
    }
}

public class Config
{
    public string? LastUpdated { get; set; }
    public List<ModItem>? Mods { get; set; }
    public string? DownloadDirectory { get; set; }
}

public class ModItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Game { get; set; }
    public string? AddedDate { get; set; }
    public bool Downloaded { get; set; }
    public string? FilePath { get; set; }
    public string? DownloadDate { get; set; }
}

public class FlingtrainerMod
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Game { get; set; }
    public string? DownloadUrl { get; set; }
    public string? UploadDate { get; set; }
    public bool IsDownloaded { get; set; }
}

//https://flingtrainer.com/download-trainer.php?path=%2Fwp-content%2Fuploads%2F2020%2F05%2FAce.Combat.7.Skies.Unknown.v1.0-v20211019.Plus.11.Trainer-FLiNG.zip
//https://flingtrainer.com/download-trainer.php?path=%2fwp-content%2fuploads%2f2021%2f10%2fAce.Combat.7.Skies.Unknown.v1.0-v20211019.Plus.11.Trainer-FLiNG.zip
//https://flingtrainer.com/download-trainer.php?path=%2Fwp-content%2Fuploads%2F2020%2F05%2FAce.Combat.7.Skies.Unknown.v1.0-v20211019.Plus.11.Trainer-FLiNG.zip
//https://flingtrainer.com/download-trainer.php?path=%2Fwp-content%2Fuploads%2F2021%2F10%2FAce.Combat.7.Skies.Unknown.v1.0-v20211019.Plus.11.Trainer-FLiNG.zip
//Ace Combat 7 Skies Unknown v1.0-v20211019 Plus 11 Trainer-FLiNG
//Ace Combat 7 Skies Unknown v1.0-v20211019 Plus 11 Trainer.exe