using Grammars;
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

        public string[] ReadLines() {
            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
            return lines;
        }
    }
}
