using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Grammars.Graph;
using Grammars;
using System;
using UnityEngine.EventSystems;

namespace Demo {
    public class GraphDemoController : MonoBehaviour {
        EventSystem system;

        public GraphRenderer currentGraphRenderer;
        public bool paused = false;

        public GameObject attributePopUp;
        public InputField attributeNameField;
        public InputField attributeValueField;

        public Node currentNode;
        
        // Use this for initialization
        void Start() {
            system = EventSystem.current;
        }

        // Update is called once per frame
        void Update() {
            if (Input.GetKeyDown(KeyCode.A) && currentGraphRenderer.currentNode != null && !paused) {
                OpenAttributePopUp();
            }
            if (paused && attributePopUp.activeSelf) {
                if (Input.GetKeyDown(KeyCode.Tab)) {
                    Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();

                    if (next != null) {
                        InputField inputfield = next.GetComponent<InputField>();
                        if (inputfield != null)
                            inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
                        system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
                    }
                }
                if (Input.GetKeyDown(KeyCode.Return)) {
                    CloseAttributePopUp();
                }
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    CancelAttributePopUp();
                }
            }
        }

        public void OpenAttributePopUp() {
            attributeNameField.text = "";
            attributeValueField.text = "";
            currentNode = currentGraphRenderer.currentNode.GetNode();
            if (currentNode != null) {
                paused = true;
                attributePopUp.SetActive(true);
                attributeNameField.OnPointerClick(new PointerEventData(system));
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

        public void CancelAttributePopUp() {
            attributeNameField.text = "";
            attributeValueField.text = "";
            paused = false;
            attributePopUp.SetActive(false);
        }
    }
}