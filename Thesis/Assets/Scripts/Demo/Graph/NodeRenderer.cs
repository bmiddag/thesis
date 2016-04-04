using UnityEngine;
using Grammars.Graph;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using Grammars;

namespace Demo {
	public class NodeRenderer : MonoBehaviour, IElementRenderer {
		Node node;
		SpriteRenderer spriteRender;
        Text text;
        public GraphRenderer graphRenderer;

        public Vector3 newPosition;
        Vector3 dragCenter;
        bool dragging = false;

        bool updateRenderer = false; // If true, sprite & text will be updated during the next call of Update(). Prevents chaining of renderer updates.

        public AttributedElement Element {
            get {
                return node;
            }
        }

        // Use this for initialization
        void Start() {
			spriteRender = gameObject.AddComponent<SpriteRenderer>();
            CircleCollider2D circleCol = gameObject.AddComponent<CircleCollider2D>();
            circleCol.radius = 29;
            circleCol.offset = Vector2.zero;
            newPosition = gameObject.transform.position;
		}

		// Update is called once per frame
		void Update() {
            if (updateRenderer) {
                updateRenderer = false;
                UpdateSprite();
                UpdateText();
            }
            if (node != null) {
                if (dragging) {
                    Camera camera = Camera.main;
                    Vector3 newPos = camera.ScreenToWorldPoint(Input.mousePosition) + dragCenter;

                    if(newPos.x != gameObject.transform.position.x || newPos.y != gameObject.transform.position.y) {
                        gameObject.transform.position = new Vector3(newPos.x, newPos.y);
                        newPosition = gameObject.transform.position;
                        UpdateEdgePositions();
                    }
                } else if (!graphRenderer.draggingNode && graphRenderer.optimizeLayout) {
                    IEnumerable<NodeRenderer> otherNodeRens = graphRenderer.GetNodeRenderers();
                    foreach (NodeRenderer nr2 in otherNodeRens) {
                        if (this == nr2) continue;
                        Vector3 v1 = newPosition;
                        Vector3 v2 = nr2.newPosition;
                        newPosition = new Vector3(v1.x + UnityEngine.Random.Range(-0.1F, 0.1F), v1.y + UnityEngine.Random.Range(-0.1F, 0.1F), v1.z);
                        v1 = newPosition;
                        if (node.GetEdges().ContainsKey(nr2.node)) {
                            if (Vector3.Distance(v1, v2) > 200) {
                                newPosition = Vector3.MoveTowards(v1, v2, 4);
                                nr2.newPosition = Vector3.MoveTowards(v2, v1, 4);
                            } else if (Vector3.Distance(v1, v2) < 150) {
                                newPosition = Vector3.MoveTowards(v1, v2, -2);
                                nr2.newPosition = Vector3.MoveTowards(v2, v1, -2);
                            }
                        } else {
                            if (Vector3.Distance(v1, v2) < 300) {
                                newPosition = Vector3.MoveTowards(v1, v2, -2);
                                nr2.newPosition = Vector3.MoveTowards(v2, v1, -2);
                            }
                        }
                    }
                    if (!dragging && Vector3.Distance(gameObject.transform.position, newPosition) > 0.5) {
                        gameObject.transform.position = newPosition;
                        UpdateEdgePositions();
                    }
                    newPosition = gameObject.transform.position;
                }
            }
		}

        public void UpdateEdgePositions() {
            if (node != null) {
                foreach (KeyValuePair<Node, Edge> entry in node.GetEdges()) {
                    graphRenderer.UpdateEdge(entry.Value);
                }
            }
        }

		public void SetNode(Node node) {
            if (this.node != null) {
                this.node.AttributeChanged -= new EventHandler(NodeAttributeChanged);
            }
			this.node = node;
            if (this.node != null) {
                this.node.AttributeChanged += new EventHandler(NodeAttributeChanged);
            }
            updateRenderer = true;
        }

		public Node GetNode() {
			return node;
		}

		public string GetAttribute(string key) {
			return node.GetAttribute(key);
		}

		public void SetAttribute(string key, string value) {
			node.SetAttribute(key, value);
		}

        public void OnMouseDown() {
            if (!graphRenderer.controller.paused) {
                Camera camera = Camera.main;
                dragCenter = gameObject.transform.position - camera.ScreenToWorldPoint(Input.mousePosition);
                dragging = true;
                graphRenderer.draggingNode = true;
            }
        }

        public void OnMouseUp() {
            dragging = false;
            graphRenderer.draggingNode = false;
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

        void NodeAttributeChanged(object sender, EventArgs e) {
            updateRenderer = true;
        }

        void OnDestroy() {
            if (node != null) {
                this.node.AttributeChanged -= new EventHandler(NodeAttributeChanged);
            }
        }

        // ************************** NODE RENDERING CODE ************************** \\
        void UpdateSprite() {
			if (node != null) {
                if (node.HasAttribute("_demo_shape")) {
                    string shape = node.GetAttribute("_demo_shape");
                    string upperShape = char.ToUpper(shape[0]) + shape.Substring(1);
                    spriteRender.sprite = Resources.Load<Sprite>("Sprites/" + upperShape);
                } else {
                    spriteRender.sprite = Resources.Load<Sprite>("Sprites/Circle");
                }
                if (node.HasAttribute("_demo_color")) {
                    string color = node.GetAttribute("_demo_color");
                    switch (color) {
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
                } else {
                    spriteRender.color = Color.white;
                }
			}
		}

        void UpdateText() {
            if (text == null) {
                text = gameObject.AddComponent<Text>();
                text.color = Color.black;
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
            }
            if (node != null) {
                text.text = node.GetID().ToString();
                gameObject.name = "Node " + node.GetID().ToString();
            }
        }
    }
}
 