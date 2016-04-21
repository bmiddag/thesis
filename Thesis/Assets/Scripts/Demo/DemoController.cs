using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Grammars.Graphs;
using Grammars;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using Grammars.Events;
using System.Threading;
using System;
using Grammars.Control;
using Grammars.Tiles;

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
        List<IStructureRenderer> structureRenderers = new List<IStructureRenderer>();

        IDictionary<string, AttributeClass> attributeClasses = new Dictionary<string, AttributeClass>(); // TODO: move to grammar

        public GameObject canvas;

        public string Name {
            get { return "DemoController"; }
            set {}
        }

        // Use this for initialization
        void Start() {
            system = EventSystem.current;
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
                if (Input.GetKeyDown(KeyCode.Keypad6)) {
                    if (structureRenderers.Contains(currentStructureRenderer)) {
                        int index = structureRenderers.IndexOf(currentStructureRenderer);
                        index = (index + 1) % structureRenderers.Count;
                        currentStructureRenderer = structureRenderers[index];
                    }
                }
                if (Input.GetKeyDown(KeyCode.Keypad4)) {
                    if (structureRenderers.Contains(currentStructureRenderer)) {
                        int index = structureRenderers.IndexOf(currentStructureRenderer);
                        if (index == 0) {
                            index = structureRenderers.Count - 1;
                        } else {
                            index--;
                        }
                        currentStructureRenderer = structureRenderers[index];
                    }
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

        public void PrepareGrammar(string dirName, string grmName) {
            print("Loading grammar: " + grmName);
            string filename = dirName + grmName + "/" + grmName + ".xml";
            DemoIO serializer = new DemoIO(filename, this);
            IGrammarEventHandler eventHandler = serializer.ParseGrammar();
            //SetGrammar(eventHandler);
            bool success = false;

            if (typeof(InterGrammarController).IsAssignableFrom(eventHandler.GetType())) {
                InterGrammarController con = (InterGrammarController)eventHandler;
                Dictionary<string, IGrammarEventHandler> conListeners = con.GetListeners();
                foreach (KeyValuePair<string, IGrammarEventHandler> conListener in conListeners) {
                    CreateRenderer(conListener.Value);
                }
                success = true;
            } else if (typeof(Grammar<Graph>).IsAssignableFrom(eventHandler.GetType()) && currentStructureRenderer != null) {
                Grammar<Graph> grammar = (Grammar<Graph>)eventHandler;
                SetGrammar(currentStructureRenderer, grammar);
                success = true;
            } else if (typeof(Grammar<TileGrid>).IsAssignableFrom(eventHandler.GetType()) && currentStructureRenderer != null) {
                Grammar<TileGrid> grammar = (Grammar<TileGrid>)eventHandler;
                SetGrammar(currentStructureRenderer, grammar);
                success = true;
            }
            if (success) {
                eventHandler.AddListener(this);
                AddListener(eventHandler);
                print("Grammar successfully set.");
                SendGrammarEvent("Start", targets: new string[] { eventHandler.Name });
            }
        }

        public void CreateRenderer(IGrammarEventHandler eventHandler) {
            if (typeof(Grammar<Graph>).IsAssignableFrom(eventHandler.GetType())) {
                Grammar<Graph> grammar = (Grammar<Graph>)eventHandler;
                GraphRenderer graphRen = new GameObject().AddComponent<GraphRenderer>();
                graphRen.controller = this;
                graphRen.cameraControl = FindObjectOfType<CameraControl>();
                if (canvas != null) graphRen.transform.SetParent(canvas.transform);
                graphRen.gameObject.name = grammar.Name;
                graphRen.transform.localPosition = new Vector3(structureRenderers.Count * 1000, 0);
                SetGrammar(graphRen, grammar);
                structureRenderers.Add(graphRen);
            } else if (typeof(Grammar<TileGrid>).IsAssignableFrom(eventHandler.GetType())) {
                Grammar<TileGrid> grammar = (Grammar<TileGrid>)eventHandler;
                TileGridRenderer gridRen = new GameObject().AddComponent<TileGridRenderer>();
                gridRen.controller = this;
                gridRen.cameraControl = FindObjectOfType<CameraControl>();
                if (canvas != null) gridRen.transform.SetParent(canvas.transform);
                gridRen.gameObject.name = grammar.Name;
                SetGrammar(gridRen, grammar);
                structureRenderers.Add(gridRen);
            }
        }

        public void SetGrammar<T>(IStructureRenderer renderer, Grammar<T> grammar) where T : StructureModel {
            //Type renType = currentStructureRenderer.GetType();
            if (typeof(T) != renderer.Source.GetType()) return;
            grammar.Source = (T)renderer.Source;
            renderer.Source.LinkType = grammar.Name;
            renderer.Grammar = grammar;
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
            if (currentStructureRenderer == null) {
                PrepareGrammar("Grammars/", "controller");
            } else if(currentStructureRenderer.GetType() == typeof(GraphRenderer)) {
                PrepareGrammar("Grammars/", "mission");
            } else if (currentStructureRenderer.GetType() == typeof(GraphRenderer)) {
                PrepareGrammar("Grammars/", "tilespace");
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
            foreach (KeyValuePair<string, IGrammarEventHandler> listener in listeners) {
                SendGrammarEvent("Stop", targets: new string[] { listener.Key });
            }
            /*if (currentStructureRenderer != null && currentStructureRenderer.Grammar != null) {
                SendGrammarEvent("Stop", targets: new string[] { currentStructureRenderer.Grammar.Name });
            }*/
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
            IGrammarEventHandler source = null, string[] targets = null,
            Dictionary<string, string> stringParameters = null, Dictionary<string, object> objectParameters = null) {
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
            if (stringParameters != null) {
                foreach (KeyValuePair<string, string> pair in stringParameters) {
                    task.SetAttribute(pair.Key, pair.Value);
                }
            }
            if (objectParameters != null) {
                foreach (KeyValuePair<string, object> pair in objectParameters) {
                    task.SetObjectAttribute(pair.Key, pair.Value);
                }
            }
            return SendGrammarEvent(task);
        }

        public void AddListener(IGrammarEventHandler handler, string name = null) {
            if (handler == null) return;
            if (name == null) name = handler.Name;
            listeners.Add(name, handler);
        }

        public IGrammarEventHandler GetListener(string name) {
            if (listeners.ContainsKey(name)) {
                return listeners[name];
            } else return null;
        }
    }
}