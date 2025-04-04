using System;
using UnityEngine;

public static class Log
{
    public static void Warning(string msg)
    {
#if UNITY_EDITOR
        Debug.LogWarning(msg);
#endif
    }

    public static void Info(string msg)
    {
#if UNITY_EDITOR
        Debug.Log(msg);
#endif
    }

    public static void Error(string msg)
    {
#if UNITY_EDITOR
        Debug.LogError(msg);
#endif
    }

    public static void Error(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }

    public static void Exception(Exception exception)
    {
#if UNITY_EDITOR
        Debug.LogException(exception);
#endif
    }

}
