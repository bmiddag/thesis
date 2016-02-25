using UnityEngine;
using System.Collections.Generic;
using Grammars.Graph;
using Grammars;
using System;

namespace Demo {
	public class GraphRenderer : MonoBehaviour {
		Graph graph;
        public NodeRenderer currentNode = null;
        public EdgeRenderer currentEdge = null;
        NodeRenderer startNode = null;
        bool drawingEdge = false;
        EdgeRenderer drawingEdgeRenderer;
		public IDictionary<Edge, EdgeRenderer> edgeRenderers = new Dictionary<Edge, EdgeRenderer>();
		IDictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();

        IDictionary<string, AttributeClass> attributeClasses = new Dictionary<string, AttributeClass>(); // TODO: move to grammar

        public bool draggingNode = false;
        public CameraControl cameraControl;

        bool updateRenderer = false; // If true, node/edge renderers will be updated during the next call of Update(). Prevents chaining of renderer updates.

        public GraphDemoController controller;

        // Use this for initialization
        void Start() {
            // Define some attribute classes
            attributeClasses["yellow_triangles"] = new AttributeClass("yellow_triangles");
            attributeClasses["blue_squares"] = new AttributeClass("blue_squares");
            attributeClasses["white_circles"] = new AttributeClass("white_circles");

            attributeClasses["yellow_triangles"].SetAttribute("_demo_shape", "triangle");
            attributeClasses["yellow_triangles"].SetAttribute("_demo_color", "yellow");
            attributeClasses["blue_squares"].SetAttribute("_demo_shape", "square");
            attributeClasses["blue_squares"].SetAttribute("_demo_color", "blue");
            attributeClasses["white_circles"].SetAttribute("_demo_shape", "circle");

			// Create the graph
			graph = new Graph();
            graph.StructureChanged += GraphStructureChanged;

			/*Node root = new Node(graph, 0);
			root.AddAttributeClass(yellow_triangles);
			root.SetAttribute("_demo_x", "100");
			root.SetAttribute("_demo_y", "100");

			Node node2 = new Node(graph, 1);
			node2.AddAttributeClass(blue_squares);
			node2.SetAttribute("_demo_x", "-100");
			node2.SetAttribute("_demo_y", "-100");
            root.AddEdge(node2);*/
        }

        // Update is called once per frame
        void Update() {
            if (graph != null) {
                if (updateRenderer) {
                    updateRenderer = false;
                    SyncGraphStructure();                    
                }

                // Pan
                cameraControl.cameraPanBlocked = draggingNode || controller.paused;

                // Add nodes or edges
                if (Input.GetMouseButtonDown(1) && !controller.paused) {
                    if (currentNode == null) {
                        if (drawingEdge) {
                            startNode = null;
                            drawingEdge = false;
                        } else {
                            Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            Node node = new Node(graph, graph.GetNodes().Count);
                            node.AddAttributeClass(attributeClasses["white_circles"]);
                            node.SetAttribute("_demo_x", newPos.x.ToString());
                            node.SetAttribute("_demo_y", newPos.y.ToString());
                        }
                    } else {
                        if (drawingEdge) {
                            if (currentNode != startNode) {
                                // Create new edge to currentNode from startNode
                                Edge edge = startNode.GetNode().AddEdge(currentNode.GetNode(), true);
                                startNode = null;
                                drawingEdge = false;
                            }
                        } else {
                            startNode = currentNode;
                            drawingEdge = true;
                        }
                    }
                }

                // Remove nodes
                if (Input.GetKey(KeyCode.X) && !controller.paused) {
                    if (currentNode != null) {
                        currentNode.GetNode().Destroy();
                        //graph.RemoveNode(currentNode.GetNode());
                    } else if (currentEdge != null) {
                        currentEdge.GetEdge().Destroy();
                    }
                }

                // Handle temporary line renderer (before actual edge creation)
                if (drawingEdge && !controller.paused) {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 corrMousePos = new Vector3(mousePos.x, mousePos.y);
                    if (drawingEdgeRenderer == null) {
                        // Create temporary line that follows cursor
                        drawingEdgeRenderer = CreateEdgeRenderer(startNode.gameObject.transform.position, corrMousePos);
                    } else {
                        // Update temporary line to follow cursor
                        drawingEdgeRenderer.SetPosition(1, corrMousePos);
                    }
                } else if (drawingEdgeRenderer != null) {
                    Destroy(drawingEdgeRenderer.gameObject);
                }
            }
		}

        public void UpdateEdge(Edge edge) {
            if (edgeRenderers.ContainsKey(edge)) {
                edgeRenderers[edge].SetPositions(nodeRenderers[edge.GetNode1()].gameObject.transform.position,
                    nodeRenderers[edge.GetNode2()].gameObject.transform.position);
            }
        }

