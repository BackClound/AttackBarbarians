# Audio System 模块需求提示词

## 目标
实现 Audio System，统一管理背景音乐、UI 音效、战斗音效、Boss 音乐、音量设置和移动端性能。

## 当前基础
项目规则要求 Audio System，但当前脚本目录尚未看到独立 AudioManager。UI 文档中提到按钮音效通过 AudioManager 播放。

## 输出要求
- 生成 `AudioManager`：播放 BGM、SFX、UI、Loop 音效。
- 生成 `AudioConfigSO`：音频 ID、Clip、音量、Pitch、MixerGroup、是否预加载。
- 生成 `AudioChannel`：BGM、SFX、UI、Ambient、Boss。
- 生成音量设置保存：主音量、音乐、音效、UI。
- 支持对象池化 AudioSource，避免高频创建。

## 实现流程
1. GameState 驱动 BGM 切换。
2. UI 按钮、技能释放、敌人受击、Boss 出现通过事件触发 SFX。
3. 音量设置写入 SaveData。
4. 高频短音效使用 AudioSource 池。
5. 移动端限制同时播放数量，避免混音过载。

## 数据流
`GameEvents -> AudioManager -> AudioConfigSO -> AudioSourcePool -> SaveManager`

## 验收标准
- UI、战斗、Boss、结算都有独立音频入口。
- 设置界面修改音量后立即生效并可保存。
- 高频音效不会频繁创建 AudioSource。
