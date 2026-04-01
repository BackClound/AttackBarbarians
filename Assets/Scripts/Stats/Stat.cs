using System;
using UnityEngine;

//数据类
[Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    [SerializeField] private bool shouldUpdate;

    private float finalValue;

    public void SetBaseValue(float value)
    {
        baseValue = value;
        finalValue = value;
    }

    public float GetValue()
    {
        finalValue = GetFinalValue();

        return finalValue;
    }


    private float GetFinalValue()
    {
        finalValue = baseValue;

        return finalValue;
    }


}
