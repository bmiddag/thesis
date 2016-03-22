﻿using Grammars;
using Grammars.Graph;
using Grammars.Tile;
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

        public DemoIO(string filename, DemoController controller) {
            this.filename = filename;
            this.controller = controller;
            settings = new XmlWriterSettings {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
        }

        void SerializeAttributedElement(XmlWriter writer, AttributedElement el) {
            // Save attribute classes
            if (el.GetAttributeClasses().Count > 0) {
                writer.WriteStartElement("AttributeClasses");
                foreach (AttributeClass cl in el.GetAttributeClasses()) {
                    writer.WriteStartElement("AttributeClass");
                    writer.WriteAttributeString("name", cl.GetName());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            // Save attributes
            if (el.GetAttributes().Count > 0) {
                writer.WriteStartElement("Attributes");
                foreach (KeyValuePair<string, string> att in el.GetAttributes()) {
                    writer.WriteStartElement("Attribute");
                    writer.WriteAttributeString("key", att.Key);
                    writer.WriteAttributeString("value", att.Value);
                    writer.WriteEndElement();
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
            AttributedElement currentElement = null;
            TileGrid grid = null;
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
            AttributedElement currentElement = null;
            Graph graph = new Graph();
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
                                if (name == null) return null; // Deserialization failed
                                AttributeClass cl = new AttributeClass(name);
                                classesDict.Add(name, cl);
                                classAttributes.Add(name, new Dictionary<string, string>());
                                classClasses.Add(name, new List<string>());
                                if (!reader.IsEmptyElement) currentClass = cl;
                                break;
                            case "AttributeClass":
                                string clName = reader["name"];
                                if (clName == null || currentClass == null) return null; // Deserialization failed
                                classClasses[currentClass.GetName()].Add(clName);
                                break;
                            case "Attribute":
                                string key = reader["key"];
                                string val = reader["value"];
                                if (key == null || val == null || currentClass == null) return null; // Deserialization failed
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

        public void ParseGrammar() {
            string grammarType = null;
            string findFirst = null;
            // Create an XML reader for this file.
            using (XmlReader reader = XmlReader.Create(filename)) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element) {
                        if (reader.Name == "Grammar") {
                            grammarType = reader["type"];
                            findFirst = reader["findFirst"];
                            if (grammarType == null) return; // Deserialization failed
                        }
                    }
                }
                switch (grammarType.ToLowerInvariant()) {
                    case "graph":
                        Grammar<Graph> graphGrammar = new Grammar<Graph>(null, findFirst=="true");
                        _ParseGrammar(reader, graphGrammar);
                        controller.SetGrammar(graphGrammar);
                        break;
                    case "tilegrid":
                        Grammar<TileGrid> tileGrammar = new Grammar<TileGrid>(null, findFirst=="true");
                        _ParseGrammar(reader, tileGrammar);
                        controller.SetGrammar(tileGrammar);
                        break;
                }
            }
        }

        private void _ParseGrammar<T>(XmlReader reader, Grammar<T> grammar) where T : StructureModel {
            Constraint<T> currentConstraint = null;
            Rule<T> currentRule = null;
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
                                if (name == null) return; // Deserialization failed
                                GrammarRuleSelector rSel = GrammarRuleSelector.FromName(name, grammar);
                                if (rSel == null) return; // Deserialization failed
                                currentMethodCaller.Push(rSel);
                                break;
                            case "Constraint":
                                name = reader["name"];
                                activeStr = reader["active"];
                                active = true;
                                if (activeStr != null) {
                                    if (!bool.TryParse(activeStr, out active)) {
                                        active = true;
                                    }
                                }
                                Constraint<T> constraint = new Constraint<T>(grammar, active);
                                currentConstraint = constraint;
                                break;
                            case "GrammarCondition":
                                name = reader["name"];
                                if (name == null) return; // Deserialization failed
                                GrammarCondition grCond = GrammarCondition.FromName(name, grammar);
                                if (grCond == null) return; // Deserialization failed
                                if (currentMethodCaller.Count > 0) {
                                    // ARGUMENT
                                    currentMethodCaller.Peek().AddArgument(grCond);
                                } else if (currentConstraint != null) {
                                    // CONSTRAINT CONDITION
                                    currentConstraint.AddCondition(grCond);
                                } else {
                                    // STOP CONDITION
                                    grammar.AddStopCondition(grCond);
                                }
                                currentMethodCaller.Push(grCond);
                                break;
                            case "Rule":
                                string probabilityStr = reader["probability"];
                                activeStr = reader["active"];
                                active = true;
                                if (activeStr != null) {
                                    if (!bool.TryParse(activeStr, out active)) {
                                        active = true;
                                    }
                                }
                                double probability;
                                if (probabilityStr == null || !double.TryParse(probabilityStr, out probability)) return; // Deserialization failed
                                currentRule = new Rule<T>(probability, active);
                                if (currentConstraint != null) {
                                    currentConstraint.AddRule(currentRule);
                                } else {
                                    grammar.AddRule(currentRule);
                                }
                                break;
                            case "RuleProbability":
                                reader.Read();
                                name = reader.Value;
                                if (currentRule == null || name == null) return; // Deserialization failed
                                RuleProbability rProb = RuleProbability.FromName(name, currentRule);
                                if (rProb == null) return; // Deserialization failed
                                if (currentMethodCaller.Count == 0) {
                                    currentRule.DynamicProbability = rProb;
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rProb);
                                }
                                currentMethodCaller.Push(rProb);
                                break;
                            case "RuleMatchSelector":
                                reader.Read();
                                name = reader.Value;
                                if (currentRule == null || name == null) return; // Deserialization failed
                                RuleMatchSelector rMatchSel = RuleMatchSelector.FromName(name, currentRule);
                                if (currentMethodCaller.Count == 0) {
                                    currentRule.MatchSelector = rMatchSel;
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rMatchSel);
                                }
                                currentMethodCaller.Push(rMatchSel);
                                break;
                            case "RuleCondition":
                                name = reader["name"];
                                if (name == null) return; // Deserialization failed
                                if (currentRule == null) return; // Deserialization failed
                                RuleCondition rCond = RuleCondition.FromName(name, currentRule);
                                if (currentMethodCaller.Count == 0) {
                                    if (currentRule != null) currentRule.Condition = rCond;
                                } else {
                                    currentMethodCaller.Peek().AddArgument(rCond);
                                }
                                if (rCond == null) return; // Deserialization failed
                                currentMethodCaller.Push(rCond);
                                break;
                            case "Query":
                                reader.Read();
                                name = reader.Value;
                                if (name == null || currentRule == null) return; // Deserialization failed
                                T query = Deserialize<T>(name, controller);
                                currentRule.Query = query;
                                break;
                            case "Target":
                                reader.Read();
                                name = reader.Value;
                                if (name == null || currentRule == null) return; // Deserialization failed
                                T target = Deserialize<T>(name, controller);
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
                                if (name == null) return; // Deserialization failed
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
                                if (name == null) return; // Deserialization failed
                                double doubleResult;
                                if (double.TryParse(name, out doubleResult)) {
                                    if (currentMethodCaller.Count > 0) {
                                        currentMethodCaller.Peek().AddArgument(doubleResult);
                                    }
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
                                currentMethodCaller.Pop();
                                break;
                            case "Constraint":
                                currentConstraint = null;
                                break;
                            case "Rule":
                                currentRule = null;
                                break;
                        }
                        break;
                }
            }
        }

        public string[] ReadLines() {
            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
            return lines;
        }

        public static T Deserialize<T>(string name, DemoController controller) where T : StructureModel {
            if (name == null || name.Trim() == "") return null;
            string filename = name;
            if (typeof(T) == typeof(Graph)) {
                filename = "Graph/" + filename + ".xml";
                DemoIO serializer = new DemoIO(filename, controller);
                TileGrid grid = serializer.DeserializeGrid();
                return (T)(object)grid;
            } else if (typeof(T) == typeof(TileGrid)) {
                filename = "TileGrid/" + filename + ".xml";
                DemoIO serializer = new DemoIO(filename, controller);
                Graph graph = serializer.DeserializeGraph();
                return (T)(object)graph;
            }
            return null;
    }
}
