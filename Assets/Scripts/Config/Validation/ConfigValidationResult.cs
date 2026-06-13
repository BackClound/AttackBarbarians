using System.Collections.Generic;
using System.Text;

/// <summary>
/// 配置校验结果集合，聚合错误与警告。
/// </summary>
public sealed class ConfigValidationResult
{
    private readonly List<string> errors = new List<string>(16);
    private readonly List<string> warnings = new List<string>(8);

    /// <summary>无错误时校验通过。</summary>
    public bool IsValid => errors.Count == 0;

    /// <summary>所有错误信息列表。</summary>
    public IReadOnlyList<string> Errors => errors;

    /// <summary>所有警告信息列表。</summary>
    public IReadOnlyList<string> Warnings => warnings;

    /// <summary>
    /// 向结果集合追加一条错误信息。
    /// </summary>
    /// <param name="source">错误来源（通常为资产名或模块名）。</param>
    /// <param name="message">错误描述文本。</param>
    public void AddError(string source, string message)
    {
        errors.Add($"[Error][{source}] {message}");
    }

    /// <summary>
    /// 向结果集合追加一条警告信息。
    /// </summary>
    /// <param name="source">警告来源（通常为资产名或模块名）。</param>
    /// <param name="message">警告描述文本。</param>
    public void AddWarning(string source, string message)
    {
        warnings.Add($"[Warning][{source}] {message}");
    }

    /// <summary>
    /// 将另一份校验结果中的错误与警告合并到当前实例。
    /// </summary>
    /// <param name="other">待合并的校验结果；为 null 时不执行任何操作。</param>
    public void Merge(ConfigValidationResult other)
    {
        if (other == null)
        {
            return;
        }

        errors.AddRange(other.errors);
        warnings.AddRange(other.warnings);
    }

    /// <summary>
    /// 将所有错误与警告格式化为可读文本报告。
    /// </summary>
    /// <returns>多行文本报告；无问题时返回"配置校验通过。"。</returns>
    public string BuildReport()
    {
        var sb = new StringBuilder(256);
        for (int i = 0; i < errors.Count; i++)
        {
            sb.AppendLine(errors[i]);
        }

        for (int i = 0; i < warnings.Count; i++)
        {
            sb.AppendLine(warnings[i]);
        }

        return sb.Length == 0 ? "配置校验通过。" : sb.ToString();
    }
}
