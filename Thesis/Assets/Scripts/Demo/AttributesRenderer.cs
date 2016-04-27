using UnityEngine;
using UnityEngine.UI;
using Grammars;
using Grammars.Graphs;
using System.Collections.Generic;
using Grammars.Tiles;

namespace Demo {
    [RequireComponent(typeof(Text))]
    public class AttributesRenderer : MonoBehaviour {
        public DemoController controller;
        AttributedElement currentlyDisplaying = null;
        Text text;

		// Use this for initialization
		void Start() {
            text = GetComponent<Text>();
            text.text = "";
		}

		// Update is called once per frame
		void Update() {
            if (controller.paused) {
                AttributedElement controllerElement = controller.currentElement;
                if (controllerElement == null) {
                    currentlyDisplaying = null;
                    SetText(null);
                } else if (controllerElement != null && currentlyDisplaying != controllerElement) {
                    currentlyDisplaying = controllerElement;
                    SetText(currentlyDisplaying);
                }
            } else if(controller.currentStructureRenderer != null) {
                IElementRenderer hoveringElement = controller.currentStructureRenderer.CurrentElement;
                if (hoveringElement == null) {
                    currentlyDisplaying = null;
                    SetText(null);
                } else if (hoveringElement != null && currentlyDisplaying != hoveringElement.Element) {
                    currentlyDisplaying = hoveringElement.Element;
                    SetText(currentlyDisplaying);
                }
            }
		}

        void SetText(AttributedElement el) {
            string textString = "";
            if (el == null) {
                text.text = textString;
                return;
            }
            if(el.GetType() == typeof(Node)) {
                textString += "<b>ID:</b>\n";
                Node node = (Node)el;
                textString += "\t" + node.GetID().ToString() + "\n";
                textString += "\n";
            } else if (el.GetType() == typeof(Edge)) {
                textString += "<b>Node IDs:</b>\n";
                Edge edge = (Edge)el;
                
                textString += "\t" + edge.GetNode1().GetID().ToString() + " - " + edge.GetNode2().GetID().ToString() + "\n";
                textString += "\n";
            } else if (el.GetType() == typeof(Tile)) {
                textString += "<b>Position:</b>\n";
                Tile tile = (Tile)el;

                textString += "\t" + tile.GetIndices().x + " - " + tile.GetIndices().y + "\n";
                textString += "\n";
            }
            textString += "<b>Classes:</b>\n";
            HashSet<AttributeClass> attClasses = el.GetAttributeClasses();
            foreach (AttributeClass attClass in attClasses) {
                textString += "\t" + attClass.GetName() + "\n";
            }
            textString += "\n";
            textString += "<b>Attributes:</b>\n";
            IDictionary<string, string> attDict = el.GetAttributes();
            foreach (KeyValuePair<string, string> att in attDict) {
                textString += "\t" + att.Key + ": <i>" + att.Value + "</i>\n";
            }

            IDictionary<string, List<AttributedElement>> links = el.GetLinks();
            if (links.Count > 0) {
                textString += "\n";
                textString += "<b>Links:</b>\n";
                foreach (KeyValuePair<string, List<AttributedElement>> linkList in links) {
                    textString += "\t" + linkList.Key + ":\n";
                    foreach (AttributedElement linkEl in linkList.Value) {
                        List<AttributeClass> cls = new List<AttributeClass>(linkEl.GetAttributeClasses());
                        if (cls.Count > 0) {
                            textString += "\t<i>- " + linkEl.GetType().Name + ": ";
                            for(int i = 0; i < cls.Count; i++) {
                                AttributeClass cl = cls[i];
                                textString += cl.GetName();
                                if (i < cls.Count - 1) textString += ", ";
                            }
                            textString += "</i>\n";
                        } else {
                            textString += "\t<i>- " + linkEl.GetType().Name + "</i>\n";
                        }
                    }
                }
            }
            text.text = textString;
        }
	}
}