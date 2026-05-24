七、AI 长上下文管理规则（非常重要）

Cursor 最大问题：
长时间开发后上下文污染。

解决方案：

规则1：模块化开发
永远不要：
“帮我开发整个游戏”

必须：
“帮我开发 Buff System”

规则2：每个系统单独文档
例如：
skill_system.md
buff_system.md
enemy_system.md

规则3：固定架构
架构一旦确定：
禁止AI随意修改。

规则4：使用 AI Summary
每开发完成一个系统：
让AI生成：
系统总结
API说明
数据流
扩展点

下次继续开发时先喂给AI。

规则5：不要一次生成超大代码
错误示范：
“生成整个战斗系统”
正确：
先Enemy
再Projectile
再Damage
再Buff