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

        public static void Log(string log)
        {
            DefaultDebugger?.LogFormat("[{0}] {1}", new object[] { DateTime.Now, log });
        }

        public static void LogFormat(string format, params object[] args)
        {
            DefaultDebugger?.LogFormat("[{0}] {1}", new object[] { DateTime.Now, string.Format(format, args) });
        }

        public static void LogWarning(string warn)
        {
            DefaultDebugger?.LogWarningFormat("[{0}] {1}", new object[] { DateTime.Now, warn });
        }

        internal static void LogException(Exception exception)
        {
            throw new NotImplementedException();
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            DefaultDebugger?.LogWarningFormat("[{0}] {1}", new object[] { DateTime.Now, string.Format(format, args) });
        }

        public static void LogError(string error)
        {
            DefaultDebugger?.LogErrorFormat("[{0}] {1}", new object[] { DateTime.Now, error });
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            DefaultDebugger?.LogErrorFormat("[{0}] {1}", new object[] { DateTime.Now, string.Format(format, args) });
        }
    }
}
