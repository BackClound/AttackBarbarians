using System;

/// <summary>
/// 玩家设置：音量、画质、语言、触控偏好。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。嵌套在 <see cref="SaveData"/> 中持久化。</para>
/// </remarks>
[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float uiVolume = 1f;

    /// <summary>-1 表示跟随系统默认画质档位。</summary>
    public int graphicsQualityLevel = -1;

    public string languageCode = "zh-CN";
    public bool invertTouchY;
    public bool hapticEnabled = true;

    /// <summary>0=简单, 1=普通, 2=困难；影响局末金币/钻石结算倍率。</summary>
    public int gameDifficulty = 1;

    /// <summary>创建默认玩家设置实例。</summary>
    /// <returns>音量、画质与语言等均为默认值的设置对象。</returns>
    public static SettingsData CreateDefault() => new SettingsData();
}
