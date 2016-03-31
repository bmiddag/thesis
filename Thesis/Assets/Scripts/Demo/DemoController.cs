using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Grammars.Graph;
using Grammars;
using System;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using Grammars.Tile;
using Grammars.Events;
using System.Threading;

namespace Demo {
    public class DemoController : MonoBehaviour, IGrammarEventHandler {
        EventSystem system;

        public IStructureRenderer currentStructureRenderer;
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

        Dictionary<string, IGrammarEventHandler> listeners = new Dictionary<string, IGrammarEventHandler>();

        IDictionary<string, AttributeClass> attributeClasses = new Dictionary<string, AttributeClass>(); // TODO: move to grammar

        public string Name {
            get { return "DemoController"; }

            set {}
        }

        // Use this for initialization
        void Start() {
            system = EventSystem.current;
            //if (currentStructureRenderer == null) currentStructureRenderer = FindObjectOfType<GraphRenderer>();
            //if (currentStructureRenderer == null) currentStructureRenderer = FindObjectOfType<TileGridRenderer>();

            // Define some attribute classes
            /*attributeClasses["yellow_triangles"] = new AttributeClass("yellow_triangles");
            attributeClasses["blue_squares"] = new AttributeClass("blue_squares");
            attributeClasses["white_circles"] = new AttributeClass("white_circles");

            attributeClasses["yellow_triangles"].SetAttribute("_demo_shape", "triangle");
            attributeClasses["yellow_triangles"].SetAttribute("_demo_color", "yellow");
            attributeClasses["blue_squares"].SetAttribute("_demo_shape", "square");
            attributeClasses["blue_squares"].SetAttribute("_demo_color", "blue");
            attributeClasses["white_circles"].SetAttribute("_demo_shape", "circle");
            attributeClasses["white_circles"].SetAttribute("_demo_color", "white");*/

            StartCoroutine("LoadAttributeClasses");
        }

        // TODO: Move to grammar
        public void AddAttributeClass(AttributedElement el, string className) {
            if (el != null && className != null && className != "" && attributeClasses.ContainsKey(className)) {
                el.AddAttributeClass(attributeClasses[className]);
            }
        }

        public void RegisterStructureRenderer(IStructureRenderer structRen) {
            currentStructureRenderer = structRen;
        }

