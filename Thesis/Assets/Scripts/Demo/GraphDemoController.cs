using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Grammars.Graph;
using Grammars;
using System;

namespace Demo {
    public class GraphDemoController : MonoBehaviour {
        public GraphRenderer currentGraphRenderer;
        public bool paused = false;

        public GameObject attributePopUp;
        public InputField attributeNameField;
        public InputField attributeValueField;

        public Node currentNode;
        
        // Use this for initialization
        void Start() {
            
        }

        // Update is called once per frame
        void Update() {
            if (Input.GetKeyDown(KeyCode.A) && currentGraphRenderer.currentNode != null && !paused) {
                OpenAttributePopUp();
            }
        }

        public void OpenAttributePopUp() {
            attributeNameField.text = "";
            attributeValueField.text = "";
            currentNode = currentGraphRenderer.currentNode.GetNode();
            if (currentNode != null) {
                paused = true;
                attributePopUp.SetActive(true);
            }
        }

        public void CloseAttributePopUp() {
            string name = attributeNameField.text.Trim();
            string value = attributeValueField.text.Trim();
            if (name != "" && value != "") {
                currentNode.SetAttribute(name, value);
            }
            paused = false;
            attributePopUp.SetActive(false);
        }
    }
}