        public void AddEdgeRenderer(Edge edge) {
            if (edgeRenderers.ContainsKey(edge)) {
                UpdateEdge(edge);
            } else {
                EdgeRenderer edgeRen = CreateEdgeRenderer(nodeRenderers[edge.GetNode1()].gameObject.transform.position,
                    nodeRenderers[edge.GetNode2()].gameObject.transform.position);
                edgeRen.gameObject.name = "Edge " + edge.GetNode1().GetID() + "-" + edge.GetNode2().GetID();
                edgeRen.SetEdge(edge);
                edgeRenderers[edge] = edgeRen;
            }
        }

        public void RemoveEdgeRenderer(Edge edge) {
            if (edgeRenderers.ContainsKey(edge)) {
                Destroy(edgeRenderers[edge].gameObject);
                edgeRenderers.Remove(edge);
            }
        }

        public EdgeRenderer CreateEdgeRenderer(Vector3 pos0, Vector3 pos1) {
            EdgeRenderer edge = new GameObject().AddComponent<EdgeRenderer>();
            edge.graphRenderer = this;
            edge.SetPositions(pos0, pos1);
            edge.transform.SetParent(transform);
            return edge;
        }

        public void AddNodeRenderer(Node node, float x = 0f, float y = 0f) {
            NodeRenderer obj = new GameObject().AddComponent<NodeRenderer>();
            obj.gameObject.name = "Node " + node.GetID().ToString();
            obj.graphRenderer = this;
            if (node.HasAttribute("_demo_x") && node.HasAttribute("_demo_y")) {
                obj.gameObject.transform.position = new Vector3(float.Parse(node["_demo_x"]), float.Parse(node["_demo_y"]));
            } else {
                obj.gameObject.transform.position = new Vector3(x, y);
            }
            obj.SetNode(node);
            nodeRenderers[node] = obj;
            obj.transform.SetParent(transform);
        }

        public void RemoveNodeRenderer(Node node) {
            RemoveNodeRenderer(nodeRenderers[node]);
        }

        public void RemoveNodeRenderer(NodeRenderer nodeToRemove) {
            // Remove current node status
            if (currentNode == nodeToRemove) {
                currentNode = null;
            }
            // Remove temporary line renderer
            if (drawingEdge && nodeToRemove.Equals(startNode)) {
                drawingEdge = false;
                startNode = null;
            }
            if (nodeToRemove.GetNode() != null) {
                // Remove edge renderers
                ICollection<Edge> edgesToRemove = nodeToRemove.GetNode().GetEdges().Values;
                foreach (Edge edge in edgesToRemove) {
                    RemoveEdgeRenderer(edge);
                }
                // Remove node renderer & node
                nodeRenderers.Remove(nodeToRemove.GetNode());
                Destroy(nodeToRemove.gameObject);
                nodeToRemove.GetNode().Destroy();
            } else {
                // Remove node renderer only
                nodeRenderers.Remove(nodeToRemove.GetNode());
                Destroy(nodeToRemove.gameObject);
            }
        }

        // ************************** STRUCTURE UPDATES ************************** \\
        private void GraphStructureChanged(object sender, EventArgs e) {
            updateRenderer = true; // SyncGraphStructure will be called during next update.
        }

        void OnDestroy() {
            if (graph != null) {
                graph.StructureChanged -= new EventHandler(GraphStructureChanged);
            }
        }

        void SyncGraphStructure() {
            HashSet<Node> nodesInGraph = graph.GetNodes();
            ICollection<Node> nodesInRenderer = nodeRenderers.Keys;
            if (!nodesInGraph.SetEquals(nodesInRenderer)) {
                HashSet<Node> nodesToAdd = new HashSet<Node>(nodesInGraph);
                nodesToAdd.ExceptWith(nodesInRenderer);
                foreach (Node node in nodesToAdd) {
                    AddNodeRenderer(node);
                }
                HashSet<Node> nodesToRemove = new HashSet<Node>(nodesInRenderer);
                nodesToRemove.ExceptWith(nodesInGraph);
                foreach (Node node in nodesToRemove) {
                    RemoveNodeRenderer(node);
                }
            }
            HashSet<Edge> edgesInGraph = graph.GetEdges();
            ICollection<Edge> edgesInRenderer = edgeRenderers.Keys;
            if (!edgesInGraph.SetEquals(edgesInRenderer)) {
                HashSet<Edge> edgesToAdd = new HashSet<Edge>(edgesInGraph);
                edgesToAdd.ExceptWith(edgesInRenderer);
                foreach (Edge edge in edgesToAdd) {
                    AddEdgeRenderer(edge);
                }
                HashSet<Edge> edgesToRemove = new HashSet<Edge>(edgesInRenderer);
                edgesToRemove.ExceptWith(edgesInGraph);
                foreach (Edge edge in edgesToRemove) {
                    RemoveEdgeRenderer(edge);
                }
            }
        }
    }
}