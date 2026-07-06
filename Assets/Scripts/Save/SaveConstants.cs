/// <summary>
/// 存档模块常量：版本号、文件名、备份后缀。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。静态类。</para>
/// </remarks>
public static class SaveConstants
{
    /// <summary>当前存档 schema 版本号。</summary>
    public const int CurrentVersion = 4;

    /// <summary>主存档默认文件名。</summary>
    public const string DefaultSaveFileName = "save.json";

    /// <summary>备份文件后缀。</summary>
    public const string BackupSuffix = ".bak";

    /// <summary>导出存档默认文件名。</summary>
    public const string ExportFileName = "save_export.json";
}
