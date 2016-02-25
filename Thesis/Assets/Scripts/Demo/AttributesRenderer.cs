using UnityEngine;
using UnityEngine.UI;
using Grammars;
using Grammars.Graph;
using System.Collections;
using System.Collections.Generic;

namespace Demo {
    [RequireComponent(typeof(Text))]
    public class AttributesRenderer : MonoBehaviour {
        public GraphRenderer graphRenderer;
        AttributedElement currentlyDisplaying = null;
        Text text;

		// Use this for initialization
		void Start() {
            text = GetComponent<Text>();
            text.text = "";
		}

		// Update is called once per frame
		void Update() {
            if (graphRenderer.controller.paused) {
                if (graphRenderer.controller.currentElement == null) {
                    currentlyDisplaying = null;
                    SetText(null);
                } else if (graphRenderer.controller.currentElement != null && currentlyDisplaying != graphRenderer.controller.currentElement) {
                    currentlyDisplaying = graphRenderer.controller.currentElement;
                    SetText(currentlyDisplaying);
                }
            } else {
                if (graphRenderer.currentNode == null && graphRenderer.currentEdge == null) {
                    currentlyDisplaying = null;
                    SetText(null);
                } else if (graphRenderer.currentNode != null && currentlyDisplaying != graphRenderer.currentNode.GetNode()) {
                    currentlyDisplaying = graphRenderer.currentNode.GetNode();
                    SetText(currentlyDisplaying);
                } else if (graphRenderer.currentEdge != null && currentlyDisplaying != graphRenderer.currentEdge.GetEdge()) {
                    currentlyDisplaying = graphRenderer.currentEdge.GetEdge();
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
            text.text = textString;
        }
	}
}