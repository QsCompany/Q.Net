using System;
using System.IO;
using static Server.Setup.MysqlInstance;
namespace Common.Idea
{
    [Serializable]
    public class Graph
    {
        public static GraphGroup operator +(Graph h, Graph g)
        {
            if (h is GraphGroup a) return a + g;
            else if (g is GraphGroup b) return h + b;
            return new GraphGroup(h, g);
        }
        public static GraphGroup operator +(Graph h, Action g) => h + new Graph(g);
        public static implicit operator Graph(Action action) => new Graph(action);
        public virtual Cache Cache { get; set; }
        public virtual bool Success { get; set; }
        public virtual Action Action { get; }
        public virtual object Result { get; set; }
        public virtual GraphGroup Finaly { get; set; }
        public virtual GraphGroup IfSuccess { get; set; }
        public virtual GraphGroup IfFail { get; set; }
        public virtual bool IsCacheable { get; set; }

        public virtual bool Checked { get; set; }

        public virtual void Execute(Graph parent, Graph previous, Scop scop)
        {
            if (!Checked && !(Success && IsCacheable))
            {
                if (Action == null) return;
                Success = Action(this, new GraphEventArgs(parent, previous, scop));
                if (Success)
                    IfSuccess?.Execute(this, this, scop);
                else IfFail?.Execute(this, this, scop);
            }
            Finaly?.Execute(parent, this, scop);
        }
        public Graph() { }
        public Graph(Action action, string name = null) => Action = action;
        public Graph SetName(string name) { Name = name; return this; }
        public string Name { get; set; }
        public Graph Then(Graph graph)
        {
            IfSuccess += graph;
            return this;
        }
        public Graph Catch(Graph graph)
        {
            IfFail += graph;
            return this;
        }
        public Graph ContinueWith(Graph graph)
        {
            Finaly += graph;
            return this;
        }


        public Graph Then(Action graph)
        {
            IfSuccess += graph;
            return this;
        }
        public Graph Catch(Action graph)
        {
            IfFail += graph;
            return this;
        }
        public Graph ContinueWith(Action graph)
        {
            Finaly += graph;
            return this;
        }

        public static void testc()
        {
            object c1 = null;
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter1 = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            try
            {
                using (var f = File.OpenRead("test.bin"))
                    c1 = formatter1.Deserialize(f);
                ((Graph)c1).Execute(null, null, null);
            }
            catch
            {

            }
            var c = ((Graph)((Graph sndr, GraphEventArgs e) =>
            {
                return true;
            }))
            .Then((s, e) => { return true; })
            .Catch((s, e) => { return true; })
            .ContinueWith((s, e) => { return true; })
            .ContinueWith((s, e) => { return true; })
            ;

            c += c;
            c.Execute(null, null, new Scop());
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var f = File.OpenWrite("test.bin"))
                formatter.Serialize(f, c);
            
        }
    }


    public class GraphEventArgs
    {
        public Graph Parent; public Graph Previous; public Scop Scop;
        [System.Diagnostics.DebuggerNonUserCode]
        public GraphEventArgs(Graph parent, Graph previous, Scop scop)
        {
            Parent = parent; Previous = previous; Scop = scop;
        }
        //public GraphEventArgs(Graph current,GraphEventArgs e)
        //{
        //    Parent = parent; Previous = previous; Scop = scop;
        //}
    }

    [Serializable]
    public class Scop
    {

    }
    [Serializable]
    public class Cache
    {

    }
    public delegate bool Action(Graph current, GraphEventArgs e);
    [Serializable]
    public class RefGraph : Graph
    {

    }
    [Serializable]
    public class GraphGroup : Graph
    {
        public readonly List<Graph> graphs = new List<Graph>();
        public override Action Action => EnrolleActions;
        public override bool IsCacheable { get => false; set { } }

        public static GraphGroup operator +(GraphGroup g, Graph h)
        {
            if (g == null && h == null) return null;
            if (g == null) return new GraphGroup(h);
            g.graphs.Add(h);
            return g;
        }
        public static GraphGroup operator +(Graph h, GraphGroup g)
        {
            if (g == null) return new GraphGroup(h);
            if (h == null) return g;
            g.graphs.Insert(0, h);
            return g;
        }
        public static GraphGroup operator -(GraphGroup g, Graph h)
        {
            if (h == null || g == null) return g;
            g.graphs.Remove(h);
            return g;
        }
        private bool EnrolleActions(Graph current, GraphEventArgs e)
        {
            var p = e.Previous;
            foreach (var g in graphs)
            {
                g.Execute(e.Parent, p, e.Scop);
                p = g;
            }
            return true;
        }
        public GraphGroup(params Graph[] graphs)
        {
            foreach (var g in graphs)
                if (g != null)
                    this.graphs.Add(g);
        }
    }

}