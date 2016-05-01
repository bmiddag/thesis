using System;
using System.Collections.Generic;
using Grammars.Events;
using System.Threading;
using System.Linq;

namespace Grammars {
    public class Traverser<T> : AttributedElement, IGrammarEventHandler
        where T : StructureModel {
        public override string LinkType {
            get { return "traverser"; }
            set { }
        }

        protected Dictionary<string, IGrammarEventHandler> listeners;
        protected Dictionary<string, TaskProcessor> taskProcessors;
        protected Dictionary<string, T> queries;

        protected AttributedElement currentElement;
        public AttributedElement CurrentElement {
            get { return currentElement; }
            set { currentElement = value; }
        }

        protected string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        protected T source;
        public virtual T Source {
            get {
                if (source == null) {
                    if (listeners.ContainsKey("origin")) {
                        Dictionary<string, string> stringParameters = new Dictionary<string, string>();
                        stringParameters.Add("specifier", "source.this");
                        List<object> replies = SendGrammarEvent("GetStructure",
                            replyExpected: true,
                            source: this,
                            targets: new string[] { "origin" },
                            stringParameters: stringParameters);
                        if (replies != null && replies.Count > 0 && replies[0] != null && typeof(T).IsAssignableFrom(replies[0].GetType())) {
                            source = (T)replies[0];
                        }
                    }
                }
                return source; }
            set { source = value; }
        }

        protected Type transformerType;
        public IStructureTransformer<T> Transformer {
            get {
                if (transformerType != null) {
                    IStructureTransformer<T> newTrans = (IStructureTransformer<T>)(Activator.CreateInstance(transformerType));
                    newTrans.Source = Source;
                    newTrans.Traverser = this;
                    return newTrans;
                } else return null;
            }
            set {
                if (value != null) {
                    transformerType = value.GetType();
                } else {
                    transformerType = null;
                }
            }
        }

        public override IElementContainer Container {
            get { return this; }
        }

        public Traverser(string name, Type transformerType = null) {
            this.name = name;
            this.transformerType = transformerType;
            currentElement = null;
            source = null;
            listeners = new Dictionary<string, IGrammarEventHandler>();
            queries = new Dictionary<string, T>();
            taskProcessors = new Dictionary<string, TaskProcessor>();
        }

        public virtual void GenerateMore() {
            // Send an event to generate more
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("currentElement", CurrentElement);

            List<object> replies = SendGrammarEvent("GenerateNext",
                replyExpected: true,
                source: this,
                targets: new string[] { "origin" },
                objectParameters: parameters);
            if (replies == null || replies.Count == 0) {
                // Error
                return;
            }
        }

        public virtual void SetFirstElement() {
            if (currentElement == null) {
                T src = Source;
                List<AttributedElement> possibleStarts = src.GetElements();
                if (possibleStarts != null && possibleStarts.Count > 0) {
                    IEnumerable<AttributedElement> markedStarts = possibleStarts.Where(e => e.HasAttribute("start"));
                    if (markedStarts != null && markedStarts.Count() > 0) possibleStarts = markedStarts.ToList();
                } else {
                    // Send an event to generate
                    List<object> replies = SendGrammarEvent("GenerateNext",
                        replyExpected: true,
                        source: this,
                        targets: new string[] { "origin" });
                    if (replies == null || replies.Count == 0) {
                        return;
                    }
                    possibleStarts = Source.GetElements();
                    if (possibleStarts != null && possibleStarts.Count > 0) {
                        IEnumerable<AttributedElement> markedStarts = possibleStarts.Where(e => e.HasAttribute("start"));
                        if (markedStarts != null && markedStarts.Count() > 0) possibleStarts = markedStarts.ToList();
                    } else return;
                }
                Random rand = new Random();
                CurrentElement = possibleStarts[rand.Next(0,possibleStarts.Count)];
            }
        }

        protected IDictionary<string, AttributedElement> Find(T query, bool nextIfNull=true) {
            IStructureTransformer<T> transformer = Transformer;
            if (nextIfNull && CurrentElement == null) {
                Task t = new Task("First");
                GetTaskProcessor("Next").Process(t);
                //SetFirstElement();
            }
            //UnityEngine.MonoBehaviour.print(CurrentElement);
            bool found = transformer.Find(query);
            if (found) {
                //UnityEngine.MonoBehaviour.print("Traverser found a match!");
                transformer.Select();
                return transformer.SelectedMatch;
            } else return null;
        }

        public virtual void ExecuteTask(Task task) {
            if (task == null) return;
            switch (task.Action) {
                case "Next":
                case "NextFrom":
                    if (GetTaskProcessor("Next") != null) {
                        GetTaskProcessor("Next").Process(task);
                    }
                    break;
                case "CheckMatch":
                case "Checkmatch":
                case "Match":
                case "Find":
                    if (task.HasAttribute("query") && GetQuery(task["query"]) != null) {
                        bool nextIfNull = true;
                        if (task.HasAttribute("noCurrent")) nextIfNull = false;
                        IDictionary<string, AttributedElement> matches = Find(GetQuery(task["query"]), nextIfNull: nextIfNull);
                        task.AddReply(matches);
                    } else {
                        task.AddReply(null);
                    }
                    break;
            }
        }

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null) {
                switch (key) {
                    case "_name":
                        result = name; break;
                }
            }
            return result;
        }

        public override List<AttributedElement> GetElements(string specifier = null) {
            IElementContainer subcontainer = null;
            string passSpecifier = specifier;

            if (specifier != null && specifier.Contains(".")) {
                string subcontainerStr = specifier.Substring(0,specifier.IndexOf("."));
                if (listeners.ContainsKey(subcontainerStr)) {
                    passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("specifier", passSpecifier);
                    List<object> replies = SendGrammarEvent("GetElements",
                        replyExpected: true,
                        targets: new string[] { subcontainerStr },
                        stringParameters: parameters);
                    if (replies != null && replies.Count > 0) {
                        return (List<AttributedElement>)replies[0];
                    } else return null;
                } else {
                    switch (subcontainerStr) {
                        case "source":
                            subcontainer = Source; break;
                    }
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                List<AttributedElement> attrList = new List<AttributedElement>();
                if (specifier == "current") {
                    attrList.Add(CurrentElement);
                    return attrList;
                }
                return base.GetElements(specifier);
            }
        }

        public virtual void HandleGrammarEvent(Task task) {
            if (task == null) return;
            UnityEngine.MonoBehaviour.print("[" + name + "]" + " Received event: " + task.Action);
            try {
                if (GetTaskProcessor(task.Action) != null) {
                    GetTaskProcessor(task.Action).Process(task);
                } else if (task.ReplyExpected) {
                    switch (task.Action) {
                        case "GetElements":
                            if (task.HasAttribute("specifier")) {
                                task.AddReply(GetElements(task["specifier"]));
                            } else {
                                task.AddReply(GetElements());
                            }
                            break;
                        case "SetCurrentElement":
                            if (task.HasObjectAttribute("element")) {
                                AttributedElement el = (AttributedElement)task.GetObjectAttribute("element");
                                CurrentElement = el;
                            } else if (task.HasAttribute("element")) {
                                AttributedElement el = Source.GetElement("element");
                                CurrentElement = el;
                            }
                            task.AddReply(CurrentElement);
                            break;
                        case "GetCurrentElement":
                            task.AddReply(CurrentElement);
                            break;
                        default:
                            ExecuteTask(task);
                            break;
                    }
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogError(e.Message + e.StackTrace);
            }
        }

        public virtual List<object> SendGrammarEvent(Task task) {
            if (task == null) return null;
            UnityEngine.MonoBehaviour.print("[" + name + "]" + " Sending event: " + task.Action);
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

        public void AddTaskProcessor(string eventName, TaskProcessor tProc) {
            taskProcessors.Add(eventName, tProc);
        }

        public void RemoveTaskProcessor(string eventName) {
            if (taskProcessors.ContainsKey(eventName)) {
                taskProcessors.Remove(eventName);
            }
        }

        public void AddQuery(string qName, T query) {
            queries.Add(qName, query);
        }

        public void RemoveQuery(string qName) {
            if (queries.ContainsKey(qName)) {
                queries.Remove(qName);
            }
        }

        public T GetQuery(string qName) {
            if (queries.ContainsKey(qName)) {
                return queries[qName];
            } else return null;
        }

        public TaskProcessor GetTaskProcessor(string eventName) {
            if (taskProcessors.ContainsKey(eventName)) {
                return taskProcessors[eventName];
            } else return null;
        }

        public IGrammarEventHandler GetListener(string name) {
            if (listeners.ContainsKey(name)) {
                return listeners[name];
            } else return null;
        }
    }
}
