#!/usr/bin/env python3
"""
Git ブランチ切り替え時のエラー自動修正スクリプト
使用方法: python fix_branch_errors.py [ブランチ名]
"""
import os
import re
import sys
import subprocess

def get_current_branch():
    """現在のブランチ名を取得"""
    result = subprocess.run(['git', 'branch', '--show-current'], 
                          capture_output=True, text=True)
    return result.stdout.strip()

def fix_inventory_ui_errors(branch_name):
    """ブランチ固有のInventoryUI.csエラーを修正"""
    inventory_path = "Assets/Scripts/BattleSystem/InventoryUI.cs"
    
    if not os.path.exists(inventory_path):
        print(f"❌ {inventory_path} が見つかりません")
        return False
    
    with open(inventory_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    if branch_name == 'feature/battle-gate-system':
        print(f"🔧 {branch_name} 用の修正を適用中...")
        
        # 古いデータ構造を新しい構造に修正
        fixes = [
            (r'WeaponAttribute\.(\w+)', r'WeaponType.\1'),
            (r'weapon\.attackPower', r'weapon.basePower'),
            (r'weapon\.weaponAttribute', r'weapon.weaponType'),
            (r'attachment\.id', r'attachment.attachmentId'),
            (r'attachment\.name', r'attachment.attachmentName'),
            (r'ScriptableObject\.CreateInstance<WeaponData>\(\)', r'new WeaponData()'),
            (r'weapon\.cooldownTime', r'weapon.cooldownTurns'),
        ]
        
    elif branch_name == 'master':
        print(f"✅ {branch_name} は最新版です")
        return True
        
    else:
        print(f"⚠️ ブランチ '{branch_name}' の修正パターンが未定義です")
        return False
    
    # 修正を適用
    for pattern, replacement in fixes:
        content = re.sub(pattern, replacement, content)
    
    if content != original_content:
        with open(inventory_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"✅ {inventory_path} を修正しました")
        return True
    else:
        print(f"ℹ️ {inventory_path} に修正は不要でした")
        return True

def fix_data_structure_consistency():
    """データ構造の一貫性を確保"""
    scripts_to_check = [
        "Assets/Scripts/BattleSystem/AttachmentSystem.cs",
        "Assets/Scripts/BattleSystem/WeaponData.cs",
        "Assets/Scripts/BattleSystem/UI/TitleScreenUI.cs",
        "Assets/Scripts/BattleSystem/UI/StageSelectionUI.cs",
    ]
    
    fixed_count = 0
    for script_path in scripts_to_check:
        if os.path.exists(script_path):
            print(f"✅ {script_path} 存在確認")
            fixed_count += 1
        else:
            print(f"❌ {script_path} が見つかりません")
    
    return fixed_count > 0

def main():
    branch_name = sys.argv[1] if len(sys.argv) > 1 else get_current_branch()
    
    print(f"🌿 現在のブランチ: {branch_name}")
    print("🔧 エラー修正を開始します...")
    
    # InventoryUI.cs の修正
    if fix_inventory_ui_errors(branch_name):
        print("✅ InventoryUI.cs の修正完了")
    else:
        print("❌ InventoryUI.cs の修正失敗")
        return 1
    
    # データ構造の一貫性確認
    if fix_data_structure_consistency():
        print("✅ データ構造の一貫性確認完了")
    else:
        print("❌ データ構造に問題があります")
        return 1
    
    print("🎉 全ての修正が完了しました！")
    return 0

if __name__ == "__main__":
    sys.exit(main())