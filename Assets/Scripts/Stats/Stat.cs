using System;
using UnityEngine;

//数据类
[Serializable]
public class Stat
{
    [SerializeField] private float baseValue;

    [SerializeField] private float finalValue;

    public void SetBaseValue(float value)
    {
        baseValue = value;
        finalValue = value;
    }


    public float GetFinalValue()
    {
        return finalValue;
    }


}
