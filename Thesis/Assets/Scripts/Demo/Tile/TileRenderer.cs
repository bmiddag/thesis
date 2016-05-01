using UnityEngine;
using UnityEngine.UI;
using System;
using Grammars.Tiles;
using Grammars;

namespace Demo {
	public class TileRenderer : MonoBehaviour, IElementRenderer {
        Tile tile;
		SpriteRenderer spriteRender;
        SpriteRenderer objRenderer;
        Text text;
        public TileGridRenderer gridRenderer;

        bool updateRenderer = false; // If true, sprite & text will be updated during the next call of Update(). Prevents chaining of renderer updates.

        public AttributedElement Element {
            get { return tile; }
        }

        // Use this for initialization
        void Start() {
			spriteRender = gameObject.AddComponent<SpriteRenderer>();
            objRenderer = new GameObject("ObjRender").AddComponent<SpriteRenderer>();
            objRenderer.transform.SetParent(transform);
            objRenderer.transform.localPosition = new Vector3(0, 0, -1);
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.offset = Vector2.zero;
            boxCol.size = new Vector2(60, 60);
		}

		// Update is called once per frame
		void Update() {
            if (updateRenderer) {
                updateRenderer = false;
                UpdateSprite();
            }
		}

		public void SetTile(Tile tile) {
            if (this.tile != null) {
                this.tile.AttributeChanged -= new EventHandler(TileAttributeChanged);
            }
			this.tile = tile;
            if (this.tile != null) {
                this.tile.AttributeChanged += new EventHandler(TileAttributeChanged);
            }
            updateRenderer = true;
        }

		public Tile GetTile() {
			return tile;
		}

		public string GetAttribute(string key) {
			return tile.GetAttribute(key);
		}

		public void SetAttribute(string key, string value) {
			tile.SetAttribute(key, value);
		}

        public void OnMouseOver() {
            if (gridRenderer.currentTile == null) {
                //setAttribute("_demo_color", "red");
                gridRenderer.currentTile = this;
            }
        }

        public void OnMouseExit() {
            if (gridRenderer.currentTile == this) {
                gridRenderer.currentTile = null;
            }
        }

        void TileAttributeChanged(object sender, EventArgs e) {
            updateRenderer = true;
        }

        void OnDestroy() {
            if (tile != null) {
                this.tile.AttributeChanged -= new EventHandler(TileAttributeChanged);
            }
        }

        // ************************** NODE RENDERING CODE ************************** \\
        void UpdateSprite() {
			if (tile != null) {
                if (tile.HasAttribute("_demo_image")) {
                    string image = tile.GetAttribute("_demo_image");
                    string upperImage = char.ToUpper(image[0]) + image.Substring(1);
                    spriteRender.sprite = Resources.Load<Sprite>("Sprites/Images/" + upperImage);
                } else if (tile.HasAttribute("_demo_shape")) {
                    string shape = tile.GetAttribute("_demo_shape");
                    string upperShape = char.ToUpper(shape[0]) + shape.Substring(1);
                    spriteRender.sprite = Resources.Load<Sprite>("Sprites/Shapes/" + upperShape);
                } else {
                    spriteRender.sprite = Resources.Load<Sprite>("Sprites/Shapes/Square");
                }
                if (tile.HasAttribute("_demo_object")) {
                    string image = tile.GetAttribute("_demo_object");
                    string upperImage = char.ToUpper(image[0]) + image.Substring(1);
                    objRenderer.sprite = Resources.Load<Sprite>("Sprites/Images/" + upperImage);
                } else {
                    objRenderer.sprite = null;
                }
                if (tile.HasAttribute("_demo_size")) {
                    string size = tile.GetAttribute("_demo_size");
                    Vector3 scale = new Vector3(1f, 1f, 1f);
                    switch (size) {
                        case "big":
                        case "large":
                            scale = new Vector3(1.4f, 1.4f, 1f);
                            break;
                        case "huge":
                            scale = new Vector3(2f, 2f, 1f);
                            break;
                        case "small":
                            scale = new Vector3(0.6f, 0.6f, 1f);
                            break;
                        default:
                            scale = new Vector3(1f, 1f, 1f);
                            break;
                    }
                    if (tile.HasAttribute("_demo_object")) {
                        transform.localScale = new Vector3(1f, 1f, 1f);
                        objRenderer.transform.localScale = scale;
                    } else {
                        transform.localScale = scale;
                        objRenderer.transform.localScale = new Vector3(1f, 1f, 1f);
                    }
                }
                if (tile.HasAttribute("_demo_color") && !tile.HasAttribute("_demo_image")) {
                    string color = tile.GetAttribute("_demo_color");
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
                        case "black":
                            spriteRender.color = Color.black;
                            break;
                        case "white":
                            spriteRender.color = Color.white;
                            break;
                        case "gray":
                            spriteRender.color = Color.gray;
                            break;
                        default:
                            spriteRender.color = Color.gray;
                            break;
                    }
                } else {
                    if (tile.HasAttribute("_demo_image")) {
                        spriteRender.color = Color.white;
                    } else {
                        spriteRender.color = Color.gray;
                    }
                }
			}
		}
    }
}
 