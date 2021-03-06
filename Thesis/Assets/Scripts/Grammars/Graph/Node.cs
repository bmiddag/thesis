﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Grammars.Graphs {
    public class Node : AttributedElement {
		Graph graph;
		IDictionary<Node, Edge> edges;
		int id;

		private bool active = false;
        public bool Active {
            get { return active; }
            set {
                active = value;
                // Spread active state to adjacent nodes
                ICollection<Node> adjacentNodes = edges.Keys;
                foreach (Node node in adjacentNodes) {
                    if (node.Active != active) node.Active = active;
                }
            }
        }

        public override IElementContainer Container {
            get { return graph; }
        }

        public override string LinkType {
            get { return graph.LinkType; }
            set { }
        }

        public Node(Graph graph, int id) : base() {
			this.graph = graph;
			edges = new Dictionary<Node, Edge>();
			this.id = id;
			graph.AddNode(this);
		}

		public Edge AddEdge(Node node, bool directed = false, IDictionary<string, string> attributes = null) {
			if (node == null) return null;
			Edge edge;
			if (edges.ContainsKey(node)) {
                edge = edges[node];
                if (edge.IsDirected() && (edge.GetNode2() == this || !directed)) {
                    edge.MakeUndirected();
                }
			} else {
				edge = new Edge(graph, this, node, directed); // Edge is added to both nodes' edge dictionary, as well as to the graph
			}
			edge.SetAttributes(attributes);
			if (Active && !node.Active) {
				node.Active = true;
			} else if (!Active && node.Active) {
				Active = true;
			}
			return edge;
		}

		public void AddEdge(Node node, Edge edge) {
			if (edge != null) {
				edges[node] = edge;
			}
		}

		public void RemoveEdge(Node node) {
			if (node != null && edges.ContainsKey(node)) {
				Edge edge = edges[node];
				edge.Destroy();
				edges.Remove(node);
			}
		}

		public void RemoveEdge(Edge edge) {
			Node node = edges.FirstOrDefault(x => x.Value == edge).Key;
			RemoveEdge(node);
		}

		public IDictionary<Node, Edge> GetEdges() {
			return new Dictionary<Node, Edge>(edges);
		}

        public int GetID() {
            return id;
        }

        public void SetID(int id) {
            this.id = id;
            OnAttributeChanged(EventArgs.Empty);
        }

		public void Destroy() {
            /*List<KeyValuePair<Node, Edge>> edgeList = edges.ToList();
			foreach (KeyValuePair<Node, Edge> entry in edges) {
				entry.Value.destroy();
			}*/
            SetAttribute("_grammar_destroyed", "true");
            ICollection<Node> adjacentNodeList = edges.Keys;
            while (adjacentNodeList.Count > 0) {
                RemoveEdge(adjacentNodeList.First());
            }
            active = false;
			graph.RemoveNode(this);
		}

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null) {
                int count = 0;
                switch (key) {
                    case "_type":
                        result = "node"; break;
                    case "_id":
                        result = id.ToString(); break;
                    case "_edges":
                        result = edges.Count.ToString(); break;
                    case "_edges_incoming":
                        foreach (Edge edge in edges.Values) {
                            if (!edge.IsDirected() || edge.GetNode2() == this) {
                                count++;
                            }
                        }
                        result = count.ToString(); break;
                    case "_edges_outgoing":
                        foreach (Edge edge in edges.Values) {
                            if (!edge.IsDirected() || edge.GetNode1() == this) {
                                count++;
                            }
                        }
                        result = count.ToString(); break;
                }
            }
            return result;
        }
    }
}
