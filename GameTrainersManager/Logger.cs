using System;
using System.Diagnostics;
using System.IO;

namespace GameTrainersManager
{
    public static class Logger
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        private static readonly object lockObj = new object();

        static Logger()
        {
            try
            {
                File.WriteAllText(logFilePath, $"=== 日志启动: {DateTime.Now} ===\n");
            }
            catch
            {
                // 忽略日志文件初始化错误
            }
        }

        public static void WriteLine(string message)
        {
            string logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            
            lock (lockObj)
            {
                try
                {
                    Debug.WriteLine(logMessage);
                    
                    File.AppendAllText(logFilePath, logMessage + "\n");
                }
                catch
                {
                    // 忽略日志写入错误
                }
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public static void WriteError(string message, Exception? ex = null)
        {
            string errorMsg = ex != null ? $"{message}\n异常: {ex.Message}\n堆栈: {ex.StackTrace}" : message;
            WriteLine($"[错误] {errorMsg}");
        }
    }
}