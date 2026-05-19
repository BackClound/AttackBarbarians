/// <summary>
/// 存档模块常量：版本号、文件名、备份后缀。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。静态类。</para>
/// </remarks>
public static class SaveConstants
{
    public const int CurrentVersion = 1;
    public const string DefaultSaveFileName = "save.json";
    public const string BackupSuffix = ".bak";
    public const string ExportFileName = "save_export.json";
}
