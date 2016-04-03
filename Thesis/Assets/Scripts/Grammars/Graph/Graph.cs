using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
    public class Graph : StructureModel {
		HashSet<Node> nodes;
		HashSet<Edge> edges;

        public Graph() : base() {
			nodes = new HashSet<Node>();
			edges = new HashSet<Edge>();
		}

		public HashSet<Node> GetNodes() {
			return nodes;
		}

		public HashSet<Edge> GetEdges() {
			return edges;
		}

        public Node GetNodeByID(int id) {
            // Assumes correct use. There should not be more than one node with the same ID.
            IEnumerable<Node> nodeswithID = nodes.Where(n => n.GetID() == id);
            if (nodeswithID.Count() == 0) {
                return null;
            } else return nodeswithID.First();
        }

        public override List<AttributedElement> GetElements(string specifier = null) {
            List<AttributedElement> attrList = new List<AttributedElement>();
            if (specifier == "edges" || specifier == "all") {
                foreach (Edge edge in edges) {
                    attrList.Add(edge);
                }
            }
            if (specifier != "edges") {
                // Assume nodes should be included.
                foreach (Node node in nodes) {
                    attrList.Add(node);
                }
            }
            return attrList;
        }

        // ************************** SET MANAGEMENT ************************** \\
        // The following code only adds and removes elements to/from sets.
        // Element creation should be handled outside of this class.
        public void AddNode(Node node) {
			if (nodes.Count == 0) node.Active = true; // TODO: Move this
			nodes.Add(node);
			ICollection<Edge> edgeList = node.GetEdges().Values;
			foreach (Edge edge in edgeList) {
				if (!edges.Contains(edge)) AddEdge(edge);
			}
            OnStructureChanged(EventArgs.Empty);
		}

		public void AddEdge(Edge edge) {
			edges.Add(edge);
			if (!nodes.Contains(edge.GetNode1())) AddNode(edge.GetNode1());
			if (!nodes.Contains(edge.GetNode2())) AddNode(edge.GetNode2());
            OnStructureChanged(EventArgs.Empty);
        }

		public void RemoveEdge(Edge edge) {
			if (edge != null & edges.Contains(edge)) {
				edges.Remove(edge);
                OnStructureChanged(EventArgs.Empty);
            }
		}

		public void RemoveNode(Node node) {
			if (node != null) {
				nodes.Remove(node);
				ICollection<Edge> nodeEdges = node.GetEdges().Values;
				foreach (Edge edge in nodeEdges) {
					RemoveEdge(edge);
				}
                OnStructureChanged(EventArgs.Empty);
            }
		}

        public override AttributedElement GetElement(string identifier) {
            if (identifier == null) return null; ;
            if (identifier.Contains("-")) {
                int id1, id2;
                string[] splitID = identifier.Split('-');
                if (int.TryParse(splitID[0], out id1) && int.TryParse(splitID[1], out id2)) {
                    Node node1 = GetNodeByID(id1);
                    Node node2 = GetNodeByID(id2);
                    if (node1 == null || node2 == null) return null;
                    IEnumerable<Edge> matches = edges.Where(e => (e.GetNode1() == node1 && e.GetNode2() == node2) || (e.GetNode1() == node2 && e.GetNode2() == node1));
                    if (matches == null || matches.Count() == 0) return null;
                    return matches.First();
                }
            } else {
                int id;
                if (int.TryParse(identifier, out id)) {
                    return GetNodeByID(id);
                }
            }
            return null;
        }
    }
}
