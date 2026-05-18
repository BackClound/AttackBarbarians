---
description: "Unity C# 代码编写规范"
globs: ["Assets/Scripts/**/*.cs"]
alwaysApply: false
---

# C# 代码规范

## 文件结构
每个 C# 脚本文件按以下顺序组织：
1. using 语句
2. namespace 声明（可选，推荐使用）
3. 类声明（public）
4. 常量（const）和只读字段（readonly）
5. 序列化字段（`[SerializeField]` private）
6. 公共属性（Properties）
7. 私有字段
8. Unity 生命周期方法（Awake、OnEnable、Start、Update、FixedUpdate、LateUpdate、OnDisable、OnDestroy）
9. 公共方法
10. 私有方法
11. 事件回调方法

## 示例模板
```csharp
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// 游戏管理器，控制游戏的主循环状态和全局事件。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // 常量与只读字段
        private const float GAME_OVER_DELAY = 2f;
        private readonly string PLAYER_PREFS_KEY = "HighScore";

        // 序列化字段
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        
        [Header("Settings")]
        [SerializeField] private float waveDelayBetween = 2f;

        // 公共属性
        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; }

        // 私有字段
        private int currentWave;

        // Unity 生命周期
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        // 公共方法
        public void StartNewGame() { }

        // 私有方法
        private void Initialize() { }
    }
}