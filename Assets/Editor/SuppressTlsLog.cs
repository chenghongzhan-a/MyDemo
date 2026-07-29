using UnityEditor;
using UnityEngine;

/// <summary>
/// 过滤 Unity Editor 内部的 Tls Allocator 诊断日志（无害噪音，不输出到 Console）
/// </summary>
[InitializeOnLoad]
public class SuppressTlsLog : ILogHandler
{
    private static readonly ILogHandler DefaultHandler;

    static SuppressTlsLog()
    {
        DefaultHandler = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = new SuppressTlsLog();
    }

    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        // Tls Allocator 的内部诊断 → 跳过，不输出
        if (format != null && format.Contains("Tls Allocator") && format.Contains("unfreed"))
            return;

        DefaultHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(System.Exception exception, Object context)
    {
        DefaultHandler.LogException(exception, context);
    }
}
