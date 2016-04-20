using Grammars;
using Grammars.Control;
using Grammars.Events;
using Grammars.Graphs;
using Grammars.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Demo {
    public class DemoIO {
        string filename;
        XmlWriterSettings settings;
        DemoController controller;
        private static Dictionary<string, IGrammarEventHandler> tempLoaded = new Dictionary<string, IGrammarEventHandler>();
        private static Dictionary<IGrammarEventHandler, Dictionary<string, string>> tempListeners = new Dictionary<IGrammarEventHandler, Dictionary<string, string>>();
        private static Dictionary<Task, Dictionary<string, List<string>>> tempTaskListeners = new Dictionary<Task, Dictionary<string, List<string>>>();

        public DemoIO(string filename, DemoController controller, bool clearDictionaries = true) {
            this.filename = filename;
            this.controller = controller;
            settings = new XmlWriterSettings {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            if (clearDictionaries) {
                tempLoaded.Clear();
                tempListeners.Clear();
                tempTaskListeners.Clear();
            }
        }

        void SerializeAttributedElement(XmlWriter writer, AttributedElement el) {
            // Save attribute classes
            if (el.GetAttributeClasses().Count > 0) {
                writer.WriteStartElement("AttributeClasses");
                foreach (AttributeClass cl in el.GetAttributeClasses()) {
                    if (el.GetAttributeClassSource(cl) == el) {
                        writer.WriteStartElement("AttributeClass");
                        writer.WriteAttributeString("name", cl.GetName());
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }

            // Save attributes
            if (el.GetAttributes(raw:true).Count > 0) {
                writer.WriteStartElement("Attributes");
                foreach (KeyValuePair<string, string> att in el.GetAttributes(raw:true)) {
                    if (el.GetAttributeSource(att.Key) == el) {
                        writer.WriteStartElement("Attribute");
                        writer.WriteAttributeString("key", att.Key);
                        writer.WriteAttributeString("value", att.Value);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }

        public void SerializeGrid(TileGrid grid) {
            using (XmlWriter writer = XmlWriter.Create(filename, settings)) {
                writer.WriteStartDocument();
                writer.WriteStartElement("TileGrid");
                int w = grid.GetGridSize().x;
                int h = grid.GetGridSize().y;
                writer.WriteAttributeString("width", w.ToString());
                writer.WriteAttributeString("height", h.ToString());
                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        Tile tile = grid.GetTile(x, y);
                        if (tile != null) {
                            writer.WriteStartElement("Tile");
                            writer.WriteAttributeString("x", x.ToString());
                            writer.WriteAttributeString("y", y.ToString());
                            SerializeAttributedElement(writer, tile);
                            writer.WriteEndElement(); // Tile
                        }
                    }
                }
                SerializeAttributedElement(writer, grid);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public TileGrid DeserializeGrid() {
            // Create an XML reader for this file.
            TileGrid grid = null;
            AttributedElement currentElement = null;
            using (XmlReader reader = XmlReader.Create(filename)) {
                while (reader.Read()) {
                    switch(reader.NodeType) {
                        case XmlNodeType.Element:
                            // Get element name and switch on it.
                            switch (reader.Name) {
                                case "TileGrid":
                                    string wStr = reader["width"];
                                    string hStr = reader["height"];
                                    if (wStr == null || hStr == null) return null; // Deserialization failed
                                    grid = new TileGrid(int.Parse(wStr), int.Parse(hStr));
                                    if(!reader.IsEmptyElement) currentElement = grid;
                                    break;
                                case "Tile":
                                    string xStr = reader["x"];
                                    string yStr = reader["y"];
                                    if (xStr == null || yStr == null) return null; // Deserialization failed
                                    Tile tile = new Tile(grid, int.Parse(xStr), int.Parse(yStr));
                                    if (!reader.IsEmptyElement) currentElement = tile;
                                    break;
                                case "AttributeClass":
                                    string name = reader["name"];
                                    if (name == null || currentElement == null) return null; // Deserialization failed
                                    controller.AddAttributeClass(currentElement, name);
                                    break;
                                case "Attribute":
                                    string key = reader["key"];
                                    string val = reader["value"];
                                    if (key == null || val == null) return null; // Deserialization failed
                                    currentElement.SetAttribute(key, val);
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "Tile") {
                                currentElement = grid;
                            }
                            break;
                    }
                }
            }
            return grid;
        }

        public void SerializeGraph(Graph graph) {
            using (XmlWriter writer = XmlWriter.Create(filename, settings)) {
                writer.WriteStartDocument();
                writer.WriteStartElement("Graph");

                // Save nodes first
                writer.WriteStartElement("Nodes");
                foreach (Node node in graph.GetNodes()) {
                    writer.WriteStartElement("Node");
                    writer.WriteAttributeString("id", node.GetID().ToString());
                    writer.WriteAttributeString("hashCode", node.GetHashCode().ToString());
                    writer.WriteAttributeString("active", node.Active.ToString());
                    SerializeAttributedElement(writer, node);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                // Then save edges
                writer.WriteStartElement("Edges");
                foreach (Edge edge in graph.GetEdges()) {
                    writer.WriteStartElement("Edge");
                    writer.WriteAttributeString("node1", edge.GetNode1().GetHashCode().ToString()); // ID is assumed unique
                    writer.WriteAttributeString("node2", edge.GetNode2().GetHashCode().ToString()); // ID is assumed unique
                    writer.WriteAttributeString("directed", edge.IsDirected().ToString());
                    SerializeAttributedElement(writer, edge);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                SerializeAttributedElement(writer, graph);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public Graph DeserializeGraph() {
            // Create an XML reader for this file.
            Graph graph = new Graph();
            AttributedElement currentElement = graph;
            Dictionary<int, Node> hashDict = new Dictionary<int, Node>();
            using (XmlReader reader = XmlReader.Create(filename)) {
                while (reader.Read()) {
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            // Get element name and switch on it.
                            switch (reader.Name) {
                                case "Node":
                                    string hash = reader["hashCode"];
                                    string id = reader["id"];
                                    string active = reader["active"];
                                    if (hash == null || id == null || active == null) return null; // Deserialization failed
                                    Node node = new Node(graph, int.Parse(id));
                                    hashDict.Add(int.Parse(hash), node);
                                    if (!reader.IsEmptyElement) currentElement = node;
                                    break;
                                case "Edge":
                                    string node1Hash = reader["node1"];
                                    string node2Hash = reader["node2"];
                                    string directed = reader["directed"];
                                    if (node1Hash == null || node2Hash == null || directed == null) return null; // Deserialization failed
                                    if (!hashDict.ContainsKey(int.Parse(node1Hash)) || !hashDict.ContainsKey(int.Parse(node2Hash))) return null;
                                    Node node1 = hashDict[int.Parse(node1Hash)];
                                    Node node2 = hashDict[int.Parse(node2Hash)];
                                    Edge edge = new Edge(graph, node1, node2, bool.Parse(directed));
                                    if (!reader.IsEmptyElement) currentElement = edge;
                                    break;
                                case "AttributeClass":
                                    string name = reader["name"];
                                    if (name == null || currentElement == null) return null; // Deserialization failed
                                    controller.AddAttributeClass(currentElement, name);
                                    break;
                                case "Attribute":
                                    string key = reader["key"];
                                    string val = reader["value"];
                                    if (key == null || val == null) return null; // Deserialization failed
                                    currentElement.SetAttribute(key, val);
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "Nodes" || reader.Name == "Edges") {
                                currentElement = graph;
                            }
                            break;
                    }
                }
            }
            return graph;
        }

        public Task DeserializeTask() {
            // Create an XML reader for this file.
            Task task = null;
            AttributedElement currentElement = null;
            using (XmlReader reader = XmlReader.Create(filename)) {
                while (reader.Read()) {
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            string name;
                            // Get element name and switch on it.
                            switch (reader.Name) {
                                case "Task":
                                    string action = reader["action"];
                                    if (action == null) throw new System.FormatException("Deserialization failed"); // Deserialization failed
                                    task = new Task(action: action);
                                    if (!reader.IsEmptyElement) currentElement = task;
                                    break;
                                case "Source":
                                    name = reader["name"];
                                    if (name == null) {
                                        if (!reader.IsEmptyElement) {
                                            reader.Read();
                                            name = reader.Value;
                                        }
                                    }
                                    if (name != null) {
                                        if (!tempTaskListeners.ContainsKey(task)) {
                                            tempTaskListeners.Add(task, new Dictionary<string, List<string>>());
                                        }
                                        if (!tempTaskListeners[task].ContainsKey("source")) {
                                            tempTaskListeners[task].Add("source", new List<string>());
                                        }
                                        tempTaskListeners[task]["source"].Add(name);
                                    } else {
                                        throw new System.FormatException("Deserialization failed"); // Deserialization failed
                                    }
                                    break;
                                case "Target":
                                    name = reader["name"];
                                    if (name == null) {
                                        if (!reader.IsEmptyElement) {
                                            reader.Read();
                                            name = reader.Value;
                                        }
                                    }
                                    if (name != null) {
                                        if (!tempTaskListeners.ContainsKey(task)) {
                                            tempTaskListeners.Add(task, new Dictionary<string, List<string>>());
                                        }
                                        if (!tempTaskListeners[task].ContainsKey("target")) {
                                            tempTaskListeners[task].Add("target", new List<string>());
                                        }
                                        tempTaskListeners[task]["target"].Add(name);
                                    } else {
                                        throw new System.FormatException("Deserialization failed"); // Deserialization failed
                                    }
                                    break;
                                case "AttributeClass":
                                    name = reader["name"];
                                    if (name == null || currentElement == null) return null; // Deserialization failed
                                    controller.AddAttributeClass(currentElement, name);
                                    break;
                                case "Attribute":
                                    string key = reader["key"];
                                    string val = reader["value"];
                                    if (key == null || val == null) return null; // Deserialization failed
                                    currentElement.SetAttribute(key, val);
                                    break;
                            }
                            break;
                    }
                }
            }
            return task;
        }

        public void SerializeAttributeClasses(IDictionary<string, AttributeClass> classesDict) {
            using (XmlWriter writer = XmlWriter.Create(filename, settings)) {
                writer.WriteStartDocument();
                writer.WriteStartElement("AttributeClasses");
                foreach (AttributeClass cl in classesDict.Values) {
                    writer.WriteStartElement("AttClass");
                    writer.WriteAttributeString("name", cl.GetName());
                    SerializeAttributedElement(writer, cl);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement(); // AttributeClasses
                writer.WriteEndDocument();
            }
        }

        public Dictionary<string, AttributeClass> DeserializeAttributeClasses() {
            // Create an XML reader for this file.
            AttributeClass currentClass = null;
            Dictionary<string, AttributeClass> classesDict = new Dictionary<string, AttributeClass>();
            Dictionary<string, Dictionary<string, string>> classAttributes = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, List<string>> classClasses = new Dictionary<string, List<string>>();
            using (XmlReader reader = XmlReader.Create(filename)) {
                while (reader.Read()) {
                    // Only detect start elements.
                    if (reader.NodeType == XmlNodeType.Element) {
                        // Get element name and switch on it.
                        switch (reader.Name) {
                            case "AttClass":
                                string name = reader["name"];
                                if (name == null) throw new System.FormatException("Deserialization failed");
                                AttributeClass cl = new AttributeClass(name);
                                classesDict.Add(name, cl);
                                classAttributes.Add(name, new Dictionary<string, string>());
                                classClasses.Add(name, new List<string>());
                                if (!reader.IsEmptyElement) currentClass = cl;
                                break;
                            case "AttributeClass":
                                string clName = reader["name"];
                                if (clName == null || currentClass == null) throw new System.FormatException("Deserialization failed");
                                classClasses[currentClass.GetName()].Add(clName);
                                break;
                            case "Attribute":
                                string key = reader["key"];
                                string val = reader["value"];
                                if (key == null || val == null || currentClass == null) throw new System.FormatException("Deserialization failed");
                                classAttributes[currentClass.GetName()].Add(key, val);
                                break;
                        }
                    }
                }
            }

            foreach (string name in classesDict.Keys) {
                foreach (string clName in classClasses[name]) {
                    classesDict[name].AddAttributeClass(classesDict[clName]);
                }
                foreach (KeyValuePair<string, string> att in classAttributes[name]) {
                    classesDict[name].SetAttribute(att.Key, att.Value);
                }
            }

            return classesDict;
        }

        public List<string> GetSubDirectories() {
            DirectoryInfo directory = new DirectoryInfo(filename);
            DirectoryInfo[] directories = directory.GetDirectories();
            List<string> dirList = new List<string>();
            foreach (DirectoryInfo folder in directories)
                dirList.Add(folder.Name);
            return dirList;
        }

        public IGrammarEventHandler ParseGrammar(bool clearDictionaries = true) {
            if (clearDictionaries) {
                tempLoaded.Clear();
                tempListeners.Clear();
                tempTaskListeners.Clear();
            }
            string grammarType = null;
            string findFirst = null;
            string name = null;
            bool intergrammar = false;
            bool traverser = false;
            // Create an XML reader for this file.
            using (XmlReader reader = XmlReader.Create(filename)) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element) {
                        if (reader.Name == "Grammar") {
                            grammarType = reader["type"];
                            findFirst = reader["findFirst"];
                            name = reader["name"];
                            if (grammarType == null || name == null) throw new System.FormatException("Deserialization failed");
                            break;
                        } else if (reader.Name == "InterGrammarController") {
                            findFirst = reader["findFirst"];
                            name = reader["name"];
                            grammarType = "task";
                            intergrammar = true;
                            if (name == null) throw new System.FormatException("Deserialization failed");
                            break;
                        } else if (reader.Name == "Traverser") {
                            traverser = true;
                            grammarType = reader["type"];
                            name = reader["name"];
                            if (grammarType == null || name == null) throw new System.FormatException("Deserialization failed");
                            break;
                        }
                    }
                }
                IGrammarEventHandler eventHandler = null;
                if (!traverser) {
                    switch (grammarType.ToLowerInvariant()) {
                        case "graph":
                            Grammar<Graph> graphGrammar = new Grammar<Graph>(name, typeof(GraphTransformer), null, findFirst == "true");
                            _ParseGrammar<Graph>(reader, graphGrammar);
                            eventHandler = graphGrammar;
                            break;
                        case "tilegrid":
                            Grammar<TileGrid> tileGrammar = new Grammar<TileGrid>(name, typeof(TileGridTransformer), null, findFirst == "true");
                            _ParseGrammar<TileGrid>(reader, tileGrammar);
                            eventHandler = tileGrammar;
                            break;
                        case "task":
                            Grammar<Task> taskGrammar;
                            if (intergrammar) {
                                taskGrammar = new InterGrammarController(name, ruleSelectionController: null, findAllRules: findFirst == "true");
                            } else {
                                taskGrammar = new Grammar<Task>(name, typeof(TaskTransformer), null, findFirst == "true");
                            }
                            _ParseGrammar<Task>(reader, taskGrammar);
                            eventHandler = taskGrammar;
                            break;
                    }
                } else {
                    switch (grammarType.ToLowerInvariant()) {
                        case "graph":
                            Traverser<Graph> graphTraverser = new Traverser<Graph>(name, typeof(GraphTransformer));
                            _ParseGrammar<Graph>(reader, graphTraverser);
                            eventHandler = graphTraverser;
                            break;
                        case "tilegrid":
                            Traverser<TileGrid> tileTraverser = new Traverser<TileGrid>(name, typeof(TileGridTransformer));
                            _ParseGrammar<TileGrid>(reader, tileTraverser);
                            eventHandler = tileTraverser;
                            break;
                        case "task":
                            Traverser<Task> taskTraverser = new Traverser<Task>(name, typeof(TaskTransformer));
                            _ParseGrammar<Task>(reader, taskTraverser);
                            eventHandler = taskTraverser;
                            break;
                    }
                }
                if (eventHandler != null) {
                    tempLoaded.Add(eventHandler.Name, eventHandler);
                }
                if (tempListeners.ContainsKey(eventHandler)) {
                    foreach (string listener in tempListeners[eventHandler].Keys) {
                        if (!tempLoaded.ContainsKey(listener)) {
                            Deserialize<IGrammarEventHandler>(new FileInfo(filename).Directory.FullName, listener, controller, clearDictionaries: false);
                        }
                    }
                }
                if (clearDictionaries) {
                    // add listeners
                    foreach (KeyValuePair<IGrammarEventHandler, Dictionary<string, string>> lList in tempListeners) {
                        foreach (KeyValuePair<string, string> listener in lList.Value) {
                            IGrammarEventHandler eh = null;
                            if (tempLoaded.TryGetValue(listener.Key, out eh)) {
                                if (listener.Value != null && listener.Value != "") {
                                    lList.Key.AddListener(eh, name: listener.Value);
                                } else {
                                    lList.Key.AddListener(eh);
                                }
                                //eh.AddListener(lList.Key);
                            } else throw new System.FormatException("Deserialization failed: listener not found");
                        }
                    }
                    foreach (KeyValuePair<Task, Dictionary<string, List<string>>> taskPair in tempTaskListeners) {
                        Task task = taskPair.Key;
                        foreach (KeyValuePair<string, List<string>> listenerPair in taskPair.Value) {
                            string target = listenerPair.Key;
                            foreach (string listener in listenerPair.Value) {
                                IGrammarEventHandler eh = null;
                                if (tempLoaded.TryGetValue(listener, out eh)) {
                                    if (target == "source") {
                                        task.Source = eh;
                                    } else if (target == "target") {
                                        task.AddTarget(eh);
                                    }
                                } else throw new System.FormatException("Deserialization failed: listener not found");
                            }
                        }
                    }
                    // clear them
                    tempLoaded.Clear();
                    tempListeners.Clear();
                    tempTaskListeners.Clear();
                }
                return eventHandler;
            }
        }

        private void _ParseGrammar<T>(XmlReader reader, IGrammarEventHandler eventHandler) where T : StructureModel {
            Constraint<T> currentConstraint = null;
            Rule<T> currentRule = null;
            Grammar<T> grammar = null;
            Traverser<T> traverser = null;
            if (eventHandler == null) return;
            if (typeof(Grammar<T>).IsAssignableFrom(eventHandler.GetType())) {
                grammar = (Grammar<T>)eventHandler;
            } else if (typeof(Traverser<T>).IsAssignableFrom(eventHandler.GetType())) {
                traverser = (Traverser<T>)eventHandler;
            }
            //Stack<GrammarCondition> currentGrammarCondition = new Stack<GrammarCondition>();
            //Stack<RuleCondition> currentRuleCondition = new Stack<RuleCondition>();
            Stack<MethodCaller> currentMethodCaller = new Stack<MethodCaller>();
            while (reader.Read()) {
                string name, activeStr;
                bool active;
                switch (reader.NodeType) {
                    case XmlNodeType.Element:
                        // Get element name and switch on it.
                        switch (reader.Name) {
                            case "RuleSelector":
                                name = reader["name"];
                                GrammarRuleSelector rSel = null;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (name == null) throw new System.FormatException("Deserialization failed");
                                    rSel = (GrammarRuleSelector)StringEvaluator.ParseMethodCaller(name, typeof(GrammarRuleSelector), grammar);
                                } else {
                                    rSel = GrammarRuleSelector.FromName(name, grammar);
                                }
                                if (rSel == null) throw new System.FormatException("Deserialization failed");
                                if (currentConstraint != null) {
                                    currentConstraint.Selector = rSel;
                                } else grammar.RuleSelector = rSel;
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(rSel);
                                break;
                            case "Constraint":
                                name = reader["name"];
                                activeStr = reader["active"];
                                string findFirst = reader["findFirst"];
                                active = true;
                                if (activeStr != null) {
                                    if (!bool.TryParse(activeStr, out active)) {
                                        active = true;
                                    }
                                }
                                if (name == null) throw new System.FormatException("Deserialization failed");
                                Constraint<T> constraint = new Constraint<T>(grammar, name, active);
                                if (findFirst == "true") constraint.FindFirst = true;
                                currentConstraint = constraint;
                                break;
                            case "GrammarCondition":
                                name = reader["name"];
                                GrammarCondition grCond = null;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (name == null) throw new System.FormatException("Deserialization failed");
                                    grCond = (GrammarCondition)StringEvaluator.ParseMethodCaller(name, typeof(GrammarCondition), grammar);
                                } else {
                                    grCond = GrammarCondition.FromName(name, grammar);
                                }
                                if (grCond == null) throw new System.FormatException("Deserialization failed");
                                if (currentMethodCaller.Count > 0) {
                                    // ARGUMENT
                                    currentMethodCaller.Peek().AddArgument(grCond);
                                } else if (currentConstraint != null) {
                                    // CONSTRAINT CONDITION
                                    currentConstraint.Condition = grCond;
                                } else {
                                    // STOP CONDITION
                                    grammar.AddStopCondition(grCond);
                                }
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(grCond);
                                break;
                            case "GrammarProbability":
                                name = reader["name"];
                                GrammarProbability grProb;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (name == null) throw new System.FormatException("Deserialization failed");
                                    grProb = (GrammarProbability)StringEvaluator.ParseMethodCaller(name, typeof(GrammarProbability), grammar);
                                } else {
                                    grProb = GrammarProbability.FromName(name, grammar);
                                }
                                if (grProb == null) throw new System.FormatException("Deserialization failed");
                                if (currentMethodCaller.Count > 0) {
                                    // ARGUMENT
                                    currentMethodCaller.Peek().AddArgument(grProb);
                                } else if (currentConstraint != null) {
                                    // CONSTRAINT CONDITION
                                    currentConstraint.ProbabilityCalculator = grProb;
                                }
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(grProb);
                                break;
                            case "Initial":
                            case "Rule":
                                string probabilityStr = reader["probability"];
                                activeStr = reader["active"];
                                name = reader["name"];
                                while (name == null) {
                                    Random rand = new Random();
                                    name = rand.Next(int.MaxValue).ToString();
                                    Rule<T> existingRule = null;
                                    if (currentConstraint != null) {
                                        existingRule = currentConstraint.GetRule(name);
                                    } else {
                                        existingRule = grammar.GetRule(name);
                                    }
                                    if (existingRule != null) name = null;
                                }
                                active = true;
                                int priority = 0;
                                string priorityStr = reader["priority"];
                                if (priorityStr != null) {
                                    int.TryParse(priorityStr, out priority);
                                }
                                if (activeStr != null) {
                                    if (!bool.TryParse(activeStr, out active)) {
                                        active = true;
                                    }
                                }
                                double probability;
                                if (probabilityStr == null || !double.TryParse(probabilityStr, out probability)) {
                                    probability = 1;
                                }
                                if (name == null) throw new System.FormatException("Deserialization failed");
                                currentRule = new Rule<T>(grammar, name, probability, priority:priority, active:active);
                                if (currentConstraint != null) {
                                    currentConstraint.AddRule(currentRule);
                                } else {
                                    grammar.AddRule(currentRule);
                                }
                                break;
                            case "RuleProbability":
                                name = reader["name"];
                                RuleProbability rProb;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (currentRule == null || name == null) throw new System.FormatException("Deserialization failed");
                                    rProb = (RuleProbability)StringEvaluator.ParseMethodCaller(name, typeof(RuleProbability), grammar, currentRule);
                                } else {
                                    if (currentRule == null) throw new System.FormatException("Deserialization failed");
                                    rProb = RuleProbability.FromName(name, currentRule);
                                }
                                if (rProb == null) throw new System.FormatException("Deserialization failed");
                                if (currentMethodCaller.Count == 0) {
                                    currentRule.DynamicProbability = rProb;
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rProb);
                                }
                                if(!reader.IsEmptyElement) currentMethodCaller.Push(rProb);
                                break;
                            case "RuleMatchSelector":
                                name = reader["name"];
                                RuleMatchSelector rMatchSel;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (currentRule == null || name == null) throw new System.FormatException("Deserialization failed");
                                    rMatchSel = (RuleMatchSelector)StringEvaluator.ParseMethodCaller(name, typeof(RuleMatchSelector), grammar, currentRule);
                                } else {
                                    if (currentRule == null) throw new System.FormatException("Deserialization failed");
                                    rMatchSel = RuleMatchSelector.FromName(name, currentRule);
                                }
                                if(rMatchSel == null) throw new System.FormatException("Deserialization failed");
                                if (currentMethodCaller.Count == 0) {
                                    currentRule.MatchSelector = rMatchSel;
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rMatchSel);
                                }
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(rMatchSel);
                                break;
                            case "RuleCondition":
                                name = reader["name"];
                                RuleCondition rCond;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (currentRule == null || name == null) throw new System.FormatException("Deserialization failed");
                                    rCond = (RuleCondition)StringEvaluator.ParseMethodCaller(name, typeof(RuleCondition), grammar, currentRule);
                                } else {
                                    if (currentRule == null) throw new System.FormatException("Deserialization failed");
                                    rCond = RuleCondition.FromName(name, currentRule);
                                }
                                if (rCond == null) throw new System.FormatException("Deserialization failed");
                                if (currentMethodCaller.Count == 0) {
                                    if (currentRule != null) currentRule.Condition = rCond;
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rCond);
                                }
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(rCond);
                                break;
                            case "RuleAction":
                                name = reader["name"];
                                RuleAction rAct;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (currentRule == null || name == null) throw new System.FormatException("Deserialization failed");
                                    rAct = (RuleAction)StringEvaluator.ParseMethodCaller(name, typeof(RuleAction), grammar, currentRule);
                                } else {
                                    if (currentRule == null) throw new System.FormatException("Deserialization failed");
                                    rAct = RuleAction.FromName(name, currentRule);
                                }
                                if (rAct == null) throw new System.FormatException("Deserialization failed");
                                if (currentMethodCaller.Count == 0) {
                                    if (currentRule != null) currentRule.AddAction(rAct);
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rAct);
                                }
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(rAct);
                                break;
                            case "TaskProcessor":
                                string ev = reader["event"];
                                name = reader["name"];
                                TaskProcessor tProc;
                                if (name == null) {
                                    reader.Read();
                                    name = reader.Value;
                                    if (name == null) throw new System.FormatException("Deserialization failed");
                                    tProc = (TaskProcessor)StringEvaluator.ParseMethodCaller(name, typeof(TaskProcessor), grammar, container: eventHandler);
                                } else {
                                    //if (currentRule == null) throw new System.FormatException("Deserialization failed");
                                    tProc = TaskProcessor.FromName(name, container: eventHandler);
                                }
                                if (tProc == null) throw new System.FormatException("Deserialization failed");
                                if (ev == null) {
                                    ev = tProc.Method.Name;
                                }
                                if (currentMethodCaller.Count == 0) {
                                    if (grammar != null) {
                                        grammar.AddTaskProcessor(ev, tProc);
                                    } else if (traverser != null) {
                                        traverser.AddTaskProcessor(ev, tProc);
                                    }
                                } else {
                                    currentMethodCaller.Peek().AddArgument(tProc);
                                }
                                if (!reader.IsEmptyElement) currentMethodCaller.Push(tProc);
                                break;
                            case "Query":
                                if (traverser != null) {
                                    string qName = reader["name"];
                                    reader.Read();
                                    name = reader.Value;
                                    if (name == null || qName == null) throw new System.FormatException("Deserialization failed");
                                    T query = Deserialize<T>(new FileInfo(filename).Directory.FullName, name, controller, clearDictionaries: false);
                                    traverser.AddQuery(qName, query);
                                } else {
                                    reader.Read();
                                    name = reader.Value;
                                    if (name == null || currentRule == null) throw new System.FormatException("Deserialization failed");
                                    T query = Deserialize<T>(new FileInfo(filename).Directory.FullName, name, controller, clearDictionaries: false);
                                    currentRule.Query = query;
                                }
                                break;
                            case "Target":
                                reader.Read();
                                name = reader.Value;
                                if (name == null || currentRule == null) throw new System.FormatException("Deserialization failed");
                                T target = Deserialize<T>(new FileInfo(filename).Directory.FullName, name, controller, clearDictionaries: false);
                                currentRule.Target = target;
                                break;
                            case "string":
                                reader.Read();
                                name = reader.Value;
                                if (name == null) name = "";
                                if (currentMethodCaller.Count > 0) {
                                    currentMethodCaller.Peek().AddArgument(name);
                                }
                                break;
                            case "int":
                                reader.Read();
                                name = reader.Value;
                                if (name == null) throw new System.FormatException("Deserialization failed");
                                int intResult;
                                if (int.TryParse(name, out intResult)) {
                                    if (currentMethodCaller.Count > 0) {
                                        currentMethodCaller.Peek().AddArgument(intResult);
                                    }
                                }
                                break;
                            case "double":
                                reader.Read();
                                name = reader.Value;
                                if (name == null) throw new System.FormatException("Deserialization failed");
                                double doubleResult;
                                if (double.TryParse(name, out doubleResult)) {
                                    if (currentMethodCaller.Count > 0) {
                                        currentMethodCaller.Peek().AddArgument(doubleResult);
                                    }
                                }
                                break;
                            case "Listener":
                                name = reader["name"];
                                string alias = reader["alias"];
                                if (name == null && !reader.IsEmptyElement) {
                                    reader.Read();
                                    name = reader.Value;
                                }
                                if(name == null) throw new System.FormatException("Deserialization failed");
                                if (!tempListeners.ContainsKey(eventHandler)) {
                                    tempListeners.Add(eventHandler, new Dictionary<string, string>());
                                }
                                if (alias == null || alias.Trim() == "") {
                                    tempListeners[eventHandler].Add(name, "");
                                } else {
                                    tempListeners[eventHandler].Add(name, alias);
                                }
                                break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name) {
                            case "GrammarCondition":
                            case "RuleSelector":
                            case "RuleCondition":
                            case "RuleProbability":
                            case "RuleMatchSelector":
                            case "RuleAction":
                            case "TaskProcessor":
                                currentMethodCaller.Pop();
                                break;
                            case "Constraint":
                                currentConstraint = null;
                                break;
                            case "Initial":
                            case "Rule":
                                currentRule = null;
                                break;
                        }
                        break;
                }
            }
        }

        public static T Deserialize<T>(string dirname, string name, DemoController controller, bool clearDictionaries = true) {
            if (name == null || name.Trim() == "") return default(T);
            string filename = name;
            if (typeof(T) == typeof(Graph)) {
                filename = dirname + "/Graph/" + filename + ".xml";
                DemoIO serializer = new DemoIO(filename, controller, clearDictionaries: clearDictionaries);
                Graph graph = serializer.DeserializeGraph();
                return (T)(object)graph;
            } else if (typeof(T) == typeof(TileGrid)) {
                filename = dirname + "/TileGrid/" + filename + ".xml";
                DemoIO serializer = new DemoIO(filename, controller, clearDictionaries: clearDictionaries);
                TileGrid grid = serializer.DeserializeGrid();
                return (T)(object)grid;
            } else if (typeof(T) == typeof(Task)) {
                filename = dirname + "/Task/" + filename + ".xml";
                DemoIO serializer = new DemoIO(filename, controller, clearDictionaries: clearDictionaries);
                Task task = serializer.DeserializeTask();
                return (T)(object)task;
            } else if (typeof(T) == typeof(IGrammarEventHandler)) {
                DirectoryInfo dirInfo = new DirectoryInfo(dirname);
                FileInfo[] files = dirInfo.GetFiles();
                foreach (FileInfo file in files) {
                    if (file.Name == filename + ".xml") {
                        filename = dirInfo.FullName + "/" + filename + ".xml";
                        DemoIO serializer = new DemoIO(filename, controller, clearDictionaries: clearDictionaries);
                        IGrammarEventHandler eh = serializer.ParseGrammar(clearDictionaries: clearDictionaries);
                        return (T)(object)eh;
                    }
                }
                // Not returned yet? Check subdirs
                DirectoryInfo[] subdirs = dirInfo.GetDirectories();
                foreach (DirectoryInfo subdir in subdirs) {
                    if (subdir.Name == filename) {
                        filename = subdir.FullName + "/" + filename + ".xml";
                        DemoIO serializer = new DemoIO(filename, controller, clearDictionaries: clearDictionaries);
                        IGrammarEventHandler eh = serializer.ParseGrammar(clearDictionaries: clearDictionaries);
                        return (T)(object)eh;
                    }
                }
                // Then check parent subdirs.
                DirectoryInfo[] psubdirs = dirInfo.Parent.GetDirectories();
                foreach (DirectoryInfo subdir in psubdirs) {
                    if (subdir.Name == filename) {
                        filename = subdir.FullName + "/" + filename + ".xml";
                        DemoIO serializer = new DemoIO(filename, controller, clearDictionaries: clearDictionaries);
                        IGrammarEventHandler eh = serializer.ParseGrammar(clearDictionaries: clearDictionaries);
                        return (T)(object)eh;
                    }
                }
            }
            return default(T);
        }
    }
}
