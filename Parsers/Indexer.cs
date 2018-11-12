using Common.Parsers.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Parsers
{
    public class Indexer : JObject
    {
        public bool IsStringified;
        public bool? IsReferenced;
        public int NRefs;
        public virtual int __ref__
        {
            get => (int)((JNumber)base["__ref__"]).Value;
            set => ((JNumber)base["__ref__"]).Value = value;
        }
        public Indexer(int number)
        {
            base["__ref__"] = new JNumber(number);
        }
        public override void Stringify(Context c)
        {
            var s = c.GetBuilder();
            if (!IsStringified)
            {
                s.Append("\"@ref\":");
                IsStringified = true;
            }
            //else this.NRefs++;
            s.Append("{\"__ref__\":").Append(__ref__.ToString()).Append("}");
        }


        public bool StringifyAsRef(Context c, out bool start)
        {
            var indexer = this;
            var s = c.GetBuilder();
            start = false;
            if (indexer.IsStringified)
            {
                s.Append("{\"__ref__\":").Append(__ref__.ToString()).Append("}");
                return true;
            }
            s.Append("{\"@ref\":{\"__ref__\":").Append(__ref__.ToString()).Append("}");
            IsStringified = true;
            return false;
        }

        public override void SimulateStringify(Context c)
        {
            if (IsStringified)
                NRefs++;
            else
                IsStringified = true;
        }
        public virtual void Reset(int @ref)
        {
            __ref__ = @ref;
            IsReferenced = NRefs > 0;
            IsStringified = false;
        }

    }

    sealed public class EIndexer : Indexer
    {
        public static EIndexer Value = new EIndexer();

        public override int __ref__
        {
            get { return 0; }
            set { }
        }
        private EIndexer() : base(0)
        {
        }
        public override void Stringify(Context c)
        {
        }
        public override void SimulateStringify(Context c)
        {
        }


        public new bool StringifyAsRef(Context c, out bool start)
        {
            var indexer = this;
            var s = c.GetBuilder();
            s.Append('{');
            start = true;
            return false;
        }

        public override void Reset(int @ref)
        {
            __ref__ = @ref;
            IsReferenced = false;
            IsStringified = false;
        }
    }
}
