using UnityEngine;

/// <summary>
/// 波次成长曲线纯函数计算器（无 MonoBehaviour 依赖）。
/// </summary>
public static class WaveProgressionCalculator
{
    /// <summary>计算当前等级升级所需经验。</summary>
    public static float GetNeedExperience(
        int level,
        int wave,
        WaveProgressionConfigSO cfg,
        float difficultyExpMult)
    {
        if (cfg == null)
        {
            return 100f;
        }

        level = Mathf.Max(1, level);
        wave = Mathf.Max(1, wave);
        float mult = Mathf.Max(0.01f, difficultyExpMult);

        float baseNeed = cfg.ExpBase * Mathf.Pow(level, cfg.ExpGrowthPower) *
                         Mathf.Exp(cfg.ExpLambda * Mathf.Max(0, level - cfg.ExpLambdaStartLevel));
        float waveNeed = 1f + cfg.NeedWaveCoeff * Mathf.Pow(wave - 1f, cfg.NeedWavePower);
        return Mathf.Max(1f, baseNeed * waveNeed * mult);
    }

    /// <summary>计算指定属性的波次乘法倍率。</summary>
    public static float GetStatMultiplier(StatType statType, int wave, WaveProgressionConfigSO cfg)
    {
        if (cfg == null)
        {
            return 1f;
        }

        WaveStatCurve curve = cfg.GetCurveForStat(statType);
        if (curve.IsAdditive)
        {
            return 1f;
        }

        return EvaluateMultiplicativeCurve(wave, curve);
    }

    /// <summary>计算指定属性的波次加法增量（暴击类）。</summary>
    public static float GetStatAdditiveBonus(StatType statType, int wave, WaveProgressionConfigSO cfg)
    {
        if (cfg == null)
        {
            return 0f;
        }

        WaveStatCurve curve = cfg.GetCurveForStat(statType);
        if (!curve.IsAdditive)
        {
            return 0f;
        }

        wave = Mathf.Max(1, wave);
        float bonus = curve.Coefficient * Mathf.Pow(wave - 1f, curve.Power);
        return Mathf.Min(bonus, curve.AdditiveCap);
    }

    /// <summary>计算击杀经验（含波次与敌种权重）。</summary>
    public static int GetKillExperience(
        int baseExp,
        int wave,
        float typeWeight,
        WaveProgressionConfigSO cfg,
        float difficultyGainMult)
    {
        if (baseExp <= 0 || cfg == null)
        {
            return 0;
        }

        wave = Mathf.Max(1, wave);
        float mult = 1f + cfg.ExpWaveCoeff * Mathf.Pow(wave - 1f, cfg.ExpWavePower);
        mult *= Mathf.Max(0.01f, typeWeight);
        mult *= Mathf.Max(0.01f, difficultyGainMult);
        return Mathf.Max(1, Mathf.RoundToInt(baseExp * mult));
    }

    /// <summary>计算有效刷怪间隔（秒）。</summary>
    public static float GetSpawnInterval(int wave, WaveProgressionConfigSO cfg, float mapMult)
    {
        if (cfg == null)
        {
            return 1.5f * mapMult;
        }

        wave = Mathf.Max(1, wave);
        float interval = cfg.SpawnIntervalBase * Mathf.Pow(cfg.SpawnIntervalDecay, wave - 1);
        interval = Mathf.Max(cfg.SpawnIntervalMin, interval);
        return interval * Mathf.Max(0.01f, mapMult);
    }

    /// <summary>计算单波最大刷怪数。</summary>
    public static int GetMaxSpawnCount(int wave, WaveProgressionConfigSO cfg, float mapMult)
    {
        if (cfg == null)
        {
            return Mathf.Max(1, Mathf.RoundToInt(20 * mapMult));
        }

        wave = Mathf.Max(1, wave);
        int count = cfg.SpawnCountBase + cfg.SpawnCountPerWave * (wave - 1);
        count = Mathf.Min(cfg.SpawnCountMax, count);
        return Mathf.Max(1, Mathf.RoundToInt(count * Mathf.Max(0.01f, mapMult)));
    }

    /// <summary>计算波次持续时间（秒）。</summary>
    public static float GetWaveDuration(int wave, WaveProgressionConfigSO cfg)
    {
        if (cfg == null)
        {
            return 30f;
        }

        wave = Mathf.Max(1, wave);
        int steps = (wave - 1) / 5;
        float duration = cfg.WaveDurationBase + steps * cfg.WaveDurationStep;
        return Mathf.Clamp(duration, cfg.WaveDurationBase, cfg.WaveDurationMax);
    }

    /// <summary>随波次递增的精英生成概率。</summary>
    public static float GetEliteSpawnChance(int wave, WaveProgressionConfigSO cfg, float baseChance)
    {
        if (cfg == null)
        {
            return baseChance;
        }

        wave = Mathf.Max(1, wave);
        return Mathf.Clamp(baseChance + cfg.EliteChancePerWave * (wave - 1), 0f, cfg.EliteChanceMax);
    }

    /// <summary>随波次递增的特殊怪生成概率。</summary>
    public static float GetSpecialSpawnChance(int wave, WaveProgressionConfigSO cfg, float baseChance)
    {
        if (cfg == null)
        {
            return baseChance;
        }

        wave = Mathf.Max(1, wave);
        return Mathf.Clamp(baseChance + cfg.SpecialChancePerWave * (wave - 1), 0f, cfg.SpecialChanceMax);
    }

    private static float EvaluateMultiplicativeCurve(int wave, WaveStatCurve curve)
    {
        wave = Mathf.Max(1, wave);
        float value = 1f + curve.Coefficient * Mathf.Pow(wave - 1f, curve.Power);
        if (curve.MaxMultiplier > 0f)
        {
            value = Mathf.Min(value, curve.MaxMultiplier);
        }

        return Mathf.Max(1f, value);
    }
}
