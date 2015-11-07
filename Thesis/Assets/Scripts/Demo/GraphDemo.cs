using UnityEngine;
using System.Collections;
using Demo;

namespace Demo {
	public class GraphDemo : MonoBehaviour {

		// Use this for initialization
		void Start() {
			Node root = new GameObject().AddComponent<Node>();
			root.gameObject.transform.position = new Vector3(0, 0);
			Grammars.Graph.Node node = new Grammars.Graph.Node("root");
			node.setAttribute("demo__shape", "triangle");
			node.setAttribute("demo__color", "yellow");
			root.setNode(node);
		}

		// Update is called once per frame
		void Update() {

		}
	}
}