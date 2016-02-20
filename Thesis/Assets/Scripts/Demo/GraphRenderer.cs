using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Grammars.Graph;
using Grammars;

namespace Demo {
	public class GraphRenderer : MonoBehaviour {
		Graph graph;
        public NodeRenderer currentNode = null;
        NodeRenderer startNode = null;
        bool drawingEdge = false;
        LineRenderer drawingLineRenderer;
		public IDictionary<Edge, LineRenderer> lineRenderers = new Dictionary<Edge, LineRenderer>();
		IDictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();

        AttributeClass yellow_triangles = new AttributeClass("yellow_triangles");
        AttributeClass blue_squares = new AttributeClass("blue_squares");
        AttributeClass white_circles = new AttributeClass("white_circles");

        public bool draggingNode = false;
        public CameraControl cameraControl;

        // Use this for initialization
        void Start() {
			// Define a few types of nodes
			yellow_triangles.setAttribute("_demo_shape", "triangle");
			yellow_triangles.setAttribute("_demo_color", "yellow");
			blue_squares.setAttribute("_demo_shape", "square");
			blue_squares.setAttribute("_demo_color", "blue");
			white_circles.setAttribute("_demo_shape", "circle");

			// Create the graph
			graph = new Graph();

			Node root = new Node(graph, 0);
			root.addAttributeClass(yellow_triangles);
			root.setAttribute("_demo_x", "100");
			root.setAttribute("_demo_y", "100");

			Node node2 = new Node(graph, 1);
			node2.addAttributeClass(blue_squares);
			node2.setAttribute("_demo_x", "-100");
			node2.setAttribute("_demo_y", "-100");
            root.addEdge(node2);

			HashSet<Node> nodes = graph.getNodes();
			foreach (Node node in nodes) {
                addNodeRenderer(node);
			}
        }

		// Update is called once per frame
		void Update() {
            if (graph != null) {
                HashSet<Edge> edges = graph.getEdges();
                foreach (Edge edge in edges) {
                    if (!lineRenderers.ContainsKey(edge)) {
                        updateEdge(edge);
                    }
                }

                // Pan
                cameraControl.cameraPanBlocked = draggingNode;

                // Add nodes or edges
                if (Input.GetMouseButtonDown(1)) {
                    if (currentNode == null) {
                        if (drawingEdge) {
                            startNode = null;
                            drawingEdge = false;
                        } else {
                            Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            Node node = new Node(graph, graph.getNodes().Count);
                            node.addAttributeClass(white_circles);
                            node.setAttribute("_demo_x", newPos.x.ToString());
                            node.setAttribute("_demo_y", newPos.y.ToString());
                            addNodeRenderer(node);
                        }
                    } else {
                        if (drawingEdge) {
                            if (currentNode != startNode) {
                                // Create new edge to currentNode from startNode
                                Edge edge = startNode.getNode().addEdge(currentNode.getNode());
                                updateEdge(edge);
                                startNode = null;
                                drawingEdge = false;
                            }
                        } else {
                            startNode = currentNode;
                            drawingEdge = true;
                        }
                    }
                }
                if (drawingEdge) {
                    if (drawingLineRenderer == null) {
                        LineRenderer line = new GameObject().AddComponent<LineRenderer>();
                        line.SetPosition(0, startNode.gameObject.transform.position);
                        line.SetWidth(3.0f, 3.0f);
                        Material mat = new Material(Shader.Find("Unlit/Color"));
                        mat.color = Color.black;
                        line.material = mat;
                        drawingLineRenderer = line;
                    }
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 corrMousePos = new Vector3(mousePos.x, mousePos.y);
                    drawingLineRenderer.SetPosition(1, corrMousePos);
                } else {
                    if (drawingLineRenderer != null) {
                        Destroy(drawingLineRenderer.gameObject);
                    }
                }
            }
		}

        public void updateEdge(Edge edge) {
            if(lineRenderers.ContainsKey(edge)) {
                Destroy(lineRenderers[edge].gameObject);
            }
            LineRenderer line = new GameObject().AddComponent<LineRenderer>();
            line.SetPosition(0, nodeRenderers[edge.getNode1()].gameObject.transform.position);
            line.SetPosition(1, nodeRenderers[edge.getNode2()].gameObject.transform.position);
            line.SetWidth(3.0f, 3.0f);
            line.gameObject.name = "Edge " + edge.getNode1().getID() + "-" + edge.getNode2().getID();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.black;
            line.material = mat;
            lineRenderers[edge] = line;
            line.transform.SetParent(transform);
        }

        public void addNodeRenderer(Node node) {
            NodeRenderer obj = new GameObject().AddComponent<NodeRenderer>();
            obj.gameObject.name = "Node " + node.getID().ToString();
            obj.graphRenderer = this;
            obj.gameObject.transform.position = new Vector3(float.Parse(node.getAttribute("_demo_x")), float.Parse(node.getAttribute("_demo_y")));
            obj.setNode(node);
            nodeRenderers[node] = obj;
            obj.transform.SetParent(transform);
        }
	}
}