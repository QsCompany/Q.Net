using Common.Services;
using Common.Utils;

namespace Common.Models
{
    public delegate void CommandCallback(TaskQueue<CommandsParam> queue, CommandsParam param);
    public class CommandsParam
    {
        public CommandsParam(RequestArgs args, CommandCallback callback, object param)
        {
            Args = args;
            Parms = param;
            Callback = callback;
        }
        public RequestArgs Args;
        public object Parms;
        public CommandCallback Callback;
    }

}
