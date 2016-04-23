using System;
using System.Collections.Generic;
using System.Linq;

namespace Grammars.Graphs {
	public class GraphTransformer : IStructureTransformer<Graph> {
        Graph source = null;
        List<Dictionary<Node, Node>> matches;
        Dictionary<Node, Node> selectedMatch; // Associates <node in source graph, node in query graph> with each other
        Graph query = null;
        bool findFirst = false;
        public bool FindFirst {
            get { return findFirst; }
            set { findFirst = value; }
        }

        private Rule<Graph> rule = null;
        public Rule<Graph> Rule {
            get { return rule; }
            set { rule = value; }
        }

        public Graph Source {
            get { return source; }
            set {
                if (source != value) {
                    if (matches != null) {
                        matches.Clear();
                    }
                    matches = null;
                    selectedMatch = null;
                    query = null;
                    source = value;
                }
            }
        }

        public IDictionary<string, AttributedElement> SelectedMatch {
            get {
                Dictionary<string, AttributedElement> dict = new Dictionary<string, AttributedElement>();
                if (source == null || query == null || selectedMatch == null || selectedMatch.Count == 0) return dict;
                foreach (KeyValuePair<Node, Node> pair in selectedMatch) {
                    dict.Add("query_" + pair.Value.GetID(), pair.Key);
                }
                return dict;
            }
        }

        protected Traverser<Graph> traverser = null;
        public Traverser<Graph> Traverser {
            get { return traverser; }
            set { traverser = value; }
        }

        public GraphTransformer() {
            selectedMatch = null;
            findFirst = false;
            rule = null;
            traverser = null;
        }

		public HashSet<Node> GetSelectedNodes() {
            if (selectedMatch != null) {
                return new HashSet<Node>(selectedMatch.Keys);
            } else return null;
		}

        public Node GetSelectedNode(int id) {
            if (selectedMatch != null) {
                foreach (KeyValuePair<Node, Node> nodePair in selectedMatch) {
                    if (nodePair.Value.GetID() == id) return nodePair.Key;
                }
                return null;
            } else return null;
        }

        public bool Find(Graph query) {
            /* Step 1: select ( ignore attributes starting with "_grammar_" but use those for additional conditions
                       e.g. Negative conditions like Adams p13). */
            if (query == null || query.GetNodes().Count == 0) {
                if (source != null && source.GetNodes().Count == 0) {
                    this.query = query;
                    matches = new List<Dictionary<Node, Node>>();
                    selectedMatch = null;
                    return true;
                }
                return false;
            }
            if (source == null || source.GetNodes().Count == 0) return false;

            selectedMatch = null;
            matches = new List<Dictionary<Node, Node>>();
            this.query = query;
            List<Node> queryNodes = null;
            if (Traverser != null) {
                IEnumerable<Edge> currentEdges = query.GetEdges().Where(e => e.HasAttribute("_grammar_current"));
                if (currentEdges.Count() > 0) {
                    queryNodes = new List<Node>();
                    Edge currentEdge = currentEdges.First();
                    queryNodes.Add(currentEdge.GetNode1());
                    queryNodes.Add(currentEdge.GetNode2());
                } else {
                    queryNodes = query.GetNodes().OrderByDescending(n => n.GetAttributes().Count).ToList(); // start with most specific node
                }
            } else {
                queryNodes = query.GetNodes().OrderByDescending(n => n.GetAttributes().Count).ToList(); // start with most specific node
            }
            Dictionary<Node, Node> selection = new Dictionary<Node, Node>(); // source, query

            // Temporarily turn off events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(true);
            }
            foreach (Node startNode in source.GetNodes()) {
                if (findFirst && matches.Count > 0) continue;
                if (!MatchAttributes(startNode, queryNodes[0])) continue;
                if (startNode.GetEdges().Count < queryNodes[0].GetEdges().Count) continue;
                // Is node already marked? (useful when using multiple graph transformers)
                if (startNode.HasAttribute("_grammar_query_id")) continue;
                // add number attribute
                startNode.SetAttribute("_grammar_query_id", queryNodes[0].GetID().ToString());
                selection.Add(startNode, queryNodes[0]);
                _Find(startNode, queryNodes[0], selection);
                if (selection.Count > 1) {
                    // Only partly matched - clean up selection
                    foreach (KeyValuePair<Node, Node> wrongMark in selection) {
                        wrongMark.Key.RemoveAttribute("_grammar_query_id");
                    }
                    selection.Clear();
                } else {
                    // remove number attribute
                    startNode.RemoveAttribute("_grammar_query_id");
                    selection.Remove(startNode);
                }
            }
            return _Find_End();
        }

