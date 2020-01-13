using System;

namespace CommonLib
{
    public interface ITimer
    {
        bool Active { get; set; }
        event Action<int> OnCall;
    }

    public interface ITimerFactory
    {
        ITimer Create();
    }
}
