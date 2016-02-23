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
                if (graphRenderer.controller.currentNode == null) {
                    currentlyDisplaying = null;
                    SetText(null);
                } else if (currentlyDisplaying != graphRenderer.controller.currentNode) {
                    currentlyDisplaying = graphRenderer.controller.currentNode;
                    SetText(currentlyDisplaying);
                }
            } else {
                if (graphRenderer.currentNode == null) {
                    currentlyDisplaying = null;
                    SetText(null);
                } else if (currentlyDisplaying != graphRenderer.currentNode.GetNode()) {
                    currentlyDisplaying = graphRenderer.currentNode.GetNode();
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