        /// <summary>
        /// Private helper method for Find. Contains the body of the subgraph matching algorithm.
        /// </summary>
        /// <param name="currentSourceNode">the node in the source graph that is currently observed</param>
        /// <param name="currentQueryNode">the node in the query graph that is currently observed</param>
        /// <param name="selection">a dictionary of nodes that have already been selected (Source node, Query node)</param>
        /// <returns>True if the path being followed in the query graph is a dead end (except for already marked nodes)</returns>
        private bool _Find(Node currentSourceNode, Node currentQueryNode, Dictionary<Node, Node> selection) {
            if (query.GetNodes().Count == selection.Keys.Count) {
                // Algorithm ends here
                matches.Add(new Dictionary<Node, Node>(selection));
                return false;
            }

            // Determine the most specific node(s) that will be best for querying first
            List<Node> queryNodes;
            if (Traverser != null && Traverser.CurrentElement != null && selection.Count == 1) {
                queryNodes = new HashSet<Node>(currentQueryNode.GetEdges().Keys).Except(selection.Values)
                .OrderByDescending(n => currentQueryNode.GetEdges()[n].HasAttribute("_grammar_current") ? int.MaxValue : n.GetAttributes().Count).ToList();
            } else {
                queryNodes = new HashSet<Node>(currentQueryNode.GetEdges().Keys).Except(selection.Values)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            }
            List<Node> sourceNodes = new HashSet<Node>(currentSourceNode.GetEdges().Keys).Except(selection.Keys)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            if (sourceNodes.Count < queryNodes.Count) return false;
            if (queryNodes.Count == 0) return true; // Nothing else to query along this path => dead end
            Node queryNode = queryNodes.First();
            Edge queryEdge = currentQueryNode.GetEdges()[queryNode];
            //bool noEdge = false;
            if (currentQueryNode.GetEdges()[queryNode].HasAttribute("_grammar_noEdge")) {
                //noEdge = true;
                sourceNodes = source.GetNodes().Except(currentSourceNode.GetEdges().Keys).Except(selection.Keys)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            }
            
            foreach (Node node in sourceNodes) {
                if (findFirst && matches.Count > 0) continue;
                // Is node already marked? (useful when using multiple graph transformers)
                if (node.HasAttribute("_grammar_query_id")) continue;
                // Compare edge
                Edge sourceEdge = currentSourceNode.GetEdges()[node];
                if (Traverser != null && Traverser.CurrentElement != null) {
                    if (queryEdge.HasAttribute("_grammar_current") && sourceEdge != Traverser.CurrentElement) continue;
                }
                if (!MatchAttributes(sourceEdge, queryEdge)) continue; // Compare edge attributes
                // Compare node at the other end
                if (!MatchAttributes(node, queryNode)) continue; // Compare node attributes
                if (node.GetEdges().Count < queryNode.GetEdges().Values.Where(e => !e.HasAttribute("_grammar_noEdge")).Count()) continue; // Compare edge count
                /*if (noEdge) {
                    if (node.GetEdges().Count < queryNode.GetEdges().Values.Where(e => !e.HasAttribute("_grammar_noEdge")).Count()) continue;
                } else {
                    if (node.GetEdges().Count < queryNode.GetEdges().Count) continue;
                }*/
                // For all edges that node has to nodes already selected (including currentNode): compare edge
                HashSet<Node> adjacentMarkedNodes = new HashSet<Node>(node.GetEdges().Keys.Intersect(selection.Keys));
                bool edgesValid = true;
                foreach (Node markedNode in adjacentMarkedNodes) {
                    // Get the query node that matches this marked source node
                    Node markedQueryNode = selection.Values.Where(n => (n.GetID().ToString() == markedNode["_grammar_query_id"])).First();
                    Edge markedSourceEdge = node.GetEdges()[markedNode];
                    Edge markedQueryEdge = queryNode.GetEdges().ContainsKey(markedQueryNode) ? queryNode.GetEdges()[markedQueryNode] : null;
                    if (markedQueryEdge == null || markedQueryEdge.HasAttribute("_grammar_noEdge")) {
                        edgesValid = false;
                        break;
                    }
                    if (markedSourceEdge.IsDirected() != markedQueryEdge.IsDirected()) {
                        edgesValid = false;
                        break;
                    }
                    if (markedSourceEdge.IsDirected()) { // If directed, same direction?
                        if ((markedSourceEdge.GetNode1() == node) != (markedQueryEdge.GetNode1() == queryNode)) {
                            edgesValid = false;
                            break;
                        }
                    }
                }
                if (!edgesValid) continue;
                // add number attribute && add to selection
                node.SetAttribute("_grammar_query_id", queryNode.GetID().ToString());
                selection.Add(node, queryNode);
                
                // Copy selection and create remaining query/source nodes lists
                Dictionary<Node, Node> selectionCopy = new Dictionary<Node, Node>(selection); // source, query
                bool partlyMatched = _Find(node, queryNode, selectionCopy);
                if (partlyMatched) {
                    // Copy selection and create remaining query/source nodes lists
                    //Dictionary<Node, Node> selectionCopy = new Dictionary<Node, Node>(selection); // source, query

                    partlyMatched = _Find(currentSourceNode, currentQueryNode, selectionCopy);
                    if (partlyMatched) {
                        // If this node is also a dead end when the extra part of the selection is added, go back to previous node WITH current selection
                        IEnumerable<KeyValuePair<Node, Node>> newMarks = selectionCopy.Except(selection);
                        foreach (KeyValuePair<Node, Node> newMark in newMarks) {
                            selection.Add(newMark.Key, newMark.Value);
                        }
                        return true;
                    } else {
                        // clean up selection
                        IEnumerable<KeyValuePair<Node, Node>> wrongMarks = selectionCopy.Except(selection);
                        foreach (KeyValuePair<Node, Node> wrongMark in wrongMarks) {
                            wrongMark.Key.RemoveAttribute("_grammar_query_id");
                        }
                    }
                }
                // remove number attribute && remove from selection
                node.RemoveAttribute("_grammar_query_id");
                selection.Remove(node);
            }
            return false;
        }

