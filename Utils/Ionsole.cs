using System;

namespace Common.Utils
{
    public delegate void WriteMethod(string s);
    public static class Ionsole
    {
        static readonly TaskQueue<string> pipe = new TaskQueue<string>(OnSuccess, OnError);
        public static event WriteMethod OnWrite;
        private static void OnError(Operation<string> value, Exception e)
        {

        }

        private static void OnSuccess(Operation<string> value)
        {
            OnWrite?.Invoke(value.Value);
        }
        private class WriteLn
        {
            public string s;
            public object[] args;
            public object un;

            public WriteLn(string s, object[] args)
            {
                this.s = s;
                this.args = args;
            }
            public WriteLn(object un)
            {
                this.un = un;
            }

        }


        public static void WriteLine(object s)
        {
            pipe.Add((s ?? "").ToString() + Environment.NewLine);
        }
        public static System.Reflection.MethodInfo format =  typeof(string).GetMethod(nameof(string.Format),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public, null,
            new Type[] { typeof(string), typeof(object[]) }, null);

        //public static void WriteLine(string s, params object[] args)
        //{
        //    if (args.Length == 0)
        //        pipe.Add((s ?? "").ToString() + Environment.NewLine);
        //    var x = new System.Collections.Generic.List<object>() { s };
        //    x.AddRange(args);

        //    WriteLine(format.Invoke(null, new object[] { s, x.ToArray() }));
        //}
        public static void Write(string s)
        {
            pipe.Add(s ?? "");
        }
        public static int BufferWidth = 100;
        public static void WriteSeparator(string v, char sp = '*')
        {
            var s = (BufferWidth - v.Length - 6) / 2;
            var vv = s <= 0 ? "" : new String(sp, s);
            pipe.Add(vv + "   " + v + "   " + vv + Environment.NewLine);
        }
        public static void WriteSeparator(char sp = '*')
        {
            pipe.Add(new string(sp, BufferWidth) + Environment.NewLine);
        }
        public static void WriteSeparator(params object[] sp)
        {
            foreach (var p in sp)
            {
                if (p is char) WriteSeparator((char)p);
                else WriteSeparator(p?.ToString() ?? "");
            }
        }

        public static void Write(Exception e)
        {
            WriteSeparator();
            var t = e;
            do
            {
                WriteLine(e.Message);
                t = t.InnerException;
                WriteSeparator('%');
                WriteLine(t.StackTrace);
                WriteSeparator('%');

            } while (t != null);
            
            WriteSeparator();
        }
    }
}