using UnityEngine;
using System.Collections.Generic;
using Grammars.Graph;
using Grammars;
using System;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Demo {
	public class GraphRenderer : MonoBehaviour, IStructureRenderer {
		Graph graph;
        public NodeRenderer currentNode = null;
        public EdgeRenderer currentEdge = null;
        NodeRenderer startNode = null;
        bool drawingEdge = false;
        EdgeRenderer drawingEdgeRenderer;
		public IDictionary<Edge, EdgeRenderer> edgeRenderers = new Dictionary<Edge, EdgeRenderer>();
		IDictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();

        public bool draggingNode = false;
        public CameraControl cameraControl;

        bool updateRenderer = false; // If true, node/edge renderers will be updated during the next call of Update(). Prevents chaining of renderer updates.

        public DemoController controller;

        public IElementRenderer CurrentElement {
            get {
                if (currentNode != null) return currentNode;
                if (currentEdge != null) return currentEdge;
                return null;
            }
        }

        IEnumerator FindTransform() {
            Graph query = new Graph();
            Graph target = new Graph();

            // Test graph
            Node queryNode1 = new Node(query, 1);
            controller.AddAttributeClass(queryNode1, "white_circles");

            Node targetNode1 = new Node(target, 1);
            controller.AddAttributeClass(targetNode1, "blue_squares");
            targetNode1.SetAttribute("transformed", "true");

            Node queryNode2 = new Node(query, 2);
            controller.AddAttributeClass(queryNode2, "blue_squares");

            Node targetNode2 = new Node(target, 2);
            controller.AddAttributeClass(targetNode2, "white_circles");
            targetNode2.SetAttribute("transformed", "true");

            Node targetNode3 = new Node(target, 3);
            controller.AddAttributeClass(targetNode3, "yellow_triangles");
            //targetNode3.SetAttribute("transformed", "true");

            Node queryNode4 = new Node(query, 4);
            controller.AddAttributeClass(queryNode4, "yellow_triangles");

            Edge queryEdge = new Edge(query, queryNode2, queryNode1, true);
            Edge queryEdge2 = new Edge(query, queryNode4, queryNode2, false);
            Edge targetEdge = new Edge(target, targetNode2, targetNode1, true);
            Edge targetEdge2 = new Edge(target, targetNode1, targetNode3, false);
            targetEdge["transformed"] = "true";
            targetEdge["_demo_color"] = "blue";

            GraphTransformer t = new GraphTransformer();
            t.Source = graph;
            bool found = t.Find(query);
            if (found) {
                print("Found it!");
                print(t.nodeTransformations.Count);
            } else {
                print("Not found");
            }
            //yield return new WaitForSeconds(2);
            if(found) t.Transform(target);
            yield return null;
        }

        // Use this for initialization
        void Start() {
            controller.RegisterStructureRenderer(this);

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
                            controller.AddAttributeClass(node, "white_circles");
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

                if (!controller.paused) {
                    if (Input.GetKeyDown(KeyCode.F)) {
                        StartCoroutine("FindTransform");
                    }

                    if (Input.GetKeyDown(KeyCode.S)) {
                        StartCoroutine("SaveStructure");
                    }

                    if (Input.GetKeyDown(KeyCode.L)) {
                        StartCoroutine("LoadStructure");
                    }
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

        public void SetGraph(Graph graph) {
            if (graph == null) return;
            if (this.graph != null) {
                this.graph.StructureChanged -= new EventHandler(GraphStructureChanged);
            }
            this.graph = graph;
            graph.StructureChanged += new EventHandler(GraphStructureChanged);
            updateRenderer = true;
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

        public IEnumerator SaveStructure() {
            string dateTime = DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString()
                + "_" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" + DateTime.Now.Second.ToString()
                + "_" + DateTime.Now.Millisecond.ToString();
            string filename = "Graph_" + dateTime + ".xml";
            DemoIO serializer = new DemoIO(filename, controller);
            serializer.SerializeGraph(graph);
            print("Saved!");
            yield return null;
        }

        public IEnumerator LoadStructure() {
            string filename = "Graph_test.xml";
            DemoIO serializer = new DemoIO(filename, controller);
            Graph newGraph = serializer.DeserializeGraph();
            print(newGraph.GetNodes().Count);
            SetGraph(newGraph);
            print("Loaded!");
            yield return null;
        }
    }
}