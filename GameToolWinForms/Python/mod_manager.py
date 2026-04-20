#!/usr/bin/env python3
"""
游戏修改器管理脚本
"""

import json
import os
import sys
import urllib.request
import urllib.error

class ModManager:
    def __init__(self, config_file):
        self.config_file = config_file
        self.config = self._load_config()
    
    def _load_config(self):
        """加载配置文件"""
        if not os.path.exists(self.config_file):
            return {
                "last_updated": "",
                "mods": []
            }
        
        with open(self.config_file, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    def _save_config(self):
        """保存配置文件"""
        with open(self.config_file, 'w', encoding='utf-8') as f:
            json.dump(self.config, f, indent=4, ensure_ascii=False)
    
    def list_mods(self):
        """列出所有修改器"""
        return json.dumps(self.config.get('mods', []), ensure_ascii=False)
    
    def add_mod(self, name, url, game):
        """添加修改器"""
        mods = self.config.get('mods', [])
        new_id = len(mods) + 1
        
        new_mod = {
            "id": new_id,
            "name": name,
            "url": url,
            "game": game,
            "added_date": "",
            "downloaded": False,
            "file_path": ""
        }
        
        mods.append(new_mod)
        self.config['mods'] = mods
        self._save_config()
        
        return json.dumps({"success": True, "mod": new_mod}, ensure_ascii=False)
    
    def remove_mod(self, mod_id):
        """移除修改器"""
        mods = self.config.get('mods', [])
        mods = [mod for mod in mods if mod['id'] != mod_id]
        
        # 更新ID
        for i, mod in enumerate(mods):
            mod['id'] = i + 1
        
        self.config['mods'] = mods
        self._save_config()
        
        return json.dumps({"success": True}, ensure_ascii=False)
    
    def search_mods(self, keyword):
        """搜索修改器"""
        mods = self.config.get('mods', [])
        results = [mod for mod in mods if keyword.lower() in mod['name'].lower() or keyword.lower() in mod['game'].lower()]
        return json.dumps(results, ensure_ascii=False)
    
    def download_mod(self, mod_id, mods_dir):
        """下载修改器"""
        mods = self.config.get('mods', [])
        mod = next((m for m in mods if m['id'] == mod_id), None)
        
        if not mod:
            return json.dumps({"success": False, "error": "修改器未找到"}, ensure_ascii=False)
        
        try:
            # 确保目录存在
            if not os.path.exists(mods_dir):
                os.makedirs(mods_dir)
            
            # 构建文件路径
            game_dir = os.path.join(mods_dir, mod['game'])
            if not os.path.exists(game_dir):
                os.makedirs(game_dir)
            
            file_name = f"{mod['name']}.exe"
            file_path = os.path.join(game_dir, file_name)
            
            # 下载文件
            urllib.request.urlretrieve(mod['url'], file_path)
            
            # 更新修改器状态
            mod['downloaded'] = True
            mod['file_path'] = file_path
            self._save_config()
            
            return json.dumps({"success": True, "file_path": file_path}, ensure_ascii=False)
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)}, ensure_ascii=False)

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python mod_manager.py <command> <config_file> [args...]")
        sys.exit(1)
    
    command = sys.argv[1]
    config_file = sys.argv[2]
    manager = ModManager(config_file)
    
    if command == "list":
        print(manager.list_mods())
    elif command == "add":
        if len(sys.argv) < 6:
            print("Usage: python mod_manager.py add <config_file> <name> <url> <game>")
            sys.exit(1)
        name = sys.argv[3]
        url = sys.argv[4]
        game = sys.argv[5]
        print(manager.add_mod(name, url, game))
    elif command == "remove":
        if len(sys.argv) < 4:
            print("Usage: python mod_manager.py remove <config_file> <mod_id>")
            sys.exit(1)
        mod_id = int(sys.argv[3])
        print(manager.remove_mod(mod_id))
    elif command == "search":
        if len(sys.argv) < 4:
            print("Usage: python mod_manager.py search <config_file> <keyword>")
            sys.exit(1)
        keyword = sys.argv[3]
        print(manager.search_mods(keyword))
    elif command == "download":
        if len(sys.argv) < 5:
            print("Usage: python mod_manager.py download <config_file> <mod_id> <mods_dir>")
            sys.exit(1)
        mod_id = int(sys.argv[3])
        mods_dir = sys.argv[4]
        print(manager.download_mod(mod_id, mods_dir))
    else:
        print(f"Unknown command: {command}")
        sys.exit(1)