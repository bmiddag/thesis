using UnityEngine;
using Grammars.Graph;

namespace Demo {
	public class NodeRenderer : MonoBehaviour {
		Node node;
		string shapePath;
		SpriteRenderer spriteRender;

		// Use this for initialization
		void Start() {
			spriteRender = gameObject.AddComponent<SpriteRenderer>();
			UpdateSprite();
		}

		// Update is called once per frame
		void Update() {

		}

		// Custom function with draw code
		void UpdateSprite() {
			if (node != null) {
				if (node.hasAttribute("demo__shape")) {
					string shape = node.getAttribute("demo__shape");
					string upperShape = char.ToUpper(shape[0]) + shape.Substring(1);
					spriteRender.sprite = Resources.Load<Sprite>("Sprites/" + upperShape);
				}
				if (node.hasAttribute("demo__color")) {
					string color = node.getAttribute("demo__color");
					switch (node.getAttribute("demo__color")) {
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
			if (key.Contains("demo__")) UpdateSprite();
		}
    }
}