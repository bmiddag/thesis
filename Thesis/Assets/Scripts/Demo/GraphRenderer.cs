using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Grammars.Graph;

namespace Demo {
	public class GraphRenderer : MonoBehaviour {
		Graph graph;
		IDictionary<Edge, LineRenderer> lineRenderers = new Dictionary<Edge, LineRenderer>();
		IDictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();

		Dictionary<string, string> yellow_triangles = new Dictionary<string, string>();
		Dictionary<string, string> blue_squares = new Dictionary<string, string>();
		Dictionary<string, string> white_circles = new Dictionary<string, string>();

		// Use this for initialization
		void Start() {
			// Define a few types of nodes
			yellow_triangles.Add("demo__shape", "triangle");
			yellow_triangles.Add("demo__color", "yellow");
			blue_squares.Add("demo__shape", "square");
			blue_squares.Add("demo__color", "blue");
			white_circles.Add("demo__shape", "circle");

			// Create the graph
			graph = new Graph();

			Node root = new Node(graph, "root");
			root.setAttributes(yellow_triangles);
			root.setAttribute("demo__startX", "100");
			root.setAttribute("demo__startY", "100");

			Node node2 = new Node(graph, "node2");
			node2.setAttributes(blue_squares);
			node2.setAttribute("demo__startX", "-100");
			node2.setAttribute("demo__startY", "-100");
			root.addEdge(node2);

			HashSet<Node> nodes = graph.getNodes();
			foreach (Node node in nodes) {
				NodeRenderer obj = new GameObject().AddComponent<NodeRenderer>();
				obj.gameObject.transform.position = new Vector3(int.Parse(node.getAttribute("demo__startX")), int.Parse(node.getAttribute("demo__startY")));
				obj.setNode(node);
				nodeRenderers[node] = obj;
			}
		}

		// Update is called once per frame
		void Update() {
			if (graph != null) {
				HashSet<Edge> edges = graph.getEdges();
				foreach (Edge edge in edges) {
					if (!lineRenderers.ContainsKey(edge)) {
						LineRenderer line = new GameObject().AddComponent<LineRenderer>();
						line.SetPosition(0, nodeRenderers[edge.getNode1()].gameObject.transform.position);
						line.SetPosition(1, nodeRenderers[edge.getNode2()].gameObject.transform.position);
						line.SetWidth(3.0f, 3.0f);
						Material whiteDiffuseMat = new Material(Shader.Find("Unlit/Texture"));
						line.material = whiteDiffuseMat;
						lineRenderers[edge] = line;
					}
				}

			}
		}
	}
}