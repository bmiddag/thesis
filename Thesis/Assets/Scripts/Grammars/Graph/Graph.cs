using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class Graph : AttributedElement {
		HashSet<Node> nodes;
		HashSet<Edge> edges;

		public Graph() : base() {
			nodes = new HashSet<Node>();
			edges = new HashSet<Edge>();
		}

		public HashSet<Node> getNodes() {
			return nodes;
		}

		public HashSet<Edge> getEdges() {
			return edges;
		}

		// ************************** SET MANAGEMENT ************************** \\
		// The following code only adds and removes elements to/from sets.
		// Element creation should be handled outside of this class.
		public void addNode(Node node) {
			if (nodes.Count == 0) node.setActive(true); // TODO: Move this
			nodes.Add(node);
			ICollection<Edge> edgeList = node.getEdges().Values;
			foreach (Edge edge in edgeList) {
				if (!edges.Contains(edge)) addEdge(edge);
			}
		}

		public void addEdge(Edge edge) {
			edges.Add(edge);
			if (!nodes.Contains(edge.getNode1())) addNode(edge.getNode1());
			if (!nodes.Contains(edge.getNode2())) addNode(edge.getNode2());
		}

		public void removeEdge(Edge edge) {
			if (edge != null & edges.Contains(edge)) {
				edges.Remove(edge);
			}
		}

		public void removeNode(Node node) {
			if (node != null) {
				nodes.Remove(node);
				ICollection<Edge> nodeEdges = node.getEdges().Values;
				foreach (Edge edge in nodeEdges) {
					removeEdge(edge);
				}
			}
		}
	}
}
