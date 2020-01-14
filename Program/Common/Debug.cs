using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib
{
    public interface IDebugger
    {
        void LogFormat(string format, object[] args);
        void LogWarningFormat(string format, object[] args);
        void LogErrorFormat(string format, object[] args);
    }

    public static class Debug
    {
        public static IDebugger DefaultDebugger { get; set; }

        public static void LogFormat(string format, params object[] args)
        {
            DefaultDebugger.LogFormat("[{0}] {1}", new object[] { DateTime.Now, string.Format(format, args) });
        }
        public static void LogWarningFormat(string format, params object[] args)
        {
            DefaultDebugger.LogWarningFormat("[{0}] {1}", new object[] { DateTime.Now, string.Format(format, args) });
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            DefaultDebugger.LogErrorFormat("[{0}] {1}", new object[] { DateTime.Now, string.Format(format, args) });
        }
    }
}
