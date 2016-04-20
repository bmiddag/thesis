using UnityEngine;
using System.Collections.Generic;
using Grammars.Graphs;
using Grammars;
using System;
using System.Collections;
using Grammars.Events;

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
        Grammar<Graph> grammar = null;

        public bool draggingNode = false;
        public CameraControl cameraControl;

        bool snapToMouse = false;
        public bool optimizeLayout = true;

        bool updateRenderer = false; // If true, node/edge renderers will be updated during the next call of Update(). Prevents chaining of renderer updates.

        public DemoController controller;

        public IElementRenderer CurrentElement {
            get {
                if (currentNode != null) return currentNode;
                if (currentEdge != null) return currentEdge;
                return null;
            }
        }

        public StructureModel Source {
            get {
                if (graph == null) {
                    graph = new Graph();
                }
                return graph;
            }
        }

        public IGrammarEventHandler Grammar {
            get { return grammar; }
            set {
                if (value == null) {
                    grammar = null;
                } else if (value.GetType() == typeof(Grammar<Graph>)) {
                    grammar = (Grammar<Graph>)value;
                } else {
                    print("Wrong grammar type for this demo renderer.");
                }
            }
        }

        public string Name {
            get {
                if (grammar != null) {
                    return grammar.Name;
                } else return "";
            }
        }

        // Use this for initialization
        void Start() {
            if(controller.currentStructureRenderer == null) controller.RegisterStructureRenderer(this);

            // Create the graph
            if(graph == null) graph = new Graph();
            graph.StructureChanged += GraphStructureChanged;
        }

        // Update is called once per frame
        void Update() {
            if (graph != null) {
                if (updateRenderer) {
                    updateRenderer = false;
                    SyncGraphStructure();                    
                }

                // Optimize layout
                if (Input.GetKeyDown(KeyCode.K) && !controller.paused) {
                    optimizeLayout = !optimizeLayout;
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
                            // Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            snapToMouse = true;
                            new Node(graph, graph.GetNodes().Count);

                            //controller.AddAttributeClass(node, "white_circles");
                            //node.SetAttribute("_demo_x", newPos.x.ToString());
                            //node.SetAttribute("_demo_y", newPos.y.ToString());
                        }
                    } else {
                        if (drawingEdge) {
                            if (currentNode != startNode) {
                                // Create new edge to currentNode from startNode
                                startNode.GetNode().AddEdge(currentNode.GetNode(), true);
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

                /*if (!controller.paused) {
                    if (Input.GetKeyDown(KeyCode.F)) {
                        StartCoroutine("FindTransform");
                    }
                }*/
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
            } else if(nodeRenderers.ContainsKey(edge.GetNode1()) && nodeRenderers.ContainsKey(edge.GetNode2())) {
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
            if (snapToMouse) {
                Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                obj.gameObject.transform.position = new Vector3(newPos.x, newPos.y);
                snapToMouse = false;
            } else {
                obj.gameObject.transform.position = new Vector3(x, y);
            }
            obj.SetNode(node);
            nodeRenderers[node] = obj;
            obj.transform.SetParent(transform);
        }

        public NodeRenderer GetNodeRenderer(Node node) {
            if (nodeRenderers.ContainsKey(node)) {
                return nodeRenderers[node];
            } else return null;
        }

        public IEnumerable<NodeRenderer> GetNodeRenderers() {
            return nodeRenderers.Values;
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
            string filename = "Grammars/Graph_" + dateTime + ".xml";
            DemoIO serializer = new DemoIO(filename, controller);
            serializer.SerializeGraph(graph);
            print("Saved!");
            yield return null;
        }

        public IEnumerator LoadStructure() {
            string filename = "Grammars/Graph_test.xml";
            DemoIO serializer = new DemoIO(filename, controller);
            Graph newGraph = serializer.DeserializeGraph();
            print(newGraph.GetNodes().Count);
            SetGraph(newGraph);
            print("Loaded!");
            yield return null;
        }

        public IEnumerator GrammarStep() {
            if (grammar != null) {
                grammar.Update();
            }
            yield return null;
        }
    }
}