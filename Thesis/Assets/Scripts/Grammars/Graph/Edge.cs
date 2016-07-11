using System;

namespace Grammars.Graphs {
    public class Edge : AttributedElement {
		Graph graph;
		Node node1;
		Node node2;
		bool destroyed = false;
        bool directed = false;

        public override IElementContainer Container {
            get { return graph; }
        }

        public override string LinkType {
            get { return graph.LinkType; }
            set { }
        }

        public Edge(Graph graph, Node node1, Node node2, bool directed = false) : base() {
			this.graph = graph;
			this.node1 = node1;
			this.node2 = node2;
            this.directed = directed;
			node1.AddEdge(node2, this);
			node2.AddEdge(node1, this);
			graph.AddEdge(this);
		}

		public Node GetNode1() {
			return node1;
		}

		public Node GetNode2() {
			return node2;
		}

        public bool IsDirected() {
            return directed;
        }

        public void MakeUndirected() {
            if (directed) {
                directed = false;
                OnAttributeChanged(EventArgs.Empty);
            }
        }

		public void Destroy() {
			if (!destroyed) {
                SetAttribute("_grammar_destroyed", "true");
				destroyed = true;
				node1.RemoveEdge(node2);
                node2.RemoveEdge(node1);
				graph.RemoveEdge(this);
			}
		}

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null) {
                switch (key) {
                    case "_type":
                        result = "edge"; break;
                    case "_directed":
                        result = directed.ToString(); break;
                }
            }
            return result;
        }

        public override object GetObjectAttribute(string key) {
            object result = base.GetObjectAttribute(key);
            if (result == null && key != null) {
                switch (key) {
                    case "_node1":
                        result = node1; break;
                    case "_node2":
                        result = node2; break;
                }
            }
            return result;
        }

        // ************************** EQUALITY TESTING ************************** \\
        public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}
			Edge e = obj as Edge;
			if ((object)e == null) {
				return false;
			}
            //if (directed != e.IsDirected()) return false;
			return ((node1 == e.GetNode1()) && node2 == e.GetNode2()) || ((node1 == e.GetNode2()) && node2 == e.GetNode1());
        }

		public bool Equals(Edge e) {
			if ((object)e == null) {
				return false;
			}
            //if (directed != e.IsDirected()) return false;
            return ((node1 == e.GetNode1()) && node2 == e.GetNode2()) || ((node1 == e.GetNode2()) && node2 == e.GetNode1());
		}

		public override int GetHashCode() {
            unchecked {
                int hash = 17;
                if (node1 != null) hash = hash * 31 + node1.GetHashCode();
                if (node2 != null) hash = hash * 31 + node2.GetHashCode();
                //if (directed) hash++;
                return hash;
            }
		}

        public bool EqualsOtherGraphEdge(Edge e, bool thisAttribute = false, bool otherAttribute = false) {
            if ((object)e == null) {
                return false;
            }
            if (directed != e.IsDirected()) return false;
            if (thisAttribute && (!node1.HasAttribute("_grammar_query_id") || !node2.HasAttribute("_grammar_query_id"))) return false;
            if (otherAttribute && (!e.GetNode1().HasAttribute("_grammar_query_id") || !e.GetNode2().HasAttribute("_grammar_query_id"))) return false;
            int thisID1 = thisAttribute ? int.Parse(node1.GetAttribute("_grammar_query_id")) : node1.GetID();
            int thisID2 = thisAttribute ? int.Parse(node2.GetAttribute("_grammar_query_id")) : node2.GetID();
            int otherID1 = otherAttribute ? int.Parse(e.GetNode1().GetAttribute("_grammar_query_id")) : e.GetNode1().GetID();
            int otherID2 = otherAttribute ? int.Parse(e.GetNode2().GetAttribute("_grammar_query_id")) : e.GetNode2().GetID();
            return (thisID1 == otherID1 && thisID2 == otherID2) || (thisID1 == otherID2 && thisID2 == otherID1 && !directed);
        }
	}
}
