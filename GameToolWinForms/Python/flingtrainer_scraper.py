#!/usr/bin/env python3
"""
从 flingtrainer.com 抓取修改器列表
"""

import urllib.request
import re
import json

class FlingtrainerScraper:
    def __init__(self, base_url="https://flingtrainer.com/"):
        self.base_url = base_url
        self.all_trainers_url = "https://flingtrainer.com/all-trainers/"
    
    def get_mods(self):
        """获取修改器列表"""
        try:
            # 使用 urllib 发送请求到 all-trainers 页面
            with urllib.request.urlopen(self.all_trainers_url) as response:
                html = response.read().decode('utf-8')
            
            # 解析 HTML，提取修改器链接
            mods = []
            
            # 使用正则表达式提取修改器名称和链接
            # 查找所有符合 /trainer/ 路径的链接
            pattern = r'<a href="(https?://flingtrainer\.com/trainer/[^"/]+/)"[^>]*>([^<]+)</a>'
            matches = re.findall(pattern, html)
            
            for url, name in matches:
                # 过滤掉不需要的链接
                if 'category' not in url and '#' not in url:
                    # 清理名称，去除多余的空白字符
                    name = name.strip()
                    if name:
                        # 提取游戏名称（去除 "Trainer" 后缀）
                        game_name = name.replace(" Trainer", "")
                        mods.append({
                            "name": name,
                            "game": game_name,
                            "url": url
                        })
            
            # 去重，确保每个修改器只出现一次
            unique_mods = []
            seen_urls = set()
            for mod in mods:
                if mod['url'] not in seen_urls:
                    seen_urls.add(mod['url'])
                    unique_mods.append(mod)
            
            # 按首字母排序
            unique_mods.sort(key=lambda x: x['name'])
            
            return unique_mods
        except Exception as e:
            print(f"抓取失败: {e}")
            return []

if __name__ == "__main__":
    scraper = FlingtrainerScraper()
    mods = scraper.get_mods()
    # 输出 JSON 格式
    print(json.dumps(mods, ensure_ascii=False, indent=2))
    # 打印总数
    print(f"\n总共有 {len(mods)} 个修改器")