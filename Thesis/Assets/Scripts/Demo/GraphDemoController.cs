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

        public GameObject classPopUp;
        public InputField classNameField;

        public GameObject nodeIdPopUp;
        public InputField nodeIdField;

        GameObject currentPopUp = null;

        public AttributedElement currentElement;
        
        // Use this for initialization
        void Start() {
            system = EventSystem.current;
        }

        // Update is called once per frame
        void Update() {
            if ((currentGraphRenderer.currentNode != null || currentGraphRenderer.currentEdge != null) && !paused && currentPopUp == null) {
                if (Input.GetKeyDown(KeyCode.A)) {
                    currentPopUp = attributePopUp;
                    OpenPopUp();
                } else if (Input.GetKeyDown(KeyCode.I) && currentGraphRenderer.currentNode != null) {
                    currentPopUp = nodeIdPopUp;
                    OpenPopUp();
                } else if (Input.GetKeyDown(KeyCode.C)) {
                    currentPopUp = classPopUp;
                    OpenPopUp();
                }
            }
            if (paused && currentPopUp != null && currentPopUp.activeSelf) {
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
                    SavePopUp();
                } else if (Input.GetKeyDown(KeyCode.Escape)) {
                    ClosePopUp();
                }
            }
        }

        public void OpenPopUp() {
            attributeNameField.text = "";
            attributeValueField.text = "";
            classNameField.text = "";
            nodeIdField.text = "";

            if (currentGraphRenderer.currentNode != null) {
                currentElement = currentGraphRenderer.currentNode.GetNode();
            } else if (currentGraphRenderer.currentEdge != null) {
                currentElement = currentGraphRenderer.currentEdge.GetEdge();
            } else currentElement = null;

            if (currentElement != null) {
                paused = true;
                currentPopUp.SetActive(true);
                if (currentPopUp == attributePopUp) {
                    attributeNameField.OnPointerClick(new PointerEventData(system));
                } else if (currentPopUp == classPopUp) {
                    classNameField.OnPointerClick(new PointerEventData(system));
                } else if (currentPopUp == nodeIdPopUp) {
                    nodeIdField.OnPointerClick(new PointerEventData(system));
                }
            }
        }

        public void SavePopUp() {
            if (currentPopUp == attributePopUp) {
                string name = attributeNameField.text.Trim();
                string value = attributeValueField.text.Trim();
                if (name != "" && value != "") {
                    currentElement.SetAttribute(name, value);
                }
            } else if (currentPopUp == classPopUp) {
                string name = classNameField.text.Trim();
                if (name != "") {
                    currentGraphRenderer.AddAttributeClass(currentElement, name);
                }
            } else if (currentPopUp == nodeIdPopUp) {
                if(currentElement.GetType() == typeof(Node)) {
                    string id = nodeIdField.text.Trim();
                    int parsedId;
                    if (int.TryParse(id, out parsedId)) {
                        ((Node)currentElement).SetID(parsedId);
                    }
                }
            }
            ClosePopUp();
        }

        public void ClosePopUp() {
            attributeNameField.text = "";
            attributeValueField.text = "";
            classNameField.text = "";
            nodeIdField.text = "";
            paused = false;
            currentPopUp.SetActive(false);
            currentPopUp = null;
        }
    }
}