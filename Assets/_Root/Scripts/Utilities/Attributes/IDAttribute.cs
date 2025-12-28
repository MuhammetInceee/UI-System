using System;
using UnityEngine;

namespace UISystem.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class IDAttribute : PropertyAttribute
    {
    }
}