using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Utils
{
    public class FastArray : IDisposable
    {
        private static Stack<FastArray> store = new Stack<FastArray>(25);
        public static FastArray New(int capacity)
        {
            lock (store)
            {
                if (store.Count == 0)
                    return new FastArray(capacity);
                var s = store.Pop();
                s.capacity = capacity;
                return s;
            }
        }

        private byte[] _list;
        private int capacity;
        private int index;
        private FastArray(int capacity)
        {
            _list = new byte[capacity];
            this.capacity = capacity;
        }
        private void check(int len)
        {
            if (index + len < _list.Length) return;
            var x = new byte[len + capacity];
            Array.Copy(_list, x, index);
            _list = x;
        }
        public void AddRange(byte[] array, int len)
        {
            check(len + index);
            Array.Copy(array, 0, _list, index, len);
            index += len;
        }
        public byte[] ToArray()
        {
            var x = new byte[index];
            Array.Copy(_list, x, index);
            return x;
        }
        public void Dispose()
        {
            index = 0;
            lock (store)
            {
                store.Push(this);
            }
        }
    }
}
