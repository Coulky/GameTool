using System;
using System.Collections.Generic;

namespace GameTrainersManager.Models
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class Config
    {
        public string? LastUpdated { get; set; }
        public List<ModItem>? Mods { get; set; }
        public string? DownloadDirectory { get; set; }
    }

    /// <summary>
    /// 修改器项类
    /// </summary>
    public class ModItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Game { get; set; }
        public string? AddedDate { get; set; }
        public bool Downloaded { get; set; }
        public string? DownloadDate { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public string? UploadDate { get; set; }
    }

    /// <summary>
    /// Flingtrainer 修改器类
    /// </summary>
    public class FlingtrainerMod
    {
        public string? Name { get; set; }
        public string? Game { get; set; }
        public string? Url { get; set; }
        public string? DownloadUrl { get; set; }
        public string? UploadDate { get; set; }
        public bool IsDownloaded { get; set; }
    }
}