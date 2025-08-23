# Git ブランチ切り替えエラー解決ガイド

## 🚨 問題の概要
ブランチ切り替え時に異なるデータ構造によりコンパイルエラーが発生

## ✅ 解決済み
- 全フィーチャーブランチをmasterに統合
- データ構造を統一
- 重複ファイルを削除

## 🛠 今後の対処法

### 1. 自動修正スクリプト使用
```bash
python Scripts/fix_branch_errors.py [ブランチ名]
```

### 2. 手動修正パターン

#### A. WeaponData関連エラー
```csharp
// エラー
weapon.attackPower → weapon.basePower
weapon.weaponAttribute → weapon.weaponType
ScriptableObject.CreateInstance<WeaponData>() → new WeaponData()

// 正しい形
weapon.basePower = 100;
weapon.weaponType = WeaponType.Sword;
var weapon = new WeaponData();
```

#### B. AttachmentData関連エラー  
```csharp
// エラー
attachment.id → attachment.attachmentId
attachment.name → attachment.attachmentName

// 正しい形
attachment.attachmentId = 1;
attachment.attachmentName = "炎の加護";
```

### 3. ブランチ運用改善

#### 統合後の推奨運用
```bash
# 新機能開発時
git checkout master
git pull origin master
git checkout -b feature/new-feature
# 開発...
git checkout master
git merge feature/new-feature --no-ff
git push origin master
git branch -d feature/new-feature
```

#### エラー予防チェックリスト
- [ ] データ構造の一貫性確認
- [ ] 既存システムとの統合テスト  
- [ ] コンパイルエラーゼロ確認
- [ ] 重複ファイル存在確認

## 🔧 緊急時の対処

### エラーが発生した場合
1. `python Scripts/fix_branch_errors.py` を実行
2. 手動でプロパティ名を修正
3. 不要なファイルを削除
4. コンパイル確認

### それでも解決しない場合
```bash
git checkout master
git reset --hard origin/master
```

## 📝 現在の安定状態
- **master**: 最新・安定版
- **全機能統合済み**: UI、戦闘システム、ショップシステム
- **エラーゼロ**: 全コンポーネント動作確認済み

今後は master ブランチで直接開発を行うことを推奨します。