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
                        foreach (KeyValuePair<Node, Edge> entry in node.GetEdges()) {
                            graphRenderer.UpdateEdge(entry.Value);
                        }
                    }
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
 