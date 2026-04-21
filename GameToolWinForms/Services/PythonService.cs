using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameToolWinForms.Services
{
    public class PythonService
    {
        private readonly string? pythonPath;
        private readonly string pythonScriptsPath;

        public PythonService()
        {
            // 尝试找到 Python 可执行文件
            pythonPath = FindPythonExecutable();
            // 设置 Python 脚本路径
            pythonScriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python");
        }

        private string? FindPythonExecutable()
        {
            // 尝试在环境变量中查找 Python
            string? pythonPath = Environment.GetEnvironmentVariable("PATH")?
                .Split(Path.PathSeparator)
                .Select(p => Path.Combine(p, "python.exe"))
                .FirstOrDefault(File.Exists);

            // 如果找不到，尝试常见的安装路径
            if (string.IsNullOrEmpty(pythonPath))
            {
                string[] commonPaths = {
                    @"C:\Python39\python.exe",
                    @"C:\Python310\python.exe",
                    @"C:\Python311\python.exe",
                    @"C:\Program Files\Python39\python.exe",
                    @"C:\Program Files\Python310\python.exe",
                    @"C:\Program Files\Python311\python.exe"
                };

                pythonPath = commonPaths.FirstOrDefault(File.Exists);
            }

            return pythonPath;
        }

        public async Task<string> ExecuteScriptAsync(string scriptName, params string[] arguments)
        {
            if (string.IsNullOrEmpty(pythonPath))
            {
                throw new Exception("Python 可执行文件未找到");
            }

            string scriptPath = Path.Combine(pythonScriptsPath, scriptName);
            if (!File.Exists(scriptPath))
            {
                throw new Exception($"脚本文件未找到: {scriptPath}");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\" " + string.Join(" ", arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = pythonScriptsPath
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) outputBuilder.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Python 脚本执行失败: {errorBuilder.ToString()}");
                }

                return outputBuilder.ToString();
            }
        }

        public async Task<string> GetModsAsync(string configFile)
        {
            return await ExecuteScriptAsync("mod_manager.py", "list", configFile);
        }

        public async Task<string> AddModAsync(string configFile, string name, string url, string game)
        {
            return await ExecuteScriptAsync("mod_manager.py", "add", configFile, name, url, game);
        }

        public async Task<string> RemoveModAsync(string configFile, int modId)
        {
            return await ExecuteScriptAsync("mod_manager.py", "remove", configFile, modId.ToString());
        }

        public async Task<string> SearchModsAsync(string configFile, string keyword)
        {
            return await ExecuteScriptAsync("mod_manager.py", "search", configFile, keyword);
        }

        public async Task<string> DownloadModAsync(string configFile, int modId, string modsDir)
        {
            return await ExecuteScriptAsync("mod_manager.py", "download", configFile, modId.ToString(), modsDir);
        }

        public async Task<string> GetFlingtrainerModsAsync()
        {
            return await ExecuteScriptAsync("flingtrainer_scraper.py");
        }

        public async Task<string> GetModsFromFlingtrainerAsync()
        {
            return await ExecuteScriptAsync("flingtrainer_scraper.py");
        }
    }
}