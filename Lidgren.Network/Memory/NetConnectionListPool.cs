using System;
using System.Collections.Generic;

namespace Lidgren.Network
{
    public static class NetConnectionListPool
    {
        private static Stack<List<NetConnection>> _pool = new Stack<List<NetConnection>>();

        public static List<NetConnection> Rent()
        {
            lock (_pool)
            {
                if (_pool.Count > 0)
                    return _pool.Pop();
            }
            return new List<NetConnection>();
        }

        public static void Return(List<NetConnection> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            lock (_pool)
            {
                list.Clear();
                if (_pool.Count < 64)
                    _pool.Push(list);
            }
        }
    }
}
