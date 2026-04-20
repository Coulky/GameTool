using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GameToolWinForms.Services;
// 明确使用 HtmlAgilityPack 的 HtmlDocument
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace GameToolWinForms;

public partial class Form1 : Form
{
    private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    private string modsDir;
    private readonly string configFile;
    private List<ModItem> modsList;
    private List<FlingtrainerMod> allModsList; // 存储所有修改器库
    private const string defaultSource = "https://flingtrainer.com/";
    private PythonService pythonService;

    // 控件声明
    private System.Windows.Forms.ToolStrip toolStrip1;

    private System.Windows.Forms.ToolStripButton ListModsButton;
    private System.Windows.Forms.ToolStripButton SearchModLibraryButton;
    private System.Windows.Forms.ListView listView1;
    private System.Windows.Forms.ColumnHeader columnHeader1;
    private System.Windows.Forms.ColumnHeader columnHeader2;
    private System.Windows.Forms.ColumnHeader columnHeader3;
    private System.Windows.Forms.ColumnHeader columnHeader4;
    private System.Windows.Forms.ColumnHeader columnHeader5;
    private System.Windows.Forms.StatusStrip statusStrip1;
    private System.Windows.Forms.ToolStripStatusLabel ModCountTextBlock;
    private System.Windows.Forms.ToolStripButton OpenDownloadDirButton;

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
        // 默认下载路径为 library 目录
        modsDir = Path.Combine(baseDir, "library");
        configFile = Path.Combine(baseDir, "config.json");
        pythonService = new PythonService();
        _ = _setupAsync(); // 调用异步方法，不等待完成
    }

    private async Task _setupAsync()
    {
        if (!Directory.Exists(modsDir))
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
            await _loadConfigAsync();
        }

        UpdateModsListView();
    }

    private async Task _loadConfigAsync()
    {
        try
        {
            var configContent = File.ReadAllText(configFile);
            var config = JsonConvert.DeserializeObject<Config>(configContent);
            modsList = config.Mods ?? new List<ModItem>();
            modsDir = config.DownloadDirectory ?? modsDir;
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
        listView1.Items.Clear();

        foreach (var mod in modsList)
        {
            var item = new ListViewItem(mod.Id.ToString());
            item.SubItems.Add(mod.Name);
            item.SubItems.Add(mod.Game);
            item.SubItems.Add(mod.Downloaded ? "已下载" : "未下载");
            item.SubItems.Add(mod.AddedDate.Substring(0, 10));
            listView1.Items.Add(item);
        }

        ModCountTextBlock.Text = $"修改器数量: {modsList.Count}";

    }

    private void Form1_Load(object sender, EventArgs e)
    {
        // 表单加载时的初始化操作
    }

    private void ListModsButton_Click(object sender, EventArgs e)
    {
        // 列出所有修改器的逻辑
        UpdateModsListView();
    }

    private async void SearchModLibraryButton_Click(object sender, EventArgs e)
    {
        // 先获取修改器库
        await GetFlingtrainerModsAsync();
        // 打开修改器库表单
        var form = new FlingtrainerModsForm(allModsList, modsList, this);
        form.ShowDialog();
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
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败: {ex.Message}");
        }
    }

    private async Task GetFlingtrainerModsAsync()
    {
        try
        {
            // 尝试使用Python脚本获取修改器列表
            try
            {
                var result = await pythonService.GetModsFromFlingtrainerAsync();
                allModsList = JsonConvert.DeserializeObject<List<FlingtrainerMod>>(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Python 脚本运行失败: {ex.Message}");
                // 回退到C#实现
                allModsList = await GetModsFromFlingtrainerCSharpAsync();
            }

            if (allModsList.Count > 0)
            {
                var modsForm = new FlingtrainerModsForm(allModsList, modsList, this);
                modsForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("未找到修改器", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取修改器库失败: {ex.Message}");
            MessageBox.Show($"获取修改器库失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // 查找所有修改器链接
            var modLinks = doc.DocumentNode.SelectNodes("//a[contains(@href, '/trainer/')]");
            if (modLinks != null)
            {
                foreach (var link in modLinks)
                {
                    var url = link.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(url) && url.StartsWith("/trainer/"))
                    {
                        url = "https://flingtrainer.com" + url;
                        var name = link.InnerText.Trim();
                        if (!string.IsNullOrEmpty(name))
                        {
                            // 提取游戏名称
                            var gameName = name;
                            var dashIndex = name.LastIndexOf(" - ");
                            if (dashIndex > 0)
                            {
                                gameName = name.Substring(0, dashIndex);
                            }

                            mods.Add(new FlingtrainerMod
                            {
                                Name = name,
                                Url = url,
                                Game = gameName
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"C# 实现获取修改器失败: {ex.Message}");
        }
        return mods;
    }

    public void AddModFromLibrary(FlingtrainerMod mod)
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

public class Config
{
    public string LastUpdated { get; set; }
    public List<ModItem> Mods { get; set; }
    public string DownloadDirectory { get; set; }
}

public class ModItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Game { get; set; }
    public string AddedDate { get; set; }
    public bool Downloaded { get; set; }
    public string FilePath { get; set; }
    public string DownloadDate { get; set; }
}

public class FlingtrainerMod
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Game { get; set; }
}
