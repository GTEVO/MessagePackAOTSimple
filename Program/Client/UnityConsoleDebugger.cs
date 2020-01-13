using CommonLib;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace ClientLib
{
    public class UnityConsoleDebugger : IDebugger
    {
        void IDebugger.LogErrorFormat(string format, object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }

        void IDebugger.LogFormat(string format, object[] args)
        {
            Debug.LogFormat(format, args);
        }

        void IDebugger.LogWarningFormat(string format, object[] args)
        {
            Debug.LogWarningFormat(format, args);
        }
    }
}