        private bool _Find_End() {
            // Reactivate events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(false);
            }
            if (matches.Count > 0) {
                return true;
            } else {
                return false;
            }
        }

        public void Select() {
            if (matches == null) return;
            if (matches.Count > 0) {
                int index = -1;
                if (rule != null && rule.MatchSelector != null) {
                    index = rule.MatchSelector.Select(matches);
                }
                if (index == -1) {
                    Random rnd = new Random();
                    index = rnd.Next(matches.Count);
                }
                selectedMatch = matches.ElementAt(index);
            } else {
                if ((query == null || query.GetNodes().Count == 0) && (source != null && source.GetNodes().Count == 0)) {
                    selectedMatch = new Dictionary<Node, Node>();
                } else selectedMatch = null;
            }
        }

        public void Transform(Graph target) {
            // Preliminary checks
            if (target == null) return;
            if (selectedMatch == null) return;
            
            // Temporarily turn off events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(true);
            }
            foreach (Edge edge in source.GetEdges()) {
                edge.PostponeAttributeChanged(true);
            }

            /* Step 2: Number nodes according to query(using the attribute "_grammar_query_id") */
            foreach (KeyValuePair<Node, Node> nodePair in selectedMatch) {
                nodePair.Key["_grammar_query_id"] = nodePair.Value.GetID().ToString();
            }

            /* Step 3: remove nodes & edges
               Step 4: replace numbered nodes by their equivalents (just change their attributes), and edges as well */
            List<int> oldNodeIDs = new List<int>(); // List of node IDs present in query graph, for easier searching later
            Dictionary<Node, Node> nodeTransCopy = new Dictionary<Node, Node>(selectedMatch);
            foreach (KeyValuePair<Node, Node> marking in nodeTransCopy) {
                oldNodeIDs.Add(marking.Value.GetID());
                Node sourceNode = marking.Key;
                Node queryNode = marking.Value;
                Node targetNode = target.GetNodeByID(queryNode.GetID());
                if (targetNode == null) { // If a node in the query doesn't exist in the target graph
                    sourceNode.Destroy();
                    selectedMatch.Remove(sourceNode);
                } else {
                    // For all edges outgoing from this node in query graph, check if there is a corresponding edge in target graph
                    foreach (Edge queryEdge in queryNode.GetEdges().Values) {
                        if (queryEdge.GetNode1() != queryNode) continue;
                        if (queryEdge.HasAttribute("_grammar_noEdge")) continue;
                        // Find that edge in the target graph
                        Edge targetEdge = null;
                        foreach (Edge targetEdgeIter in targetNode.GetEdges().Values) {
                            if (queryEdge.EqualsOtherGraphEdge(targetEdgeIter)) {
                                targetEdge = targetEdgeIter;
                                break;
                            }
                        }
                        // Find that edge in the source graph
                        Edge sourceEdge = null;
                        foreach (Edge sourceEdgeIter in marking.Key.GetEdges().Values) {
                            if (queryEdge.EqualsOtherGraphEdge(sourceEdgeIter, false, true)) {
                                sourceEdge = sourceEdgeIter;
                                break;
                            }
                        }
                        if (targetEdge == null) {
                            // Delete that edge in the source graph (may already have been destroyed by nodes being deleted).
                            if(sourceEdge != null) sourceEdge.Destroy();
                        } else {
                            // Copy difference in attributes between query & target to source edge.
                            SetAttributesUsingDifference(sourceEdge, queryEdge, targetEdge);
                        }
                    }
                    // Copy difference in attributes between query & target to source node.
                    SetAttributesUsingDifference(sourceNode, queryNode, targetNode);
                }
            }
            /* Step 5: add new nodes to graph */
            Dictionary<int, Node> newNodes = new Dictionary<int, Node>();
            foreach (Node targetNode in target.GetNodes()) {
                if (!oldNodeIDs.Contains(targetNode.GetID())) {
                    Node sourceNode = new Node(source, source.GetNodes().Count);
                    SetAttributesUsingDifference(sourceNode, null, targetNode); // Copies attributes & attribute classes
                    newNodes.Add(targetNode.GetID(), sourceNode);
                }
            }

            /* Step 6: add new edges: Cycle through new nodes another time to add missing edges */
            foreach (Edge targetEdge in target.GetEdges()) {
                bool existingEdge = false;
                int id1 = targetEdge.GetNode1().GetID();
                int id2 = targetEdge.GetNode2().GetID();
                // Is this edge connected to a new node?
                if (!newNodes.ContainsKey(id1) && !newNodes.ContainsKey(id2)) {
                    // If not, we should still check if it's nonexistent in the query graph.
                    foreach (Edge queryEdge in query.GetEdges()) {
                        if (targetEdge.EqualsOtherGraphEdge(queryEdge) && !queryEdge.HasAttribute("_grammar_noEdge")) {
                            existingEdge = true;
                            break;
                        }
                    }
                }
                if (!existingEdge) {
                    bool directed = targetEdge.IsDirected();
                    Node sourceNode1 = null;
                    Node sourceNode2 = null;
                    if (newNodes.ContainsKey(id1)) sourceNode1 = newNodes[id1];
                    if (newNodes.ContainsKey(id2)) sourceNode2 = newNodes[id2];
                    foreach (Node sourceNode in selectedMatch.Keys) {
                        if (sourceNode["_grammar_query_id"] == id1.ToString()) {
                            sourceNode1 = sourceNode;
                        } else if (sourceNode["_grammar_query_id"] == id2.ToString()) {
                            sourceNode2 = sourceNode;
                        }
                        if (sourceNode1 != null && sourceNode2 != null) break;
                    }
                    Edge edge = new Edge(source, sourceNode1, sourceNode2, directed);
                    SetAttributesUsingDifference(edge, null, targetEdge);
                }
            }

            /* Step 7: Remove "_grammar_query_id" attribute */
            foreach (Node sourceNode in selectedMatch.Keys) {
                sourceNode.RemoveAttribute("_grammar_query_id");
            }
            selectedMatch.Clear();
            matches.Clear();
            selectedMatch = null;
            matches = null;

            // Reactivate events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(false);
            }
            foreach (Edge edge in source.GetEdges()) {
                edge.PostponeAttributeChanged(false);
            }

            /*
            Dormans (2010):
            Step 1: select -- done
            Step 2: number nodes accoding to query -- done
            Step 3: remove all edges between selected nodes
            Step 4: replace numbered nodes by their equivalents (just change their attributes)
            Step 5: add new nodes to graph
            Step 6: add edges for new nodes
            Step 7: remove number attribute
            NOTE THAT DORMANS DOES NOT TAKE INTO ACCOUNT HOW TO REMOVE NODES.

            Adapted:
            Step 1: select -- done
            Step 2: number nodes accoding to query -- done
            Step 3: remove nodes, then edges
            Step 4: replace numbered nodes by their equivalents (just change their attributes), then edges
            Step 5: add new nodes to graph
            Step 6: add edges for new nodes
            Step 7: remove "_grammar_" attributes
            */
        }

        protected bool MatchAttributes(AttributedElement sourceEl, AttributedElement queryEl) {
            if (sourceEl == null || queryEl == null) return false;
            if (rule != null) {
                sourceEl.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                sourceEl.SetObjectAttribute("rule", rule, notify: false);
                queryEl.SetObjectAttribute("_grammar_matching", sourceEl, notify: false);
                bool match = sourceEl.MatchAttributes(queryEl);
                queryEl.RemoveObjectAttribute("_grammar_matching", notify: false);
                sourceEl.RemoveObjectAttribute("grammar", notify: false);
                sourceEl.RemoveObjectAttribute("rule", notify: false);
                return match;
            } else return sourceEl.MatchAttributes(queryEl);
        }

        protected void SetAttributesUsingDifference(AttributedElement sourceEl, AttributedElement queryEl, AttributedElement targetEl) {
            if (sourceEl == null || targetEl == null) return;
            if (rule != null) {
                sourceEl.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                sourceEl.SetObjectAttribute("rule", rule, notify: false);
                sourceEl.SetAttributesUsingDifference(queryEl, targetEl, notify: false);
                sourceEl.RemoveObjectAttribute("grammar", notify: false);
                sourceEl.RemoveObjectAttribute("rule", notify: false);
            } else sourceEl.SetAttributesUsingDifference(queryEl, targetEl, notify: false);
        }

        public void Destroy() {
            if (selectedMatch != null) {
                foreach (Node sourceNode in selectedMatch.Keys) {
                    sourceNode.RemoveAttribute("_grammar_query_id");
                }
                selectedMatch.Clear();
                selectedMatch = null;
            }
            Source = null;
        }
    }
}