        // Update is called once per frame
        void Update() {
            if (!paused) {
                if (Input.GetKeyDown(KeyCode.D)) {
                    Scene activeScene = SceneManager.GetActiveScene();
                    if (activeScene.name == "GraphDemo") {
                        SceneManager.LoadScene("TileDemo");
                    } else if (activeScene.name == "TileDemo") {
                        SceneManager.LoadScene("GraphDemo");
                    }
                }
                if (Input.GetKeyDown(KeyCode.S)) {
                    StartCoroutine("SaveStructure");
                }
                if (Input.GetKeyDown(KeyCode.L)) {
                    StartCoroutine("LoadStructure");
                }
                if (Input.GetKeyDown(KeyCode.R)) {
                    StartCoroutine("LoadGrammar");
                }
                if (Input.GetKeyDown(KeyCode.U)) {
                    StartCoroutine("GrammarStep");
                }
                if (Input.GetKeyDown(KeyCode.O)) {
                    StartCoroutine("SaveAttributeClasses");
                }
                if (Input.GetKeyDown(KeyCode.P)) {
                    StartCoroutine("LoadAttributeClasses");
                }
            }
            if (currentStructureRenderer == null) return;
            if (currentStructureRenderer.CurrentElement != null && !paused && currentPopUp == null) {
                if (Input.GetKeyDown(KeyCode.A)) {
                    currentPopUp = attributePopUp;
                    OpenPopUp();
                } else if (Input.GetKeyDown(KeyCode.C)) {
                    currentPopUp = classPopUp;
                    OpenPopUp();
                } else if (Input.GetKeyDown(KeyCode.I) && currentStructureRenderer.CurrentElement.GetType() == typeof(NodeRenderer)) {
                    currentPopUp = nodeIdPopUp;
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

            currentElement = currentStructureRenderer.CurrentElement.Element;

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
                    AddAttributeClass(currentElement, name);
                }
            } else if (currentPopUp == nodeIdPopUp) {
                if (currentElement.GetType() == typeof(Node)) {
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

        public void SetGrammar<T>(Grammar<T> grammar) where T : StructureModel {
            //Type renType = currentStructureRenderer.GetType();
            if (typeof(T) != currentStructureRenderer.Source.GetType()) return;
            grammar.Source = (T)currentStructureRenderer.Source;
            currentStructureRenderer.Grammar = grammar;
            grammar.AddListener(this);
            this.AddListener(grammar);
            print("Grammar successfully set.");
            SendGrammarEvent("start", targets: new string[] { grammar.Name });
        }

        public IEnumerator GrammarStep() {
            return currentStructureRenderer.GrammarStep();
        }

        public IEnumerator SaveStructure() {
            return currentStructureRenderer.SaveStructure();
        }

        public IEnumerator LoadStructure() {
            return currentStructureRenderer.LoadStructure();
        }

        public IEnumerator LoadGrammar() {
            string dirName = "Grammars/";
            DemoIO dirSerializer = new DemoIO(dirName, this);
            List<string> grammars = dirSerializer.GetSubDirectories();
            foreach (string grmName in grammars) {
                print("Loading grammar: " + grmName);
                string filename = dirName + grmName + "/" + grmName + ".xml";
                DemoIO serializer = new DemoIO(filename, this);
                serializer.ParseGrammar();
            }
            yield return null;
        }

        public IEnumerator SaveAttributeClasses() {
            string filename = "Grammars/AttributeClasses.xml";
            DemoIO serializer = new DemoIO(filename, this);
            serializer.SerializeAttributeClasses(attributeClasses);
            print("Attribute classes saved!");
            yield return null;
        }

        public IEnumerator LoadAttributeClasses() {
            string filename = "Grammars/AttributeClasses.xml";
            DemoIO serializer = new DemoIO(filename, this);
            attributeClasses = serializer.DeserializeAttributeClasses();
            print("Attribute classes loaded!");
            yield return null;
        }

        void OnDestroy() {
            if (currentStructureRenderer.Grammar != null) {
                SendGrammarEvent("Stop", targets: new string[] { currentStructureRenderer.Grammar.Name });
            }
        }

        public void HandleGrammarEvent(Task task) {
            if (task == null) return;
            print("[" + Name + "]" + " Received event: " + task.Action);
        }

        public virtual List<object> SendGrammarEvent(Task task) {
            if (task == null) return null;
            print("[" + Name + "]" + " Sending event: " + task.Action);
            if (task.Targets.Count == 0) return null;
            List<Thread> startedThreads = new List<Thread>();
            foreach (IGrammarEventHandler target in task.Targets) {
                Thread t = new Thread(() => target.HandleGrammarEvent(task));
                startedThreads.Add(t);
                t.Start();
            }
            if (!task.ReplyExpected) {
                return null;
            } else {
                foreach (Thread thread in startedThreads) {
                    thread.Join();
                }
                if (task.ReplyCompleted) {
                    return task.Replies;
                } else return null;
            }
        }

        public List<object> SendGrammarEvent(string action, bool replyExpected = false,
            IGrammarEventHandler source = null, string[] targets = null, object[] parameters = null) {
            if (source == null) source = this;
            Task task = new Task(action, source);
            if (targets != null) {
                task.ReplyExpected = replyExpected;
                foreach (string tarStr in targets) {
                    IGrammarEventHandler target = listeners[tarStr];
                    if (target != null) {
                        task.AddTarget(target);
                    }
                }
            }
            return SendGrammarEvent(task);
        }

        public void AddListener(IGrammarEventHandler handler) {
            if (handler == null) return;
            listeners.Add(handler.Name, handler);
        }
    }
}