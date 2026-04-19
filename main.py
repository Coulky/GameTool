#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
游戏修改器下载工具
用于下载和管理游戏修改器
"""

import os
import requests
import json
import shutil
from datetime import datetime
from bs4 import BeautifulSoup

class GameToolDownloader:
    def __init__(self):
        self.base_dir = os.path.dirname(os.path.abspath(__file__))
        self.mods_dir = os.path.join(self.base_dir, "mods")
        self.config_file = os.path.join(self.base_dir, "config.json")
        self.mods_list = []
        self.default_source = "https://flingtrainer.com/"
        self._setup()
    
    def _setup(self):
        """初始化项目结构"""
        if not os.path.exists(self.mods_dir):
            os.makedirs(self.mods_dir)
        
        if not os.path.exists(self.config_file):
            default_config = {
                "last_updated": datetime.now().isoformat(),
                "mods": []
            }
            with open(self.config_file, 'w', encoding='utf-8') as f:
                json.dump(default_config, f, ensure_ascii=False, indent=2)
        
        self._load_config()
    
    def _load_config(self):
        """加载配置文件"""
        with open(self.config_file, 'r', encoding='utf-8') as f:
            config = json.load(f)
        self.mods_list = config.get("mods", [])
    
    def _save_config(self):
        """保存配置文件"""
        config = {
            "last_updated": datetime.now().isoformat(),
            "mods": self.mods_list
        }
        with open(self.config_file, 'w', encoding='utf-8') as f:
            json.dump(config, f, ensure_ascii=False, indent=2)
    
    def add_mod(self, name, url, game):
        """添加新的修改器"""
        mod = {
            "id": len(self.mods_list) + 1,
            "name": name,
            "url": url,
            "game": game,
            "added_date": datetime.now().isoformat(),
            "downloaded": False
        }
        self.mods_list.append(mod)
        self._save_config()
        print(f"已添加修改器: {name}")
    
    def list_mods(self):
        """列出所有修改器"""
        if not self.mods_list:
            print("没有添加任何修改器")
            return
        
        print("\n已添加的修改器:")
        print("-" * 80)
        for mod in self.mods_list:
            status = "已下载" if mod.get("downloaded", False) else "未下载"
            print(f"ID: {mod['id']} | 名称: {mod['name']} | 游戏: {mod['game']} | 状态: {status}")
        print("-" * 80)
    
    def download_mod(self, mod_id):
        """下载指定修改器"""
        mod = next((m for m in self.mods_list if m['id'] == mod_id), None)
        if not mod:
            print(f"未找到ID为 {mod_id} 的修改器")
            return
        
        try:
            print(f"正在下载: {mod['name']}")
            response = requests.get(mod['url'], stream=True, timeout=30)
            response.raise_for_status()
            
            # 创建游戏目录
            game_dir = os.path.join(self.mods_dir, mod['game'])
            if not os.path.exists(game_dir):
                os.makedirs(game_dir)
            
            # 提取文件名
            filename = os.path.basename(mod['url'])
            if not filename:
                filename = f"{mod['name']}_{datetime.now().strftime('%Y%m%d_%H%M%S')}.zip"
            
            # 下载文件
            file_path = os.path.join(game_dir, filename)
            with open(file_path, 'wb') as f:
                for chunk in response.iter_content(chunk_size=8192):
                    f.write(chunk)
            
            mod['downloaded'] = True
            mod['file_path'] = file_path
            mod['download_date'] = datetime.now().isoformat()
            self._save_config()
            print(f"下载完成: {mod['name']}")
            print(f"保存位置: {file_path}")
        except Exception as e:
            print(f"下载失败: {str(e)}")
    
    def remove_mod(self, mod_id):
        """移除指定修改器"""
        mod = next((m for m in self.mods_list if m['id'] == mod_id), None)
        if not mod:
            print(f"未找到ID为 {mod_id} 的修改器")
            return
        
        # 删除本地文件
        if mod.get('file_path') and os.path.exists(mod['file_path']):
            try:
                os.remove(mod['file_path'])
                print(f"已删除文件: {mod['file_path']}")
            except Exception as e:
                print(f"删除文件失败: {str(e)}")
        
        # 从列表中移除
        self.mods_list = [m for m in self.mods_list if m['id'] != mod_id]
        # 更新ID
        for i, m in enumerate(self.mods_list, 1):
            m['id'] = i
        self._save_config()
        print(f"已移除修改器: {mod['name']}")
    
    def search_mods(self, keyword):
        """搜索修改器"""
        results = [m for m in self.mods_list if keyword.lower() in m['name'].lower() or keyword.lower() in m['game'].lower()]
        if not results:
            print(f"未找到包含 '{keyword}' 的修改器")
            return
        
        print(f"\n搜索结果 (包含 '{keyword}'):")
        print("-" * 80)
        for mod in results:
            status = "已下载" if mod.get("downloaded", False) else "未下载"
            print(f"ID: {mod['id']} | 名称: {mod['name']} | 游戏: {mod['game']} | 状态: {status}")
        print("-" * 80)
    
    def get_flingtrainer_mods(self):
        """从 flingtrainer.com 获取最新修改器列表"""
        print("正在从 flingtrainer.com 获取所有修改器列表...")
        try:
            # 使用 /all-trainers/ 路径获取所有修改器
            all_trainers_url = self.default_source.rstrip('/') + '/all-trainers/'
            response = requests.get(all_trainers_url, timeout=30)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.text, 'html.parser')
            mods = []
            
            # 查找修改器列表（根据实际页面结构调整选择器）
            # 可能需要根据实际页面结构调整
            mod_items = soup.find_all('div', class_='post')
            
            if not mod_items:
                # 尝试其他可能的选择器
                mod_items = soup.find_all('article')
            
            if not mod_items:
                # 尝试查找所有包含修改器链接的元素
                mod_items = soup.find_all('a', href=True)
                mod_items = [item.parent for item in mod_items if 'trainer' in item.text.lower()]
            
            for item in mod_items[:20]:  # 获取前20个修改器
                # 尝试不同方式提取标题和链接
                title_element = item.find('h2') or item.find('h3') or item.find('a')
                if title_element:
                    if title_element.name == 'a':
                        title = title_element.text.strip()
                        link = title_element['href']
                    else:
                        title = title_element.text.strip()
                        link_element = title_element.find('a')
                        if link_element:
                            link = link_element['href']
                        else:
                            continue
                    
                    # 提取游戏名称（假设标题格式为 "游戏名 Trainer"）
                    game_name = title.replace(' Trainer', '')
                    
                    mods.append({
                        'name': title,
                        'url': link,
                        'game': game_name
                    })
            
            if mods:
                print(f"\n从 flingtrainer.com 获取的修改器列表 (共 {len(mods)} 个):")
                print("-" * 80)
                for i, mod in enumerate(mods, 1):
                    print(f"{i}. {mod['name']} (游戏: {mod['game']})")
                print("-" * 80)
                
                # 询问用户是否要添加其中的修改器
                choice = input("是否要添加其中的修改器？(y/n): ")
                if choice.lower() == 'y':
                    mod_index = int(input("请输入要添加的修改器编号: ")) - 1
                    if 0 <= mod_index < len(mods):
                        mod = mods[mod_index]
                        self.add_mod(mod['name'], mod['url'], mod['game'])
            else:
                print("未找到修改器列表")
                
        except Exception as e:
            print(f"获取修改器列表失败: {str(e)}")

def main():
    """主函数"""
    downloader = GameToolDownloader()
    
    while True:
        print("\n游戏修改器下载工具")
        print("1. 添加修改器")
        print("2. 列出所有修改器")
        print("3. 下载修改器")
        print("4. 移除修改器")
        print("5. 搜索修改器")
        print("6. 从 flingtrainer.com 获取最新修改器")
        print("7. 退出")
        
        choice = input("请选择操作: ")
        
        if choice == "1":
            name = input("请输入修改器名称: ")
            url = input("请输入下载链接: ")
            game = input("请输入游戏名称: ")
            downloader.add_mod(name, url, game)
        
        elif choice == "2":
            downloader.list_mods()
        
        elif choice == "3":
            mod_id = int(input("请输入修改器ID: "))
            downloader.download_mod(mod_id)
        
        elif choice == "4":
            mod_id = int(input("请输入修改器ID: "))
            downloader.remove_mod(mod_id)
        
        elif choice == "5":
            keyword = input("请输入搜索关键词: ")
            downloader.search_mods(keyword)
        
        elif choice == "6":
            downloader.get_flingtrainer_mods()
        
        elif choice == "7":
            print("再见!")
            break
        
        else:
            print("无效的选择，请重新输入")

if __name__ == "__main__":
    main()
