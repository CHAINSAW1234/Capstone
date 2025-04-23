using UnityEngine;
using System.Diagnostics;

public static class NullCheck
{
   
    public static bool Invoke<T>(T? obj) where T : class
    {
        if(obj == null)
        {
            LogNull<T>();
            return false;
        }

        return true;
    }

    [Conditional("UNITY_EDITOR")]
    private static void LogNull<T>()
    {   
        UnityEngine.Debug.LogWarning($"[NullCheck] {typeof(T).Name} is null.");
    }

}