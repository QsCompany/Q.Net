using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Utils
{
    public class CommandsQuee
    {
        public static CommandsQuee Default { get; } = new CommandsQuee();
        private static Dictionary<string, Action> commands = new Dictionary<string, Action>();
        public Action this[string index]
        {
            get => commands.TryGetValue(index.ToLowerInvariant(), out var v) ? v : null;
            set => commands[index.ToLowerInvariant()] = value;
        }
        public bool Execute(string cmd, Action callback=null)
        {
            var c = this[cmd];
            if (c == null)
            {
                if (Else != null)
                    foreach (Func<string, bool> p in Else.GetInvocationList())
                        try
                        {
                            if (p(cmd)) return true;
                        }
                        catch { }
                return false;
            }
            try
            {
                c();
            }
            catch { }
            return true;
        }

        public static void Register(string cmd, Action action) => Default[cmd] = action;

        public event Func<string, bool> Else;
    }
}
