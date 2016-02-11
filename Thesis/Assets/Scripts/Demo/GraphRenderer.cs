using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Grammars.Graph;

namespace Demo {
	public class GraphRenderer : MonoBehaviour {
		Graph graph;
        public NodeRenderer currentNode = null;
        NodeRenderer startNode = null;
        bool drawingEdge = false;
        LineRenderer drawingLineRenderer;
		public IDictionary<Edge, LineRenderer> lineRenderers = new Dictionary<Edge, LineRenderer>();
		IDictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();

		Dictionary<string, string> yellow_triangles = new Dictionary<string, string>();
		Dictionary<string, string> blue_squares = new Dictionary<string, string>();
		Dictionary<string, string> white_circles = new Dictionary<string, string>();

		// Use this for initialization
		void Start() {
			// Define a few types of nodes
			yellow_triangles.Add("_demo_shape", "triangle");
			yellow_triangles.Add("_demo_color", "yellow");
			blue_squares.Add("_demo_shape", "square");
			blue_squares.Add("_demo_color", "blue");
			white_circles.Add("_demo_shape", "circle");

			// Create the graph
			graph = new Graph();

			Node root = new Node(graph, "root");
			root.setAttributes(yellow_triangles);
			root.setAttribute("_demo_x", "100");
			root.setAttribute("_demo_y", "100");

			Node node2 = new Node(graph, "node2");
			node2.setAttributes(blue_squares);
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
                if (Input.GetMouseButtonDown(1)) {
                    if (currentNode == null) {
                        if (drawingEdge) {
                            startNode = null;
                            drawingEdge = false;
                        } else {
                            Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            Node node = new Node(graph, "newNode");
                            node.setAttributes(white_circles);
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
                        Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
                        line.material = whiteDiffuseMat;
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
            Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
            line.material = whiteDiffuseMat;
            lineRenderers[edge] = line;
        }

        public void addNodeRenderer(Node node) {
            NodeRenderer obj = new GameObject().AddComponent<NodeRenderer>();
            obj.gameObject.name = node.getName();
            obj.graphRenderer = this;
            obj.gameObject.transform.position = new Vector3(float.Parse(node.getAttribute("_demo_x")), float.Parse(node.getAttribute("_demo_y")));
            obj.setNode(node);
            nodeRenderers[node] = obj;
        }
	}
}