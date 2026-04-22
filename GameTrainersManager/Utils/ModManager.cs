using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using GameTrainersManager.Models;
using HtmlAgilityPack;

namespace GameTrainersManager.Utils
{
    public class ModManager
    {
        // 静态HttpClientHandler和HttpClient，用于管理Cookie
        private static readonly HttpClientHandler DownloadHandler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer(), // 必须开启Cookie存储
            UseCookies = true,
            AllowAutoRedirect = true // 允许自动重定向
        };
        
        public static readonly HttpClient DownloadClient = new HttpClient(DownloadHandler)
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        
        // 在初始化时设置默认的User-Agent
        static ModManager()
        {
            DownloadClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        /// <summary>
        /// 加载修改器列表
        /// </summary>
        /// <param name="configFile">配置文件路径</param>
        /// <param name="modsDir">修改器目录</param>
        /// <param name="modsList">修改器列表</param>
        public static void LoadModsList(string configFile, ref string modsDir, List<ModItem> modsList)
        {
            Logger.WriteLine("========== 方法: LoadModsList 开始 ==========");
            try
            {
                // 1. 加载配置
                var config = LoadConfig(configFile, ref modsDir);
                if (config != null && config.Mods != null)
                {
                    // 清空现有列表并添加配置中的修改器
                    modsList.Clear();
                    modsList.AddRange(config.Mods);
                }
                else
                {
                    // 配置不存在或为空，清空列表
                    modsList.Clear();
                }
                
                // 2. 扫描本地修改器目录
                ScanModsDirectory(modsDir, modsList);
                
                // 3. 保存更新后的配置
                SaveConfig(configFile, modsList, modsDir);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"加载修改器列表失败: {ex.Message}");
            }
            finally
            {
                Logger.WriteLine("========== 方法: LoadModsList 结束 ==========");
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <param name="configFile">配置文件路径</param>
        /// <param name="modsDir">修改器目录</param>
        /// <returns>配置对象</returns>
        public static Config? LoadConfig(string configFile, ref string modsDir)
        {
            Logger.WriteLine("========== 方法: LoadConfig 开始 ==========");
            try
            {
                if (File.Exists(configFile))
                {
                    var configContent = File.ReadAllText(configFile);
                    var config = JsonConvert.DeserializeObject<Config>(configContent);
                    if (config != null)
                    {
                        modsDir = config.DownloadDirectory ?? modsDir;
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"加载配置文件失败: {ex.Message}");
            }
            finally
            {
                Logger.WriteLine("========== 方法: LoadConfig 结束 ==========");
            }
            return null;
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="configFile">配置文件路径</param>
        /// <param name="modsList">修改器列表</param>
        /// <param name="modsDir">修改器目录</param>
        public static void SaveConfig(string configFile, List<ModItem> modsList, string modsDir)
        {
            Logger.WriteLine("========== 方法: SaveConfig 开始 ==========");
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
                Logger.WriteLine($"保存配置文件失败: {ex.Message}");
            }
            finally
            {
                Logger.WriteLine("========== 方法: SaveConfig 结束 ==========");
            }
        }

        /// <summary>
        /// 扫描本地修改器目录
        /// </summary>
        /// <param name="modsDir">修改器目录</param>
        /// <param name="modsList">修改器列表</param>
        public static void ScanModsDirectory(string modsDir, List<ModItem> modsList)
        {
            Logger.WriteLine("========== 方法: ScanModsDirectory 开始 ==========");
            try
            {
                // 检查 modsDir 是否为空
                if (string.IsNullOrEmpty(modsDir))
                {
                    // 使用与应用程序同级的 library 目录作为默认目录
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    modsDir = Path.Combine(baseDir, "library");
                    Logger.WriteLine($"使用默认修改器目录: {modsDir}");
                }
                
                if (Directory.Exists(modsDir))
                {
                    var modFiles = Directory.GetFiles(modsDir, "*.exe");

                    foreach (var filePath in modFiles)
                    {
                        var fileName = Path.GetFileName(filePath);
                        bool exists = modsList.Any(mod => mod != null &&
                            (!string.IsNullOrEmpty(mod.FilePath) && mod.FilePath == filePath ||
                             !string.IsNullOrEmpty(mod.Name) && mod.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)));

                        if (!exists)
                        {
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

                    // 重新编号
                    for (int i = 0; i < modsList.Count; i++)
                    {
                        modsList[i].Id = i + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"扫描目录失败: {ex.Message}");
            }
            finally
            {
                Logger.WriteLine("========== 方法: ScanModsDirectory 结束 ==========");
            }
        }

        /// <summary>
        /// 清空修改器列表
        /// </summary>
        /// <param name="configFile">配置文件路径</param>
        /// <param name="modsList">修改器列表</param>
        public static void ClearModsList(string configFile, List<ModItem> modsList)
        {
            Logger.WriteLine("========== 方法: ClearModsList 开始 ==========");
            try
            {
                // 清空修改器列表
                if (modsList != null)
                {
                    modsList.Clear();
                    Logger.WriteLine("修改器列表已清空");
                }
                
                // 保存空配置
                // if (!string.IsNullOrEmpty(configFile))
                // {
                //     SaveConfig(configFile, modsList ?? new List<ModItem>(), string.Empty);
                // }
                
                // 注意：此方法不会删除缓存文件
                Logger.WriteLine("缓存文件保持不变");
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

        /// <summary>
        /// 打开修改器文件夹
        /// </summary>
        /// <param name="modsDir">修改器目录</param>
        /// <returns>是否成功打开</returns>
        public static bool OpenModsFolder(string modsDir)
        {
            Logger.WriteLine("========== 方法: OpenModsFolder 开始 ==========");
            try
            {
                // 检查 modsDir 是否为空
                if (string.IsNullOrEmpty(modsDir))
                {
                    // 使用与应用程序同级的 library 目录作为默认目录
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    modsDir = Path.Combine(baseDir, "library");
                    Logger.WriteLine($"使用默认修改器目录: {modsDir}");
                }
                
                if (Directory.Exists(modsDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", modsDir);
                    Logger.WriteLine($"成功打开修改器文件夹: {modsDir}");
                    return true;
                }
                else
                {
                    // 如果目录不存在，尝试创建它
                    try
                    {
                        Directory.CreateDirectory(modsDir);
                        Logger.WriteLine($"创建修改器文件夹: {modsDir}");
                        System.Diagnostics.Process.Start("explorer.exe", modsDir);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine($"修改器文件夹不存在且创建失败: {ex.Message}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"打开修改器文件夹失败: {ex.Message}");
                return false;
            }
            finally
            {
                Logger.WriteLine("========== 方法: OpenModsFolder 结束 ==========");
            }
        }

        /// <summary>
        /// 构造搜索URL
        /// </summary>
        /// <param name="searchTerm">搜索关键词</param>
        /// <returns>搜索URL</returns>
        public static string ConstructSearchUrl(string searchTerm)
        {
            string processedTerm = searchTerm.ToLower().Trim();
            string encodedTerm = Uri.EscapeDataString(processedTerm);
            return $"https://flingtrainer.com/?s={encodedTerm}";
        }

        /// <summary>
        /// 从URL搜索修改器
        /// </summary>
        /// <param name="searchUrl">搜索URL</param>
        /// <returns>搜索结果列表</returns>
        public static List<FlingtrainerMod> SearchModsFromUrl(string searchUrl)
        {
            Logger.WriteLine("========== 方法: SearchModsFromUrl 开始 ==========");
            var results = new List<FlingtrainerMod>();

            try
            {
                // 1. 发送HTTP请求获取搜索结果
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = client.GetAsync(searchUrl).Result;
                response.EnsureSuccessStatusCode();

                var htmlContent = response.Content.ReadAsStringAsync().Result;

                // 2. 解析HTML内容
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(htmlContent);

                // 3. 提取修改器信息
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

                // 4. 如果没有找到结果，使用备用解析方法
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
            finally
            {
                Logger.WriteLine($"========== 方法: SearchModsFromUrl 结束，找到 {results.Count} 个结果 ==========");
            }

            return results;
        }

        /// <summary>
        /// 检查修改器更新
        /// </summary>
        /// <param name="mod">修改器对象</param>
        /// <returns>返回更新检查结果：(是否有更新, 新版本上传日期, 错误信息)</returns>
        public static async Task<(bool hasUpdate, string latestUploadDate, string errorMessage)> CheckModUpdateAsync(ModItem mod)
        {
            Logger.WriteLine("========== 方法: CheckModUpdateAsync 开始 ==========");
            try
            {
                if (string.IsNullOrEmpty(mod.Url))
                {
                    Logger.WriteLine("修改器链接无效，无法检查更新");
                    return (false, "", "修改器链接无效，无法检查更新");
                }
                
                var (downloadLink, uploadDate, htmlContent) = await GetDownloadLinkAsync(mod.Url);
                
                if (downloadLink != "未知" && !string.IsNullOrEmpty(uploadDate))
                {
                    string modUploadMonth = uploadDate.Length >= 7 ? uploadDate.Substring(0, 7) : uploadDate;
                    string existingUploadMonth = mod.UploadDate?.Length >= 7 ? mod.UploadDate.Substring(0, 7) : mod.UploadDate ?? "";
                    
                    Logger.WriteLine($"最新版本上传日期: {uploadDate}");
                    Logger.WriteLine($"当前版本上传日期: {mod.UploadDate}");
                    Logger.WriteLine($"最新版本月份: {modUploadMonth}");
                    Logger.WriteLine($"当前版本月份: {existingUploadMonth}");
                    
                    if (modUploadMonth != existingUploadMonth)
                    {
                        Logger.WriteLine("发现新版本");
                        return (true, uploadDate, "");
                    }
                    else
                    {
                        Logger.WriteLine("已是最新版本");
                        return (false, uploadDate, "已是最新版本");
                    }
                }
                else
                {
                    Logger.WriteLine("无法获取更新信息");
                    return (false, "", "无法获取更新信息");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"检查更新失败: {ex.Message}");
                return (false, "", ex.Message);
            }
            finally
            {
                Logger.WriteLine("========== 方法: CheckModUpdateAsync 结束 ==========");
            }
        }

        /// <returns>返回下载链接和上传日期</returns>
        public static async Task<(string downloadLink, string uploadDate, string htmlContent)> GetDownloadLinkAsync(string url)
        {
            Logger.WriteLine("========== 方法: GetDownloadLinkAsync 开始 ==========");
            Logger.WriteLine($"URL: {url}");
            try
            {
                // 使用静态的HttpClient
                var response = await DownloadClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var htmlContent = await response.Content.ReadAsStringAsync();

                var doc = new HtmlAgilityPack.HtmlDocument();
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
                                                string articlePublishDate = await GetArticlePublishDateAsync(url);
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

        /// <summary>
        /// 获取文章发布日期
        /// </summary>
        /// <param name="url">文章URL</param>
        /// <returns>发布日期</returns>
        public static async Task<string> GetArticlePublishDateAsync(string url)
        {
            try
            {
                var response = await DownloadClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var htmlContent = await response.Content.ReadAsStringAsync();

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(htmlContent);

                // 从 meta 标签获取发布时间
                var metaNode = doc.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']");
                if (metaNode != null)
                {
                    var dateText = metaNode.GetAttributeValue("content", "");
                    if (!string.IsNullOrEmpty(dateText))
                    {
                        // 解析日期格式：2024-08-20T02:16:38+00:00
                        if (dateText.Contains("T"))
                        {
                            return dateText.Split('T')[0];
                        }
                    }
                }

                // 备用方法：查找 time 标签
                var dateNode = doc.DocumentNode.SelectSingleNode("//time[@class='entry-date published']");
                if (dateNode != null)
                {
                    var dateText = dateNode.GetAttributeValue("datetime", "");
                    if (!string.IsNullOrEmpty(dateText))
                    {
                        // 解析日期格式：2021-10-15T12:34:56+00:00
                        if (dateText.Contains("T"))
                        {
                            return dateText.Split('T')[0];
                        }
                    }
                }

                // 备用方法：查找其他日期元素
                var dateNodes = doc.DocumentNode.SelectNodes("//time");
                if (dateNodes != null)
                {
                    foreach (var node in dateNodes)
                    {
                        var dateText = node.GetAttributeValue("datetime", "");
                        if (!string.IsNullOrEmpty(dateText) && dateText.Contains("T"))
                        {
                            return dateText.Split('T')[0];
                        }
                    }
                }

                // 如果找不到日期，返回当前日期
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"获取文章发布日期失败: {ex.Message}");
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="mod">修改器对象</param>
        /// <param name="downloadLink">下载链接</param>
        /// <param name="uploadDate">上传日期</param>
        /// <param name="modsDir">下载目录</param>
        /// <param name="modsList">修改器列表</param>
        /// <returns>下载的文件路径</returns>
        public static async Task<string> DownloadFileAsync(FlingtrainerMod mod, string downloadLink, string uploadDate, string modsDir, List<ModItem> modsList)
        {
            Logger.WriteLine("========== 方法: DownloadFileAsync 开始 ==========");
            Logger.WriteLine($"游戏名: {mod.Game}");
            Logger.WriteLine($"下载链接: {downloadLink}");
            Logger.WriteLine($"上传日期: {uploadDate}");
            try
            {
                // 检查 modsDir 是否为空
                if (string.IsNullOrEmpty(modsDir))
                {
                    // 使用与应用程序同级的 library 目录作为默认下载目录，与本地修改器库打开的目录保持一致
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    modsDir = Path.Combine(baseDir, "library");
                    Logger.WriteLine($"使用默认下载目录: {modsDir}");
                }
                
                // 记录当前使用的下载目录（本地修改器库目录）
                Logger.WriteLine($"本地修改器库目录: {modsDir}");
                
                // 确保下载目录存在
                if (!Directory.Exists(modsDir))
                {
                    Directory.CreateDirectory(modsDir);
                    Logger.WriteLine($"创建下载目录: {modsDir}");
                }

                // 生成文件名
                string fileName = $"{mod.Game}.exe";
                // 清理文件名中的无效字符
                fileName = string.Concat(fileName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                string savePath = Path.Combine(modsDir, fileName);
                Logger.WriteLine($"文件将保存到: {savePath}");

                // 检查是否存在相同游戏的旧修改器，先删除
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

                // 下载文件
                Logger.WriteLine("开始下载文件...");
                using (var response = await DownloadClient.GetAsync(downloadLink, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var bytesRead = 0L;
                    
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            bytesRead += read;
                            
                            if (totalBytes > 0)
                            {
                                var progress = (int)((bytesRead * 100) / totalBytes);
                                Logger.WriteLine($"下载进度: {progress}% ({bytesRead}/{totalBytes} bytes)");
                            }
                        }
                    }
                }
                
                // 验证文件是否存在且大小大于0
                if (!File.Exists(savePath) || new FileInfo(savePath).Length == 0)
                {
                    Logger.WriteLine("下载的文件不存在或为空");
                    throw new Exception("下载的文件不存在或为空");
                }
                
                Logger.WriteLine($"下载完成: {savePath}");
                
                // 更新 mod 对象
                mod.DownloadUrl = downloadLink;
                mod.UploadDate = uploadDate;
                mod.IsDownloaded = true;
                
                // 添加到已下载列表
                string downloadedFileName = Path.GetFileName(savePath);
                
                // 检查是否已存在相同游戏的修改器
                if (existingMod != null)
                {
                    // 更新现有记录
                    existingMod.Name = mod.Name;
                    existingMod.Url = mod.Url;
                    existingMod.AddedDate = DateTime.Now.ToString("o");
                    existingMod.Downloaded = true;
                    existingMod.DownloadDate = DateTime.Now.ToString("yyyy-MM-dd");
                    existingMod.FilePath = savePath;
                    existingMod.FileName = downloadedFileName;
                    existingMod.UploadDate = uploadDate;
                    Logger.WriteLine($"更新现有修改器记录: {existingMod.Name}");
                }
                else
                {
                    // 添加新记录
                    var newMod = new ModItem
                    {
                        Id = modsList.Count + 1,
                        Name = mod.Name,
                        Url = mod.Url,
                        Game = mod.Game,
                        AddedDate = DateTime.Now.ToString("o"),
                        Downloaded = true,
                        DownloadDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        FilePath = savePath,
                        FileName = downloadedFileName,
                        UploadDate = uploadDate
                    };
                    modsList.Add(newMod);
                    Logger.WriteLine($"添加新修改器记录: {newMod.Name}");
                }
                
                // 保存配置
                SaveConfig(Path.Combine(modsDir, "mods_config.json"), modsList, modsDir);
                Logger.WriteLine("保存配置文件");
                
                return savePath;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"下载文件失败: {ex.Message}");
                Logger.WriteLine($"错误堆栈: {ex.StackTrace}");
                throw;
            }
            finally
            {
                Logger.WriteLine("========== 方法: DownloadFileAsync 结束 ==========");
            }
        }
    }
}