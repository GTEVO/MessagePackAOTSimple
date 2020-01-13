using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib
{
    public class Singleton<T> where T : new()
    {
        private static T _instacne;

        public static T Instacne
        {
            get {
                if (_instacne == null) {
                    _instacne = new T();
                }
                return _instacne;
            }
        }
    }
}
