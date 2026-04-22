using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using GameTrainersManager.Utils;
using GameTrainersManager.Models;


namespace GameTrainersManager;

public partial class Index : Form
{
    private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    private string? modsDir;
    private readonly string configFile;
    private List<ModItem>? modsList;
    private List<FlingtrainerMod>? allModsList;
    private const string defaultSource = "https://flingtrainer.com/";
    private bool isShowingLibrary = false;
    private List<FlingtrainerMod>? filteredModsList;
    
    // 下载完成事件
    public event EventHandler<FlingtrainerMod>? DownloadCompleted;

    // 控件声明
    private System.Windows.Forms.ListView? localModsListView;
    private System.Windows.Forms.ColumnHeader? columnHeader2;
    private System.Windows.Forms.ColumnHeader? columnHeader8;
    private System.Windows.Forms.ColumnHeader? columnHeader6;
    private System.Windows.Forms.ColumnHeader? columnHeader3;
    private System.Windows.Forms.StatusStrip? statusStrip1;
    private System.Windows.Forms.ToolStripStatusLabel? ModCountTextBlock;
    private System.Windows.Forms.ToolStripButton? OpenDownloadDirButton;
    private System.Windows.Forms.ToolStripButton? FindModsButton;
    


    public Index()
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
            Logger.WriteLine($"图标加载失败: {ex.Message}");
        }
        

        
        // 默认下载路径为 library 目录
        modsDir = Path.Combine(baseDir, "library");
        configFile = Path.Combine(baseDir, "config.json");
        _setup(); // 调用同步方法
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

        // 加载修改器列表（加载配置、扫描目录、保存配置）
        LoadModsList();
    }

    private void _loadConfig()
    {
        var config = ModManager.LoadConfig(configFile, ref modsDir);
        if (config != null)
        {
            modsList = config.Mods ?? new List<ModItem>();
        }
        else
        {
            modsList = new List<ModItem>();
        }
        allModsList = new List<FlingtrainerMod>();
    }

    private void _saveConfig()
    {
        if (modsList != null)
        {
            ModManager.SaveConfig(configFile, modsList, modsDir);
        }
    }

    private void ScanModsDirectory()
    {
        if (modsList != null)
        {
            ModManager.ScanModsDirectory(modsDir, modsList);
            _saveConfig();
        }
    }

    /// <summary>
    /// 加载修改器列表
    /// </summary>
    private void LoadModsList()
    {
        if (modsList != null)
        {
            ModManager.LoadModsList(configFile, ref modsDir, modsList);
            
            // 设置我的修改器视图的列
            SetupMyModsColumns();

            if (localModsListView != null)
            {
                localModsListView.Items.Clear();

                if (modsList != null)
                {
                    // 检查并移除文件不存在的修改器
                    List<ModItem> modsToRemove = new List<ModItem>();
                    foreach (var mod in modsList)
                    {
                        if (mod != null && mod.Downloaded && !string.IsNullOrEmpty(mod.FilePath))
                        {
                            if (!File.Exists(mod.FilePath))
                            {
                                modsToRemove.Add(mod);
                            }
                        }
                    }

                    // 从缓存中移除
                    if (modsToRemove.Count > 0)
                    {
                        foreach (var modToRemove in modsToRemove)
                        {
                            modsList.Remove(modToRemove);
                        }
                        // 重新编号
                        for (int i = 0; i < modsList.Count; i++)
                        {
                            modsList[i].Id = i + 1;
                        }
                        // 保存配置
                        _saveConfig();
                    }

                    // 显示修改器列表
                    foreach (var mod in modsList)
                    {
                        if (mod != null)
                        {
                            // 优先显示配置中的游戏名字，如果没有才会显示修改器文件名字
                            string displayName = !string.IsNullOrEmpty(mod.Game) && !string.IsNullOrEmpty(mod.Name) ? $"{mod.Game}" : (mod.Name ?? "");
                            var item = new ListViewItem(displayName);
                            item.SubItems.Add("启动");
                            item.SubItems.Add("检查更新");
                            item.SubItems.Add("删除");
                            localModsListView.Items.Add(item);
                        }
                    }

                    if (ModCountTextBlock != null)
                    {
                        ModCountTextBlock.Text = $"修改器数量: {modsList.Count}";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 清空修改器列表
    /// </summary>
    private void ClearModsList()
    {
        Logger.WriteLine("========== 方法: ClearModsList 开始 ==========");
        try
        {
            // 调用 ModManager 中的 ClearModsList 方法
            if (modsList != null)
            {
                ModManager.ClearModsList(configFile, modsList);
            }
            
            // // 清空界面上的项目
            // if (localModsListView != null)
            // {
            //     localModsListView.Items.Clear();
            //     Logger.WriteLine("界面修改器列表已清空");
            // }
            
            // 更新界面显示
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "修改器库: 0 个修改器";
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"清空修改器列表失败: {ex.Message}");
        }
        finally
        {
            Logger.WriteLine("========== 方法: ClearModsList 结束 ==========");
        }
    }

    public async Task GetDownloadLinkAndStartDownloadAsync(FlingtrainerMod mod)
    {
        Logger.WriteLine("========== 方法: GetDownloadLinkAndStartDownloadAsync 开始 ==========");
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
            
            var (downloadLink, uploadDate, htmlContent) = await ModManager.GetDownloadLinkAsync(mod.Url);
            
            if (downloadLink != "未知")
            {
                // 检查是否存在相同游戏的旧修改器，先删除
                if (modsList != null)
                {
                    var existingMod = modsList.FirstOrDefault(m => m.Game == mod.Game);
                    if (existingMod != null)
                    {
                        // 删除旧的修改器文件
                        if (!string.IsNullOrEmpty(existingMod.FilePath) && File.Exists(existingMod.FilePath))
                        {
                            try
                            {
                                File.Delete(existingMod.FilePath);
                                Logger.WriteLine($"已删除旧修改器文件: {existingMod.FilePath}");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLine($"删除旧修改器文件失败: {ex.Message}");
                            }
                        }
                    }
                }

                // 直接开始下载，不需要确认
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"正在下载: {mod.Name}...";
                }
                
                await ModManager.DownloadFileAsync(mod, downloadLink, uploadDate, modsDir, modsList);
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
            Logger.WriteLine($"获取下载链接失败: {ex.Message}");
            MessageBox.Show($"获取下载链接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "获取下载链接失败";
            }
        }
        finally
        {
            Logger.WriteLine("========== 方法: GetDownloadLinkAndStartDownloadAsync 结束 ==========");
        }
    }

    // 公共下载方法，可被其他窗口调用
    public async Task DownloadModAsync(FlingtrainerMod mod, Label? statusLabel = null)
    {
        Logger.WriteLine("========== 开始下载修改器 ==========");
        Logger.WriteLine("========== DownloadModAsync ==========");
        Logger.WriteLine($"游戏名: {mod.Game}");
        Logger.WriteLine($"修改器名: {mod.Name}");
        Logger.WriteLine($"下载链接: {mod.Url}");
        
        try
        {
            if (statusLabel != null)
            {
                statusLabel.Text = $"正在获取下载链接: {mod.Game}...";
                Logger.WriteLine("状态更新: 正在获取下载链接");
            }
            
            if (string.IsNullOrEmpty(mod.Url))
            {
                Logger.WriteLine("错误: 修改器链接无效");
                MessageBox.Show($"修改器链接无效: {mod.Game}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            Logger.WriteLine("正在获取下载链接...");
            var (downloadLink, uploadDate, htmlContent) = await ModManager.GetDownloadLinkAsync(mod.Url);
            Logger.WriteLine($"获取到下载链接: {downloadLink}");
            Logger.WriteLine($"上传日期: {uploadDate}");
            
            if (downloadLink != "未知")
            {
                // 检查是否存在相同游戏的旧修改器，先删除
                if (modsList != null)
                {
                    var existingMod = modsList.FirstOrDefault(m => m.Game == mod.Game);
                    if (existingMod != null)
                    {
                        Logger.WriteLine($"发现相同游戏的旧修改器: {existingMod.Name}");
                        // 删除旧的修改器文件
                        if (!string.IsNullOrEmpty(existingMod.FilePath) && File.Exists(existingMod.FilePath))
                        {
                            try
                            {
                                File.Delete(existingMod.FilePath);
                                Logger.WriteLine($"已删除旧修改器文件: {existingMod.FilePath}");
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLine($"删除旧修改器文件失败: {ex.Message}");
                            }
                        }
                    }
                }

                // 直接开始下载，不需要确认
                if (statusLabel != null)
                {
                    statusLabel.Text = $"正在下载: {mod.Game}...";
                    Logger.WriteLine("状态更新: 正在下载");
                }
                
                Logger.WriteLine("开始下载文件...");
                await ModManager.DownloadFileAsync(mod, downloadLink, uploadDate, modsDir, modsList);
                Logger.WriteLine("下载完成");
                
                // 刷新文件目录
                LoadModsList();
                
                // 重新加载所有修改器列表，确保显示最新的下载状态
                var updatedMods = await GetModsFromFlingtrainerCSharpAsync();
                if (updatedMods != null)
                {
                    allModsList = updatedMods;
                }
                
                // 更新列表视图
                UpdateLibraryListView();
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"修改器库: {(allModsList != null ? allModsList.Count : 0)} 个修改器";
                }
                
                if (statusLabel != null)
                {
                    statusLabel.Text = $"下载完成: {mod.Game}";
                    Logger.WriteLine("状态更新: 下载完成");
                }
                
                // 显示下载完成消息
                MessageBox.Show($"下载完成: {mod.Name}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // 清空修改器列表，然后重新加载
                ClearModsList();
                LoadModsList();
                
                // 触发下载完成事件
                DownloadCompleted?.Invoke(this, mod);
            }
            else
            {
                Logger.WriteLine("错误: 下载链接为未知");
                MessageBox.Show($"无法获取下载链接，请稍后重试。\n\n错误: 下载链接为未知", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (statusLabel != null)
                {
                    statusLabel.Text = "获取下载链接失败";
                    Logger.WriteLine("状态更新: 获取下载链接失败");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"下载失败: {ex.Message}");
            Logger.WriteLine($"错误堆栈: {ex.StackTrace}");
            MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (statusLabel != null)
            {
                statusLabel.Text = $"下载失败: {ex.Message}";
                Logger.WriteLine("状态更新: 下载失败");
            }
        }
        finally
        {
            Logger.WriteLine("========== 下载流程结束 ==========");
        }
    }
    

    


    private void UpdateLibraryListView()
    {
        // 设置修改器库视图的列
        SetupLibraryColumns();

        if (localModsListView != null)
        {
            localModsListView.Items.Clear();

            if (modsList != null && modsList.Count > 0)
            {
                foreach (var modItem in modsList)
                {
                    if (modItem != null && modItem.Downloaded)
                    {
                        var item = new ListViewItem(modItem.Game ?? "");

                        // 检查是否有更新
                        var onlineMod = allModsList?.FirstOrDefault(m => m.Game == modItem.Game);
                        string buttonText = "重新下载";
                        string statusText = "已下载";

                        if (onlineMod != null && !string.IsNullOrEmpty(onlineMod.UploadDate) && !string.IsNullOrEmpty(modItem.UploadDate))
                        {
                            // 提取年月进行比较 (格式: 2024-08)
                            string modUploadMonth = onlineMod.UploadDate.Length >= 7 ? onlineMod.UploadDate.Substring(0, 7) : onlineMod.UploadDate;
                            string existingUploadMonth = modItem.UploadDate.Length >= 7 ? modItem.UploadDate.Substring(0, 7) : modItem.UploadDate;

                            if (modUploadMonth != existingUploadMonth)
                            {
                                buttonText = "更新";
                            }
                        }

                        item.SubItems.Add(statusText);
                        item.SubItems.Add(buttonText);
                        localModsListView.Items.Add(item);
                    }
                }
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"修改器库: {modsList.Count} 个修改器";
                }
            }
            else
            {
                // 没有找到修改器，在列表中显示提示信息
                var item = new ListViewItem("未找到修改器");
                item.SubItems.Add("");
                item.SubItems.Add("");
                localModsListView.Items.Add(item);
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = "修改器库: 0 个修改器";
                }
            }
        }

    }

    private void SetupMyModsColumns()
    {
        if (localModsListView != null)
        {
            localModsListView.Columns.Clear();
            if (columnHeader2 != null)
            {
                localModsListView.Columns.Add(columnHeader2); // 游戏名
                columnHeader2.Text = "游戏名";
                columnHeader2.Width = 300;
            }
            if (columnHeader6 != null)
            {
                localModsListView.Columns.Add(columnHeader6); // 启动
                columnHeader6.Text = "启动";
                columnHeader6.Width = 80;
            }
            if (columnHeader8 != null)
            {
                localModsListView.Columns.Add(columnHeader8); // 检查更新
                columnHeader8.Text = "检查更新";
                columnHeader8.Width = 80;
            }
            if (columnHeader3 != null)
            {
                localModsListView.Columns.Add(columnHeader3); // 删除
                columnHeader3.Text = "删除";
                columnHeader3.Width = 80;
            }
        }
    }

    private void SetupLibraryColumns()
    {
        if (localModsListView != null)
        {
            localModsListView.Columns.Clear();
            if (columnHeader2 != null)
            {
                localModsListView.Columns.Add(columnHeader2); // 游戏
                columnHeader2.Text = "游戏";
                columnHeader2.Width = 300;
            }
            if (columnHeader8 != null)
            {
                localModsListView.Columns.Add(columnHeader8); // 操作
                columnHeader8.Text = "操作";
                columnHeader8.Width = 80;
            }
        }
    }

    private void Index_Load(object sender, EventArgs e)
    {
        // 表单加载时的初始化操作
        // 添加列表视图的点击事件处理
        if (localModsListView != null)
        {
            localModsListView.MouseClick += LocalModsListView_MouseClick;
        }
        
        // 初始化过滤列表
        filteredModsList = new List<FlingtrainerMod>();
        Logger.WriteLine("========== Form1_Load 完成 ==========");
    }

    private void FindModsButton_Click(object? sender, EventArgs e)
    {
        SearchModsForm searchForm = new SearchModsForm();
        searchForm.StartPosition = FormStartPosition.CenterParent;
        searchForm.Owner = this;
        searchForm.SetOwnerIndex(this);
        searchForm.ShowDialog();
    }

    private void LocalModsListView_MouseClick(object? sender, MouseEventArgs e)
    {
        // 获取点击的位置
        if (localModsListView != null)
        {
            var hitTest = localModsListView.HitTest(e.Location);
            if (hitTest.Item != null)
            {
                // 获取点击的列索引
                int columnIndex = -1;
                int x = 0;
                foreach (ColumnHeader column in localModsListView.Columns)
                {
                    x += column.Width;
                    if (e.X < x)
                    {
                        columnIndex = localModsListView.Columns.IndexOf(column);
                        break;
                    }
                }

                // 根据当前视图模式处理不同的按钮点击
                if (isShowingLibrary)
                {
                    // 修改器库视图 - 处理下载按钮点击
                    if (columnIndex == 2) // 下载按钮列 (索引2是第3列)
                    {
                        int modIndex = localModsListView.Items.IndexOf(hitTest.Item);
                        // 使用过滤后的列表
                        var displayList = filteredModsList != null && filteredModsList.Count > 0 ? filteredModsList : allModsList;
                        if (displayList != null && modIndex >= 0 && modIndex < displayList.Count)
                        {
                            var mod = displayList[modIndex];
                            var existingMod = modsList?.FirstOrDefault(m => m.Game == mod.Game);

                            // 检查是否已下载，并比较上传时间
                            bool needsUpdate = false;
                            if (existingMod != null && existingMod.Downloaded && !string.IsNullOrEmpty(mod.UploadDate) && !string.IsNullOrEmpty(existingMod.UploadDate))
                            {
                                string modUploadMonth = mod.UploadDate.Length >= 7 ? mod.UploadDate.Substring(0, 7) : mod.UploadDate;
                                string existingUploadMonth = existingMod.UploadDate.Length >= 7 ? existingMod.UploadDate.Substring(0, 7) : existingMod.UploadDate;
                                needsUpdate = modUploadMonth != existingUploadMonth;
                            }

                            if (existingMod == null || !existingMod.Downloaded)
                            {
                                // 先获取真实的下载链接
                                _ = GetDownloadLinkAndStartDownloadAsync(mod);
                            }
                            else if (needsUpdate)
                            {
                                // 需要更新
                                var result = MessageBox.Show($"发现新版本，是否更新？", "确认更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (result == DialogResult.Yes)
                                {
                                    _ = GetDownloadLinkAndStartDownloadAsync(mod);
                                }
                            }
                            else
                            {
                                var result = MessageBox.Show($"文件已存在，是否重新下载？", "确认重新下载", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (result == DialogResult.Yes)
                                {
                                    _ = GetDownloadLinkAndStartDownloadAsync(mod);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 我的修改器视图 - 处理启动、重命名、删除按钮点击
                    // 处理启动按钮点击
                    if (columnIndex == 1) // 启动按钮列
                    {
                        int modIndex = localModsListView.Items.IndexOf(hitTest.Item);
                        if (modsList != null && modIndex >= 0 && modIndex < modsList.Count)
                        {
                            var mod = modsList[modIndex];
                            if (mod.Downloaded && !string.IsNullOrEmpty(mod.FilePath))
                            {
                                try
                                {
                                    string filePath = mod.FilePath;
                                    Logger.WriteLine($"原始路径: {filePath}");

                                    if (!File.Exists(filePath))
                                    {
                                        MessageBox.Show($"修改器文件不存在: {filePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
                                    }

                                    var processInfo = new System.Diagnostics.ProcessStartInfo();
                                    processInfo.FileName = filePath;
                                    processInfo.UseShellExecute = true;
                                    processInfo.Verb = "runas";
                                    processInfo.WorkingDirectory = Path.GetDirectoryName(filePath);

                                    try
                                    {
                                        System.Diagnostics.Process.Start(processInfo);
                                    }
                                    catch (System.ComponentModel.Win32Exception ex)
                                    {
                                        if (ex.NativeErrorCode == 1223)
                                        {
                                            MessageBox.Show("您取消了管理员权限请求，修改器可能无法正常运行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }
                                        else
                                        {
                                            Logger.WriteLine($"启动修改器失败: {ex.Message}");
                                            MessageBox.Show($"启动修改器失败: {ex.Message}\n路径: {filePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLine($"启动修改器失败: {ex.Message}");
                                    MessageBox.Show($"启动修改器失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("修改器文件不存在，无法启动", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    // 处理检查更新按钮点击
                    else if (columnIndex == 2) // 检查更新按钮列
                    {
                        int modIndex = localModsListView.Items.IndexOf(hitTest.Item);
                        if (modsList != null && modIndex >= 0 && modIndex < modsList.Count)
                        {
                            var mod = modsList[modIndex];
                            if (ModCountTextBlock != null)
                            {
                                ModCountTextBlock.Text = $"正在检查更新: {mod.Name}...";
                            }
                            
                            // 异步检查更新
                            _ = CheckModUpdateAsync(mod);
                        }
                    }
                    // 处理删除按钮点击
                    else if (columnIndex == 3) // 删除按钮列
                    {
                        int modIndex = localModsListView.Items.IndexOf(hitTest.Item);
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
                                        Logger.WriteLine($"删除文件失败: {ex.Message}");
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
                                LoadModsList();
                                MessageBox.Show("修改器已删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
        }
    }





    private void OpenDownloadDirButton_Click(object sender, EventArgs e)
    {
        // 打开修改器所在的文件夹
        bool success = ModManager.OpenModsFolder(modsDir);
        if (!success)
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
                LoadModsList();
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"下载失败: {ex.Message}");
        }
    }

    private Task GetFlingtrainerModsAsync()
    {
        try
        {
            // 暂时注释掉all-trainers接口调用
            // allModsList = await GetModsFromFlingtrainerCSharpAsync();
            
            // 使用空列表，等待查询时动态搜索
            allModsList = new List<FlingtrainerMod>();
            Logger.WriteLine("已禁用all-trainers接口，将使用动态搜索功能");
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"获取修改器库失败: {ex.Message}");
            // 发生错误时，将 allModsList 设为空列表
            allModsList = new List<FlingtrainerMod>();
        }
        
        return Task.CompletedTask;
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
                Logger.WriteLine("未找到 class='items-outer' 的 div");
            }
            
            Logger.WriteLine($"成功解析到 {mods.Count} 个修改器");
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"C# 实现获取修改器失败: {ex.Message}");
        }
        return mods;
    }
    

    
    private async Task CheckModUpdateAsync(ModItem mod)
    {
        try
        {
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "正在检查更新...";
            }
            
            var (hasUpdate, latestUploadDate, errorMessage) = await ModManager.CheckModUpdateAsync(mod);
            
            if (hasUpdate)
            {
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"发现新版本: {mod.Name}";
                }
                var result = MessageBox.Show($"发现新版本，是否更新？", "确认更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    _ = GetDownloadLinkAndStartDownloadAsync(new FlingtrainerMod { Name = mod.Name, Game = mod.Game, Url = mod.Url });
                }
            }
            else if (!string.IsNullOrEmpty(errorMessage) && errorMessage != "已是最新版本")
            {
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = errorMessage;
                }
                MessageBox.Show(errorMessage, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (ModCountTextBlock != null)
                {
                    ModCountTextBlock.Text = $"{mod.Name} 已是最新版本";
                }
                MessageBox.Show($"{mod.Name} 已是最新版本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"检查更新失败: {ex.Message}");
            if (ModCountTextBlock != null)
            {
                ModCountTextBlock.Text = "检查更新失败";
            }
            MessageBox.Show($"检查更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<(string downloadLink, string uploadDate, string htmlContent)> GetDownloadLinkAsync(string url)
    {
        Logger.WriteLine("========== 方法: GetDownloadLinkAsync 开始 ==========");
        Logger.WriteLine($"URL: {url}");
        try
        {
            // 使用 ModManager 中的 DownloadClient
            var response = await ModManager.DownloadClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            Logger.WriteLine($"开始解析下载页面: {url}");
            
            // 查找 class="download-attachments style-table" 的 div
            var downloadDiv = doc.DocumentNode.SelectSingleNode("//div[@class='download-attachments style-table']");
            Logger.WriteLine($"找到 download-attachments: {downloadDiv != null}");
            
            if (downloadDiv != null)
            {
                // 在这个div内部查找 class="da-attachments-table" 的 table
                var tableDiv = downloadDiv.SelectSingleNode(".//table[@class='da-attachments-table']");
                Logger.WriteLine($"找到 da-attachments-table: {tableDiv != null}");
                
                if (tableDiv != null)
                {
                    // 先查找 tbody
                    var tbody = tableDiv.SelectSingleNode(".//tbody");
                    Logger.WriteLine($"找到 tbody: {tbody != null}");
                    
                    if (tbody != null)
                    {
                        // 查找 class="zip" 的 tr 的第一个
                        var exeDiv = tbody.SelectSingleNode(".//tr[@class='zip'][1]");
                        Logger.WriteLine($"找到第一个 zip tr: {exeDiv != null}");
                        
                        // 如果仍然不存在，尝试查找任何 tr
                        if (exeDiv == null)
                        {
                            exeDiv = tbody.SelectSingleNode(".//tr[1]");
                            Logger.WriteLine($"找到第一个 tr: {exeDiv != null}");
                        }
                        
                        if (exeDiv != null)
                        {
                            // 获取 class="attachment-title" 里面的下载地址 - 直接选择子td
                            var titleDiv = exeDiv.SelectSingleNode("./td[@class='attachment-title']");
                            var dateDiv = exeDiv.SelectSingleNode("./td[@class='attachment-date']");
                            Logger.WriteLine($"找到 attachment-title: {titleDiv != null}");
                            Logger.WriteLine($"找到 attachment-date: {dateDiv != null}");
                    
                        if (titleDiv != null)
                        {
                            // 查找 titleDiv 中的 a 标签，获取下载链接 - 直接选择子a标签
                            var downloadLink = "";
                            var aTag = titleDiv.SelectSingleNode("./a");
                            Logger.WriteLine($"找到 a 标签: {aTag != null}");
                            
                            // 新方法：直接构建下载链接（最稳定）
                            if (aTag != null)
                            {
                                // 从a标签的title属性获取文件名
                                string fileName = aTag.GetAttributeValue("title", "");
                                Logger.WriteLine($"获取到文件名: {fileName}");
                                
                                // 从td标签获取日期
                                string rawDate = dateDiv != null ? dateDiv.InnerText.Trim() : "";
                                Logger.WriteLine($"获取到原始日期: {rawDate}");
                                
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    try
                                    {
                                        // 获取文章发布时间
                                        string articlePublishDate = await ModManager.GetArticlePublishDateAsync(url);
                                        Logger.WriteLine($"获取到文章发布时间: {articlePublishDate}");
                                        
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
                                            Logger.WriteLine($"构建的真实下载链接: {downloadLink}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteLine($"构建下载链接失败: {ex.Message}");
                                    }
                                }
                            }
                            
                            // 后备方法1：从a标签获取跳转链接
                            if (string.IsNullOrEmpty(downloadLink) && aTag != null)
                            {
                                var redirectLink = aTag.GetAttributeValue("href", "");
                                Logger.WriteLine($"获取到跳转链接: {redirectLink}");
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
                                        Logger.WriteLine($"从源码中找到真实下载链接: {downloadLink}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLine($"搜索真实下载链接失败: {ex.Message}");
                                }
                            }
                            
                            var uploadDate = dateDiv != null ? dateDiv.InnerText.Trim() : DateTime.Now.ToString("yyyy-MM-dd");
                            Logger.WriteLine($"获取到上传时间: {uploadDate}");
                            
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
            Logger.WriteLine($"获取下载链接失败: {ex.Message}");
        }
        finally
        {
            Logger.WriteLine("========== 方法: GetDownloadLinkAsync 结束 ==========");
        }
        return ("未知", "未知", "");
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
            LoadModsList();
        }
    }

    private string ConstructSearchUrl(string searchTerm)
    {
        string processedTerm = searchTerm
            .ToLower()
            .Trim();
        
        string encodedTerm = Uri.EscapeDataString(processedTerm);
        
        return $"https://flingtrainer.com/?s={encodedTerm}";
    }

    private async Task<List<FlingtrainerMod>> SearchModsFromUrlAsync(string searchUrl)
    {
        var searchResults = new List<FlingtrainerMod>();
        
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            var response = await client.GetAsync(searchUrl);
            response.EnsureSuccessStatusCode();
            
            var htmlContent = await response.Content.ReadAsStringAsync();
            
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);
            
            var contentDiv = doc.DocumentNode.SelectSingleNode("//div[@class='content']");
            if (contentDiv != null)
            {
                Logger.WriteLine("找到content区域");
                
                var articleNodes = contentDiv.SelectNodes(".//article[contains(@id, 'post-')]");
                if (articleNodes != null)
                {
                    Logger.WriteLine($"找到 {articleNodes.Count} 个article标签");
                    
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
                                
                                Logger.WriteLine($"找到修改器: {title} -> {url}");
                                
                                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(url) && url.Contains("/trainer/"))
                                {
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
            
            if (searchResults.Count == 0)
            {
                Logger.WriteLine("使用后备方案搜索修改器链接");
                
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
                            
                            if (!searchResults.Any(m => m.Url == mod.Url))
                            {
                                searchResults.Add(mod);
                            }
                        }
                    }
                }
            }
            
            Logger.WriteLine($"总共找到 {searchResults.Count} 个修改器");
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"搜索修改器失败: {ex.Message}");
            throw;
        }
        
        return searchResults;
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
    public string? FileName { get; set; }
    public string? UploadDate { get; set; }
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
