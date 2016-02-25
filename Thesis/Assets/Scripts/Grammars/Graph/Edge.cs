using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class Edge : AttributedElement {
		Graph graph;
		Node node1;
		Node node2;
		bool destroyed = false;
        bool directed = false;

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
				destroyed = true;
				node1.RemoveEdge(node2);
                node2.RemoveEdge(node1);
				graph.RemoveEdge(this);
			}
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
			int hash = 12345678;
			if (node1 != null) hash = node1.GetHashCode();
			if (node2 != null) hash ^= node2.GetHashCode();
            //if (directed) hash++;
			return hash;
		}
	}
}
