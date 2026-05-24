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

## 验收标准补充
- 本模块完成后必须形成最小可运行闭环，可以在当前 Unity 场景中通过手动挂载或现有入口验证核心流程。
- 相关配置、运行时数据、事件发布和事件订阅必须有明确入口，异常或缺失配置时要给出可定位的问题表现。
- 高频逻辑不得引入明显 GC 分配；涉及生成、销毁、特效、投射物、敌人或 UI 飘字时必须优先接入对象池。
- 完成实现后必须说明受影响脚本、Prefab/Scene 挂载要求、测试步骤和未迁移的旧逻辑边界。

## 模块依赖边界
- 本模块只能依赖已完成或同阶段明确约定的公共接口、配置数据和事件 Key，不直接依赖后续阶段的具体实现类。
- 跨模块通信优先通过 `EventBus`、`IGameSystem`、`ServiceLocator`、ScriptableObject 配置或明确的 Controller API 完成。
- 禁止从低层模块反向引用高层模块；例如 Damage、Pool、Config 不应依赖 UI、Upgrade、Shop 等外围系统。
- 禁止在核心逻辑中散落 `FindObjectOfType`、硬编码场景路径或直接访问无关单例；必须通过启动流程、序列化引用或服务注册注入依赖。
- 数据结构与事件 Payload 必须保持小而稳定，避免为了单个功能暴露整个 Manager 或运行时对象内部状态。

## 旧逻辑兼容与迁移约束
- 保持当前可运行逻辑，不一次性删除或重写现有 `Player`、`Enemy`、`SkillShoot`、`EnemyGenerateManager` 等已在场景中使用的脚本。
- 新系统先以适配器、旁路入口或可切换开关接入；确认新流程可运行后，再逐步迁移旧职责。
- 每次迁移只替换一个清晰职责，例如目标选择、伤害结算、对象生成、UI 刷新或配置读取，避免跨系统大改。
- 迁移期间必须保持旧 Prefab、Animator、Collider、Layer、Tag 和 Inspector 序列化字段不失效。
- 删除旧代码前必须确认没有场景引用、Prefab 引用和运行时调用；无法确认时保留兼容层并标注后续清理任务。

