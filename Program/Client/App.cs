using CommonLib;
using ClientLib.Serializer;


namespace ClientLib
{
    public class App : Singleton<App>
    {
        public void Init()
        {
            Debug.DefaultDebugger = new UnityConsoleDebugger();
            MessageProcessor.DefaultSerializer = new MsgPackBitSerializer();
        }

        public void UnInit()
        {

        }
    }
}
