using Grammars.Events;
using Grammars.Graphs;
using Grammars.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class TaskProcessor : MethodCaller {
        private IGrammarEventHandler container;
        public IGrammarEventHandler Container {
            get { return container; }
            set { container = value; }
        }

        public TaskProcessor(MethodInfo method, IGrammarEventHandler container = null) : base(method) {
            this.container = container;
        }

        public void Process(Task t) {
            // Check method signature
            int argCount = arguments.Count;
            if (container != null && method != null && method.ReturnType == typeof(void) && method.GetParameters().Count() == 2 + argCount) {
                object[] parameters = new object[2 + argCount];
                parameters[0] = container;
                parameters[1] = t;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 2] = arguments[i];
                }
                method.Invoke(null, parameters);
            }
        }

        public static TaskProcessor FromName(string name, IGrammarEventHandler container) {
            MethodInfo method = typeof(TaskProcessor).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(void) && method.GetParameters().Count() >= 2) {
                return new TaskProcessor(method, container);
            } else return null;
        }

        // Example task processor methods are below
        public static void TileTraverser_NextElement(IGrammarEventHandler container, Task task, string myName) {
            if (task == null) return;
            AttributedElement currentElement = null;
            Traverser<TileGrid> traverser = (Traverser<TileGrid>)container;
            TileGrid source = traverser.Source;
            string traversedName = traverser.Source.LinkType;

            AttributedElement startEl = null;
            if (task.HasObjectAttribute("start")) {
                startEl = (AttributedElement)task.GetObjectAttribute("start");
            } else if (task.HasAttribute("start")) {
                startEl = source.GetElement(task.GetAttribute("start"));
            }

            if (source != null) {
                if (startEl != null) {
                    if (startEl.Container == traverser.Source) {
                        traverser.CurrentElement = startEl;
                    } else if (startEl.HasLink(traversedName)) {
                        List<AttributedElement> possibleStarts = startEl.GetLinkedElements(traversedName);
                        Random rand = new Random();
                        traverser.CurrentElement = possibleStarts[rand.Next(0, possibleStarts.Count)];
                    }
                }
                currentElement = traverser.CurrentElement;
                if (currentElement == null) {
                    traverser.SetFirstElement();
                    currentElement = traverser.CurrentElement;
                }
                if (currentElement != null) {
                    if (currentElement.HasAttribute("placeholder")) {
                        traverser.GenerateMore(placeholderEl: currentElement);
                        //currentElement = traverser.CurrentElement;
                        GraphTraverser_NextEdge(container, task, myName);
                        return;
                    }
                    if (currentElement == null || currentElement.HasAttribute("placeholder")) {
                        TileTraverser_NextElement(container, task, myName);
                        return;
                    } else {
                        // Go one step further
                        // We assume current element is a tile
                        Tile currentTile = (Tile)currentElement;
                        IDictionary<string, Tile> neighbors = currentTile.GetNeighbors();
                        List<AttributedElement> targets = new List<AttributedElement>();
                        foreach (KeyValuePair<string, Tile> pair in neighbors) {
                            if (task.HasAttribute("neighborSelector")) {
                                string sel = task.GetAttribute("neighborSelector");
                                if (sel != null && !sel.Contains(pair.Key)) continue;
                            }
                            if (!pair.Value.HasLink(myName) || pair.Value.HasAttribute("placeholder")) targets.Add(pair.Value);
                        }
                        if (task.HasAttribute("tileSelector")) {
                            List<AttributedElement> nextEls = StringEvaluator.SelectElementsFromList(targets, task["tileSelector"]);
                            if (nextEls.Count > 0) targets = nextEls;
                        }
                        if (targets.Count == 0) {
                            traverser.GenerateMore();
                            TileTraverser_NextElement(container, task, myName);
                            return;
                        } else {
                            Random rand = new Random();
                            currentElement = targets[rand.Next(0, targets.Count)];
                            traverser.CurrentElement = currentElement;
                            if (currentElement == null || currentElement.HasAttribute("placeholder")) {
                                TileTraverser_NextElement(container, task, myName);
                                return;
                            }
                        }
                    }
                }
            }
            task.AddReply(currentElement);
        }

        public static void GraphTraverser_NextNode(IGrammarEventHandler container, Task task, string myName) {
            if (task == null) return;
            AttributedElement currentElement = null;
            Traverser<Graph> traverser = (Traverser<Graph>)container;
            Graph source = traverser.Source;
            string traversedName = traverser.Source.LinkType;

            AttributedElement startEl = null;
            if (task.HasObjectAttribute("start")) {
                startEl = (AttributedElement)task.GetObjectAttribute("start");
            } else if (task.HasAttribute("start")) {
                startEl = source.GetElement(task.GetAttribute("start"));
            }

            if (source != null) {
                if (startEl != null) {
                    if (startEl.Container == traverser.Source) {
                        traverser.CurrentElement = startEl;
                    } else if (startEl.HasLink(traversedName)) {
                        List<AttributedElement> possibleStarts = startEl.GetLinkedElements(traversedName);
                        Random rand = new Random();
                        traverser.CurrentElement = possibleStarts[rand.Next(0, possibleStarts.Count)];
                    }
                }
                currentElement = traverser.CurrentElement;
                if (currentElement == null || currentElement.HasAttribute("_grammar_destroyed")) {
                    traverser.CurrentElement = null;
                    traverser.SetFirstElement();
                    currentElement = traverser.CurrentElement;
                }
                if (currentElement != null) {
                    // If current element is placeholder, ask origin to generate more
                    if (currentElement.HasAttribute("placeholder")) {
                        traverser.GenerateMore(placeholderEl: currentElement);
                        //currentElement = traverser.CurrentElement;
                        GraphTraverser_NextEdge(container, task, myName);
                        return;
                    }
                    // If it is still placeholder or actually deleted this time, execute this algorithm again.
                    if (currentElement == null || currentElement.HasAttribute("placeholder")) {
                        GraphTraverser_NextNode(container, task, myName);
                        return;
                    }
                    // If current element is yet unlinked, then return this.
                    if (!currentElement.HasLink(myName)) {
                        task.AddReply(currentElement);
                        return;
                    }
                    // Go one step further
                    // We assume current element is a node
                    Node currentNode = (Node)currentElement;
                    IDictionary<Node, Edge> edges = currentNode.GetEdges();
                    List<AttributedElement> targets = new List<AttributedElement>();
                    foreach (KeyValuePair<Node, Edge> pair in edges) {
                        if ((pair.Value.IsDirected() && currentElement == pair.Value.GetNode1()) || !pair.Value.IsDirected()) {
                            if (task.HasAttribute("edgeSelector")) {
                                List<AttributedElement> edgeEls = new List<AttributedElement>();
                                edgeEls.Add(pair.Value);
                                edgeEls = StringEvaluator.SelectElementsFromList(edgeEls, task["edgeSelector"]);
                                if (edgeEls.Count == 0) continue;
                            }
                            if (!pair.Key.HasLink(myName) || pair.Key.HasAttribute("placeholder")) targets.Add(pair.Key);
                        }
                    }
                    if (task.HasAttribute("nodeSelector")) {
                        List<AttributedElement> nextEls = StringEvaluator.SelectElementsFromList(targets, task["nodeSelector"]);
                        if (nextEls.Count > 0) targets = nextEls;
                    }
                    if (targets.Count == 0) {
                        //traverser.GenerateMore();
                        // No targets? Then the next element should be any unlinked element accessible from a linked one
                        List<Node> possibleStarts = source.GetNodes().Where(n => n.HasLink(myName)).ToList();
                        foreach (Node node in possibleStarts) {
                            IDictionary<Node, Edge> nodeEdges = node.GetEdges();
                            foreach (KeyValuePair<Node, Edge> pair in nodeEdges) {
                                if ((pair.Value.IsDirected() && node == pair.Value.GetNode1()) || !pair.Value.IsDirected()) {
                                    if (!pair.Key.HasLink(myName) || pair.Key.HasAttribute("placeholder")) {
                                        targets.Add(pair.Key);
                                    }
                                }
                            }
                        }
                        //GraphTraverser_NextElement(container, task, myName);
                        //return;
                    }
                    if (targets.Count > 0) {
                        Random rand = new Random();
                        currentElement = targets[rand.Next(0, targets.Count)];
                        traverser.CurrentElement = currentElement;
                        if (currentElement == null || currentElement.HasAttribute("placeholder")) {
                            GraphTraverser_NextNode(container, task, myName);
                            return;
                        }
                    }
                }
            }
            task.AddReply(currentElement);
        }

        public static void GraphTraverser_NextEdge(IGrammarEventHandler container, Task task, string myName) {
            if (task == null) return;
            AttributedElement currentElement = null;
            Traverser<Graph> traverser = (Traverser<Graph>)container;
            Graph source = traverser.Source;
            string traversedName = traverser.Source.LinkType;

            AttributedElement startEl = null;
            if (task.HasObjectAttribute("start")) {
                startEl = (AttributedElement)task.GetObjectAttribute("start");
            } else if (task.HasAttribute("start")) {
                startEl = source.GetElement(task.GetAttribute("start"));
            }

            if (source != null) {
                if (startEl != null) {
                    if (startEl.Container == traverser.Source && startEl.GetType() == typeof(Edge)) {
                        currentElement = startEl;
                    } else if (startEl.Container == traverser.Source && startEl.GetType() == typeof(Node)) {
                        currentElement = startEl;
                    } else if (startEl.HasLink(traversedName)) {
                        List<AttributedElement> possibleStarts = startEl.GetLinkedElements(traversedName);
                        Random rand = new Random();
                        currentElement = possibleStarts[rand.Next(0, possibleStarts.Count)];
                    }
                }
                if (currentElement != null) {
                    traverser.CurrentElement = currentElement;
                } else {
                    currentElement = traverser.CurrentElement;
                }
                if (currentElement != null && currentElement.HasAttribute("_grammar_destroyed")) {
                    GraphTraverser_RandomNextEdge(container, task, myName);
                    return;
                }
                if (currentElement == null) {
                    traverser.SetFirstElement();
                    currentElement = traverser.CurrentElement;
                }
                if (currentElement != null) {
                    if (currentElement.GetType() == typeof(Node)) {
                        Node currentNode = ((Node)currentElement);
                        IDictionary<Node, Edge> currentEdges = currentNode.GetEdges();
                        foreach (KeyValuePair<Node, Edge> edge in currentEdges) {
                            if ((edge.Value.IsDirected() && currentNode == edge.Value.GetNode1()) || !edge.Value.IsDirected()) {
                                if (!edge.Value.HasLink(myName) || edge.Value.HasAttribute("placeholder")
                                    || edge.Key.HasAttribute("placeholder") || !edge.Key.HasLink(myName)) {
                                    currentElement = edge.Value;
                                    traverser.CurrentElement = currentElement;
                                    break;
                                }
                            }
                        }
                        // No better edge found? Take a random one!
                        if (currentElement.GetType() == typeof(Node)) {
                            GraphTraverser_RandomNextEdge(container, task, myName);
                            return;
                        }
                    }
                    Edge currentEdge;
                    Node node1;
                    Node node2;
                    List<Edge> targets = new List<Edge>();

                    if (currentElement.GetType() == typeof(Edge)) {
                        currentEdge = (Edge)currentElement;
                        node1 = currentEdge.GetNode1();
                        node2 = currentEdge.GetNode2();

                        // If current element is placeholder, ask origin to generate more
                        if (currentElement.HasAttribute("placeholder") || node1.HasAttribute("placeholder") || node2.HasAttribute("placeholder")) {
                            if (currentElement.HasAttribute("placeholder")) {
                                traverser.GenerateMore(placeholderEl: currentElement);
                            } else if (node1.HasAttribute("placeholder")) {
                                traverser.GenerateMore(placeholderEl: node1);
                            } else if (node2.HasAttribute("placeholder")) {
                                traverser.GenerateMore(placeholderEl: node2);
                            }
                            //currentElement = traverser.CurrentElement;
                            GraphTraverser_NextEdge(container, task, myName);
                            return;
                        }
                        // If it is still placeholder or actually deleted this time, execute this algorithm again.
                        if (currentElement == null || currentElement.GetType() != typeof(Edge) || currentElement.HasAttribute("placeholder")) {
                            GraphTraverser_NextEdge(container, task, myName);
                            return;
                        }
                        // If current element is yet unlinked, then return this.
                        if (!currentElement.HasLink(myName)) {
                            traverser.CurrentElement = currentElement;
                            task.AddReply(currentElement);
                            return;
                        }

                        currentEdge = (Edge)currentElement;
                        node1 = currentEdge.GetNode1();
                        node2 = currentEdge.GetNode2();
                        List<Node> nodeTargets = new List<Node>();
                        nodeTargets.Add(node2);
                        if (!currentEdge.IsDirected()) {
                            nodeTargets.Add(node1);
                        }
                        foreach (Node node in nodeTargets) {
                            IDictionary<Node, Edge> edges = node.GetEdges();
                            foreach (KeyValuePair<Node, Edge> pair in edges) {
                                if ((pair.Value.IsDirected() && node == pair.Value.GetNode1()) || !pair.Value.IsDirected()) {
                                    if (!pair.Value.HasLink(myName) || !pair.Key.HasLink(myName) || pair.Value.HasAttribute("placeholder") || pair.Key.HasAttribute("placeholder")) {
                                        if (task.HasAttribute("edgeSelector")) {
                                            List<AttributedElement> edgeEls = new List<AttributedElement>();
                                            edgeEls.Add(pair.Value);
                                            edgeEls = StringEvaluator.SelectElementsFromList(edgeEls, task["edgeSelector"]);
                                            if (edgeEls.Count == 0) continue;
                                        }
                                        if (task.HasAttribute("nodeSelector")) {
                                            List<AttributedElement> nodeEls = new List<AttributedElement>();
                                            nodeEls.Add(pair.Key);
                                            nodeEls = StringEvaluator.SelectElementsFromList(nodeEls, task["nodeSelector"]);
                                            if (nodeEls.Count == 0) continue;
                                        }
                                        targets.Add(pair.Value);
                                    }
                                }
                            }
                        }
                    }
                    if (targets.Count == 0) {
                        // If not already returned, then no adjacent edges were found that weren't linked.
                        // Next strategy is: choose edge at random (but make sure node has been linked).
                        GraphTraverser_RandomNextEdge(container, task, myName);
                        return;
                    }
                    if (targets.Count > 0) {
                        Random rand = new Random();
                        currentEdge = targets[rand.Next(0, targets.Count)];
                        currentElement = currentEdge;
                        node1 = currentEdge.GetNode1();
                        node2 = currentEdge.GetNode2();
                        traverser.CurrentElement = currentElement;
                        if (currentEdge == null || currentEdge.HasAttribute("placeholder")
                            || node1.HasAttribute("placeholder") || node2.HasAttribute("placeholder")) {
                            GraphTraverser_NextEdge(container, task, myName);
                            return;
                        }
                        task.AddReply(currentElement);
                        return;
                    }
                }
            }
            task.AddReply(currentElement);
        }

        public static void GraphTraverser_RandomNextEdge(IGrammarEventHandler container, Task task, string myName) {
            if (task == null) return;
            AttributedElement currentElement = null;
            Traverser<Graph> traverser = (Traverser<Graph>)container;
            Graph source = traverser.Source;
            //string traversedName = traverser.Source.LinkType;

            currentElement = null;
            traverser.CurrentElement = null;
            // Take a random edge where node1 = linked and node2 isn't
            List<Node> possibleStarts = source.GetNodes().Where(n => n.HasLink(myName)).ToList();
            List<Edge> targets = new List<Edge>();
            foreach (Node node in possibleStarts) {
                IDictionary<Node, Edge> nodeEdges = node.GetEdges();
                foreach (KeyValuePair<Node, Edge> pair in nodeEdges) {
                    if ((pair.Value.IsDirected() && node == pair.Value.GetNode1()) || !pair.Value.IsDirected()) {
                        if (!pair.Value.HasLink(myName) || !pair.Key.HasLink(myName) || pair.Key.HasAttribute("placeholder")) {
                            targets.Add(pair.Value);
                        }
                    }
                }
            }
            if (targets.Count > 0) {
                Random rand = new Random();
                Edge currentEdge = targets[rand.Next(0, targets.Count)];
                currentElement = currentEdge;
                Node node1 = currentEdge.GetNode1();
                Node node2 = currentEdge.GetNode2();
                traverser.CurrentElement = currentElement;
                if (currentEdge == null || currentEdge.HasAttribute("placeholder")
                    || node1.HasAttribute("placeholder") || node2.HasAttribute("placeholder")) {
                    GraphTraverser_NextEdge(container, task, myName);
                    return;
                }
                task.AddReply(currentElement);
                return;
            } else {
                traverser.CurrentElement = null;
                currentElement = null;
                task.AddReply(currentElement);
                return;
            }
        }
    }
}
