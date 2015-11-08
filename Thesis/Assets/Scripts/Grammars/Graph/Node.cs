using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class Node : AttributedElement {
		Graph graph;
		IDictionary<Node, Edge> edges;
		string name;
		bool active = false;

		public Node(Graph graph, string name) : base() {
			this.graph = graph;
			edges = new Dictionary<Node, Edge>();
			this.name = name;
			graph.addNode(this);
		}

		public Edge addEdge(Node node, IDictionary<string, string> attributes = null) {
			if (node == null) return null;
			Edge edge;
			if (edges.ContainsKey(node)) {
				edge = edges[node];
			} else {
				edge = new Edge(graph, this, node); // Edge is added to both nodes' edge dictionary, as well as to the graph
			}
			edge.setAttributes(attributes);
			if (isActive() && !node.isActive()) {
				node.setActive(true);
			} else if (!isActive() && node.isActive()) {
				setActive(true);
			}
			return edge;
		}

		public void addEdge(Node node, Edge edge) {
			if (edge != null) {
				edges[node] = edge;
			}
		}

		public void removeEdge(Node node) {
			if (node != null && edges.ContainsKey(node)) {
				Edge edge = edges[node];
				edge.destroy();
				edges.Remove(node);
			}
		}

		public void removeEdge(Edge edge) {
			Node node = edges.FirstOrDefault(x => x.Value == edge).Key;
			removeEdge(node);
		}

		public IDictionary<Node, Edge> getEdges() {
			return edges;
		}

		public void destroy() {
			List<KeyValuePair<Node, Edge>> edgeList = edges.ToList();
			foreach (KeyValuePair<Node, Edge> entry in edges) {
				entry.Value.destroy();
			}
			active = false;
			graph.removeNode(this);
		}

		// ************************** ACTIVE STATE ************************** \\
		public bool isActive() {
			return active;
		}

		public void setActive(bool active) {
			this.active = active;
			spreadActive();
		}

		/// <summary>
		/// Update "active" marking on adjacent nodes.
		/// </summary>
		public void spreadActive() {
			ICollection<Node> adjacentNodes = edges.Keys;
			foreach (Node node in adjacentNodes) {
				if (node.isActive() != active) node.setActive(active);
			}
		}
	}
}
