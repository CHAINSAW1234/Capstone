using UnityEngine;
using System.Diagnostics;

public static class NullCheck
{
    public static bool Invoke(object? obj)
    {
        if(obj == null)
        {
            LogNull(obj);
            return false;
        }

        return true;
    }

    [Conditional("UNITY_EDITOR")]
    private static void LogNull(object? obj)
    {
        UnityEngine.Debug.LogWarning($"[NullCheck] {obj?.GetType().Name} is null.");
    }

}