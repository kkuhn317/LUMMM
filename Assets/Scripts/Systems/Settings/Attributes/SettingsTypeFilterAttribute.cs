using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SettingTypeFilterAttribute : PropertyAttribute
{
    public SettingType[] AllowedValues;

    public SettingTypeFilterAttribute(params SettingType[] allowedValues)
    {
        AllowedValues = allowedValues;
    }
}