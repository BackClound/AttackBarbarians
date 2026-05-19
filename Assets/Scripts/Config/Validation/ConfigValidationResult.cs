using System.Collections.Generic;
using System.Text;

/// <summary>
/// 配置校验结果集合，聚合错误与警告。
/// </summary>
public sealed class ConfigValidationResult
{
    private readonly List<string> errors = new List<string>(16);
    private readonly List<string> warnings = new List<string>(8);

    public bool IsValid => errors.Count == 0;
    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<string> Warnings => warnings;

    public void AddError(string source, string message)
    {
        errors.Add($"[Error][{source}] {message}");
    }

    public void AddWarning(string source, string message)
    {
        warnings.Add($"[Warning][{source}] {message}");
    }

    public void Merge(ConfigValidationResult other)
    {
        if (other == null)
        {
            return;
        }

        errors.AddRange(other.errors);
        warnings.AddRange(other.warnings);
    }

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
