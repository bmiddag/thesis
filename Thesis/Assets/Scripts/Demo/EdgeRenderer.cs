using UnityEngine;
using Grammars.Graph;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace Demo {
	public class EdgeRenderer : MonoBehaviour {
        Edge edge;
        LineRenderer mainLine;
        LineRenderer arrowLine1;
        LineRenderer arrowLine2;

        Vector3 mainPos0 = new Vector3(0f, 0f);
        Vector3 mainPos1 = new Vector3(0f, 0f);

        CircleCollider2D circleCol;
        public GraphRenderer graphRenderer;

        bool updateRenderer = false; // If true, sprite & text will be updated during the next call of Update(). Prevents chaining of renderer updates.
        bool directed = false;

        // Use this for initialization
        void Start() {
            InitLineRenderers();
        }

		// Update is called once per frame
		void Update() {
            if (updateRenderer) {
                updateRenderer = false;
                if (edge != null) {
                    directed = edge.IsDirected();
                    if (!directed && arrowLine1 != null && arrowLine2 != null) {
                        Destroy(arrowLine1.gameObject);
                        Destroy(arrowLine2.gameObject);
                    }
                }
                InitLineRenderers();
                UpdateLineRenderers();
            }
		}

		public void SetEdge(Edge edge) {
            if (this.edge != null) {
                this.edge.AttributeChanged -= new EventHandler(EdgeAttributeChanged);
            }
            this.edge = edge;
            if (this.edge != null) {
                this.edge.AttributeChanged += new EventHandler(EdgeAttributeChanged);
            }
            updateRenderer = true;
            InitCollider();
        }

		public Edge GetEdge() {
			return edge;
		}

		public string GetAttribute(string key) {
			return edge.GetAttribute(key);
		}

		public void SetAttribute(string key, string value) {
			edge.SetAttribute(key, value);
		}

        void EdgeAttributeChanged(object sender, EventArgs e) {
            updateRenderer = true;
        }

        void OnDestroy() {
            if (edge != null) {
                edge.AttributeChanged -= new EventHandler(EdgeAttributeChanged);
            }
        }

        void InitCollider() {
            if (circleCol == null) {
                circleCol = gameObject.AddComponent<CircleCollider2D>();
                circleCol.radius = 20;
                circleCol.offset = Vector3.Lerp(mainPos0, mainPos1, 0.5f) - transform.position;
            }
        }

        public void OnMouseOver() {
            if (graphRenderer.currentEdge == null) {
                graphRenderer.currentEdge = this;
            }
        }

        public void OnMouseExit() {
            if (graphRenderer.currentEdge == this) {
                graphRenderer.currentEdge = null;
            }
        }


        // ************************** EDGE RENDERING CODE ************************** \\
        public void InitLineRenderers() {
            if (mainLine == null) {
                mainLine = gameObject.AddComponent<LineRenderer>();
                InitLineRenderer(mainLine);
            }
            if (directed) {
                if (arrowLine1 == null) {
                    arrowLine1 = new GameObject().AddComponent<LineRenderer>();
                    arrowLine1.transform.SetParent(transform);
                    InitLineRenderer(arrowLine1);
                }
                if (arrowLine2 == null) {
                    arrowLine2 = new GameObject().AddComponent<LineRenderer>();
                    arrowLine2.transform.SetParent(transform);
                    InitLineRenderer(arrowLine2);
                }
            }
            SetPositions(mainPos0, mainPos1);
        }

        LineRenderer InitLineRenderer(LineRenderer line) {
            line.SetWidth(3.0f, 3.0f);
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.black;
            line.material = mat;
            line.transform.SetParent(transform);
            return line;
        }

        void UpdateLineRenderers() {
            if (edge != null && mainLine != null) {
                Color color = Color.black;
                if (edge.HasAttribute("_demo_color")) {
                    string col = edge.GetAttribute("_demo_color");
                    switch (col) {
                        case "red":
                            color = Color.red;
                            break;
                        case "green":
                            color = Color.green;
                            break;
                        case "blue":
                            color = Color.blue;
                            break;
                        case "yellow":
                            color = Color.yellow;
                            break;
                        default:
                            color = Color.black;
                            break;
                    }
                }
                mainLine.material.color = color;
                if (arrowLine1 != null && arrowLine2 != null) {
                    arrowLine1.material.color = color;
                    arrowLine2.material.color = color;
                }
            }
        }

        public void SetPositions(Vector3 pos0, Vector3 pos1) {
            if (mainLine == null) {
                InitLineRenderers();
            }
            mainLine.SetPosition(0, pos0);
            mainLine.SetPosition(1, pos1);
            mainPos0 = pos0;
            mainPos1 = pos1;
            if (directed && arrowLine1 != null & arrowLine2 != null) {
                Vector3 middlePlus = Vector3.MoveTowards(pos0, pos1, (Vector3.Distance(pos0, pos1) / 2) + 12f);
                Vector3 arrowHead = Vector3.Normalize(pos0 - middlePlus) * 30f;
                Vector3 arrowHead1 = middlePlus + (Quaternion.Euler(0, 0, 20) * arrowHead);
                Vector3 arrowHead2 = middlePlus + (Quaternion.Euler(0, 0, -20) * arrowHead);
                arrowLine1.SetPosition(0, middlePlus);
                arrowLine2.SetPosition(0, middlePlus);
                arrowLine1.SetPosition(1, arrowHead1);
                arrowLine2.SetPosition(1, arrowHead2);
            }
            if (circleCol != null) {
                circleCol.offset = Vector3.Lerp(mainPos0, mainPos1, 0.5f) - transform.position;
            }
        }

        public void SetPosition(int index, Vector3 pos) {
            if (mainLine != null) {
                if (index == 0) {
                    SetPositions(pos, mainPos1);
                } else if (index == 1) {
                    SetPositions(mainPos0, pos);
                }
            }
        }
    }
}
 