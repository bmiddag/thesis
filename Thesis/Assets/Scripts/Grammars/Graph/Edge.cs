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

		public Edge(Graph graph, Node node1, Node node2) : base() {
			this.graph = graph;
			this.node1 = node1;
			this.node2 = node2;
			node1.addEdge(node2, this);
			node2.addEdge(node1, this);
			graph.addEdge(this);
		}

		public Node getNode1() {
			return node1;
		}

		public Node getNode2() {
			return node2;
		}

		public void destroy() {
			if (!destroyed) {
				destroyed = true;
				node1.removeEdge(node2);
				node2.removeEdge(node1);
				graph.removeEdge(this);
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
			return ((node1 == e.getNode1()) && node2 == e.getNode2()) || ((node1 == e.getNode2()) && node2 == e.getNode1());
        }

		public bool Equals(Edge e) {
			if ((object)e == null) {
				return false;
			}
			return ((node1 == e.getNode1()) && node2 == e.getNode2()) || ((node1 == e.getNode2()) && node2 == e.getNode1());
		}

		public override int GetHashCode() {
			int hash = 12345678;
			if (node1 != null) hash = node1.GetHashCode();
			if (node2 != null) hash ^= node2.GetHashCode();
			return hash;
		}
	}
}
