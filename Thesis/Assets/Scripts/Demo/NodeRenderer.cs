using UnityEngine;
using Grammars.Graph;
using System.Collections.Generic;

namespace Demo {
	public class NodeRenderer : MonoBehaviour {
		Node node;
        BoxCollider2D boxCol;
		string shapePath;
		SpriteRenderer spriteRender;
        public GraphRenderer graphRenderer;
        
        Vector3 dragCenter;
        bool dragging = false;

		// Use this for initialization
		void Start() {
			spriteRender = gameObject.AddComponent<SpriteRenderer>();
            boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(58,58);
			UpdateSprite();
		}

		// Update is called once per frame
		void Update() {
            if (node != null) {
                if (dragging) {
                    Camera camera = Camera.main;
                    Vector3 newPos = camera.ScreenToWorldPoint(Input.mousePosition) + dragCenter;
                    if(newPos.x.ToString() != node.getAttribute("_demo_x") || newPos.y.ToString() != node.getAttribute("_demo_y")) {
                        node.setAttribute("_demo_x", newPos.x.ToString());
                        node.setAttribute("_demo_y", newPos.y.ToString());
                        gameObject.transform.position = new Vector3(float.Parse(node.getAttribute("_demo_x")), float.Parse(node.getAttribute("_demo_y")));
                        foreach (KeyValuePair<Node, Edge> entry in node.getEdges()) {
                            graphRenderer.updateEdge(entry.Value);
                        }
                    }
                }
            }
		}

		// Custom function with draw code
		void UpdateSprite() {
			if (node != null) {
				if (node.hasAttribute("_demo_shape")) {
					string shape = node.getAttribute("_demo_shape");
					string upperShape = char.ToUpper(shape[0]) + shape.Substring(1);
					spriteRender.sprite = Resources.Load<Sprite>("Sprites/" + upperShape);
				}
				if (node.hasAttribute("_demo_color")) {
					string color = node.getAttribute("_demo_color");
					switch (node.getAttribute("_demo_color")) {
						case "red":
							spriteRender.color = Color.red;
							break;
						case "green":
							spriteRender.color = Color.green;
							break;
						case "blue":
							spriteRender.color = Color.blue;
							break;
						case "yellow":
							spriteRender.color = Color.yellow;
							break;
						default:
							spriteRender.color = Color.white;
							break;
					}
				}
			}
		}

		public void setNode(Node node) {
			this.node = node;
		}

		public Node getNode() {
			return node;
		}

		public string getAttribute(string key) {
			return node.getAttribute(key);
		}

		public void setAttribute(string key, string value) {
			node.setAttribute(key, value);
			if (key.Contains("_demo_")) UpdateSprite();
		}

        public void OnMouseDown() {
            Camera camera = Camera.main;
            dragCenter =  gameObject.transform.position - camera.ScreenToWorldPoint(Input.mousePosition);
            dragging = true;
        }

        public void OnMouseUp() {
            dragging = false;
        }

        public void OnMouseOver() {
            if (graphRenderer.currentNode == null) {
                //setAttribute("_demo_color", "red");
                graphRenderer.currentNode = this;
            }
        }

        public void OnMouseExit() {
            if (graphRenderer.currentNode == this) {
                graphRenderer.currentNode = null;
            }
        }
    }
}