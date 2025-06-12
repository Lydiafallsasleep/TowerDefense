# 主菜单场景设置指南

## 创建主菜单场景

1. 在Unity编辑器中，选择 `File > New Scene` 创建新场景
2. 将场景保存为 `MainMenu`
3. 确保将场景添加到构建设置中 (`File > Build Settings`)，并将其设置为索引0（第一个场景）

## 设置画布

1. 创建UI画布：`GameObject > UI > Canvas`
2. 设置Canvas Scaler为 `Scale With Screen Size`，参考分辨率设为 `1920 x 1080`
3. 添加 `EventSystem`（如果没有自动添加）

## 创建UI元素

### 背景
1. 在Canvas下创建一个Image：`GameObject > UI > Image`
2. 将其命名为 `Background`
3. 设置Rect Transform填满整个画布
4. 添加背景图片或设置颜色

### 标题
1. 在Canvas下创建一个TextMeshPro文本：`GameObject > UI > Text - TextMeshPro`
2. 将其命名为 `Title`
3. 设置文本为 `塔防游戏`
4. 调整字体大小和位置（建议放在画面上方）

### 按钮
1. 创建开始游戏按钮：`GameObject > UI > Button - TextMeshPro`
2. 将其命名为 `StartButton`
3. 设置按钮文本为 `开始游戏`
4. 调整位置和大小

5. 创建设置按钮：`GameObject > UI > Button - TextMeshPro`
6. 将其命名为 `SettingsButton`
7. 设置按钮文本为 `设置`
8. 调整位置和大小

9. 创建退出按钮：`GameObject > UI > Button - TextMeshPro`
10. 将其命名为 `QuitButton`
11. 设置按钮文本为 `退出游戏`
12. 调整位置和大小

### 最高分显示
1. 创建一个TextMeshPro文本：`GameObject > UI > Text - TextMeshPro`
2. 将其命名为 `HighScoreText`
3. 设置文本为 `最高分数: 0`
4. 调整位置（建议放在画面下方）

### 设置面板
1. 创建一个Panel：`GameObject > UI > Panel`
2. 将其命名为 `SettingsPanel`
3. 添加背景图片或设置颜色
4. 在Panel中添加各种设置选项（如音量滑块、重置最高分按钮等）
5. 添加关闭按钮

## 添加主菜单控制器

1. 将 `MainMenuController.cs` 脚本添加到Canvas或空游戏对象上
2. 在Inspector中设置引用：
   - Start Game Button: 拖放 `StartButton`
   - Settings Button: 拖放 `SettingsButton`
   - Quit Button: 拖放 `QuitButton`
   - High Score Text: 拖放 `HighScoreText`
   - Settings Panel: 拖放 `SettingsPanel`
   - Game Scene Name: 设置为您的游戏场景名称（例如 `GameScene`）
   - 如果有音效，设置Button Click Sound和Background Music

## 构建设置

1. 打开构建设置：`File > Build Settings`
2. 将MainMenu场景添加为第一个场景（索引0）
3. 将游戏主场景添加为第二个场景（索引1）
4. 确保场景顺序正确

## 测试

1. 在编辑器中运行MainMenu场景
2. 测试所有按钮功能：
   - 开始游戏按钮应该加载游戏场景
   - 设置按钮应该显示/隐藏设置面板
   - 退出按钮应该退出游戏（在编辑器中会停止播放）
   - 如果添加了重置最高分按钮，测试它是否正常工作

## 注意事项

1. 确保所有UI元素在不同分辨率下都能正确显示
2. 添加适当的过渡动画可以提升用户体验
3. 考虑添加音效反馈以增强交互体验
4. 如果游戏有多个难度级别，可以在主菜单中添加难度选择选项 