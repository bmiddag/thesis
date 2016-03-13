using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class GraphTransformer : IStructureTransformer<Graph> {
        Graph source = null;
        public Dictionary<Node, Node> nodeTransformations; // Associates <node in source graph, node in query graph> with each other
        Graph query = null;
        bool findFirst = false;

        public Graph Source {
            get {
                return source;
            }
            set {
                if (source != value) {
                    nodeTransformations = null;
                    query = null;
                    source = value;
                }
            }
        }

        public GraphTransformer(bool findFirst = false) {
            nodeTransformations = null;
            this.findFirst = findFirst;
		}

		public HashSet<Node> GetSelectedNodes() {
            if (nodeTransformations != null) {
                return new HashSet<Node>(nodeTransformations.Keys);
            } else return null;
		}

        public bool Find(Graph query) {
            /* Step 1: select ( ignore attributes starting with "_grammar_" but use those for additional conditions
                       e.g. Negative conditions like Adams p13).
               Step 2: Number nodes according to query (using the attribute "_grammar_query_id") */
            if (source == null || source.GetNodes().Count == 0) return false;
            if (query == null || query.GetNodes().Count == 0) return false;

            nodeTransformations = null;
            List<Dictionary<Node, Node>> transformationsList = new List<Dictionary<Node, Node>>();
            this.query = query;
            List<Node> queryNodes = query.GetNodes().OrderByDescending(n => n.GetAttributes().Count).ToList(); // start with most specific node
            Dictionary<Node, Node> selection = new Dictionary<Node, Node>(); // source, query

            // Temporarily turn off events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(true);
            }
            foreach (Node startNode in source.GetNodes()) {
                if (findFirst && transformationsList.Count > 0) continue;
                if (!startNode.MatchAttributes(queryNodes[0])) continue;
                if (startNode.GetEdges().Count < queryNodes[0].GetEdges().Count) continue;
                // Is node already marked? (useful when using multiple graph transformers)
                if (startNode.HasAttribute("_grammar_query_id")) continue;
                // add number attribute
                startNode.SetAttribute("_grammar_query_id", queryNodes[0].GetID().ToString());
                selection.Add(startNode, queryNodes[0]);
                _Find(startNode, queryNodes[0], selection, transformationsList);
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
            return _Find_End(transformationsList);
        }

        /// <summary>
        /// Private helper method for Find. Contains the body of the subgraph matching algorithm.
        /// </summary>
        /// <param name="currentSourceNode">the node in the source graph that is currently observed</param>
        /// <param name="currentQueryNode">the node in the query graph that is currently observed</param>
        /// <param name="selection">a dictionary of nodes that have already been selected (Source node, Query node)</param>
        /// <param name="transformationsList">list of all complete matches found so far</param>
        /// <returns>True if the path being followed in the query graph is a dead end (except for already marked nodes)</returns>
        private bool _Find(Node currentSourceNode, Node currentQueryNode, Dictionary<Node, Node> selection, List<Dictionary<Node, Node>> transformationsList) {
            if (query.GetNodes().Count == selection.Keys.Count) {
                // Algorithm ends here
                transformationsList.Add(new Dictionary<Node, Node>(selection));
                return false;
            }

            // Determine the most specific node(s) that will be best for querying first
            List<Node> queryNodes = new HashSet<Node>(currentQueryNode.GetEdges().Keys).Except(selection.Values)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            List<Node> sourceNodes = new HashSet<Node>(currentSourceNode.GetEdges().Keys).Except(selection.Keys)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            if (sourceNodes.Count < queryNodes.Count) return false;
            if (queryNodes.Count == 0) return true; // Nothing else to query along this path => dead end
            Node queryNode = queryNodes.First();
            
            foreach (Node node in sourceNodes) {
                if (findFirst && transformationsList.Count > 0) continue;
                // Is node already marked? (useful when using multiple graph transformers)
                if (node.HasAttribute("_grammar_query_id")) continue;
                // Compare node at the other end
                if (!node.MatchAttributes(queryNode)) continue; // Compare node attributes
                if (node.GetEdges().Count < queryNode.GetEdges().Count) continue; // Compare edge count
                // For all edges that node has to nodes already selected (including currentNode): compare edge
                HashSet<Node> adjacentMarkedNodes = new HashSet<Node>(node.GetEdges().Keys.Intersect(selection.Keys));
                bool edgesValid = true;
                foreach (Node markedNode in adjacentMarkedNodes) {
                    // Get the query node that matches this marked source node
                    Node markedQueryNode = selection.Values.Where(n => (n.GetID().ToString() == markedNode["_grammar_query_id"])).First();
                    Edge sourceEdge = node.GetEdges()[markedNode];
                    Edge queryEdge = queryNode.GetEdges()[markedQueryNode];
                    if (queryEdge == null) {
                        edgesValid = false;
                        break;
                    }
                    if (sourceEdge.IsDirected() != queryEdge.IsDirected()) {
                        edgesValid = false;
                        break;
                    }
                    if (sourceEdge.IsDirected()) { // If directed, same direction?
                        if ((sourceEdge.GetNode1() == node) != (queryEdge.GetNode1() == queryNode)) {
                            edgesValid = false;
                            break;
                        }
                    }
                }
                if (!edgesValid) continue;
                // add number attribute && add to selection
                node.SetAttribute("_grammar_query_id", queryNode.GetID().ToString());
                selection.Add(node, queryNode);
                bool partlyMatched = _Find(node, queryNode, selection, transformationsList);
                if (partlyMatched) {
                    // Copy selection and create remaining query/source nodes lists
                    Dictionary<Node, Node> selectionCopy = new Dictionary<Node, Node>(selection); // source, query

                    partlyMatched = _Find(currentSourceNode, currentQueryNode, selectionCopy, transformationsList);
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

        private bool _Find_End(List<Dictionary<Node, Node>> transformationsList) {
            // Reactivate events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(false);
            }
            if (transformationsList.Count > 0) {
                Random rnd = new Random();
                int r = rnd.Next(transformationsList.Count);
                nodeTransformations = transformationsList.ElementAt(r);
                foreach (KeyValuePair<Node, Node> nodePair in nodeTransformations) {
                    nodePair.Key["_grammar_query_id"] = nodePair.Value.GetID().ToString();
                }
                return true;
            } else {
                nodeTransformations = null;
                return false;
            }
        }

        public void Transform(Graph target) {
            // Preliminary checks
            if (query == null || target == null) return;
            if (nodeTransformations == null) return;
            
            // Temporarily turn off events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(true);
            }
            foreach (Edge edge in source.GetEdges()) {
                edge.PostponeAttributeChanged(true);
            }

            /* Step 3: remove nodes & edges
               Step 4: replace numbered nodes by their equivalents (just change their attributes), and edges as well */
            List<int> oldNodeIDs = new List<int>(); // List of node IDs present in query graph, for easier searching later
            Dictionary<Node, Node> nodeTransCopy = new Dictionary<Node, Node>(nodeTransformations);
            foreach (KeyValuePair<Node, Node> marking in nodeTransCopy) {
                oldNodeIDs.Add(marking.Value.GetID());
                Node sourceNode = marking.Key;
                Node queryNode = marking.Value;
                Node targetNode = target.GetNodeByID(queryNode.GetID());
                if (targetNode == null) { // If a node in the query doesn't exist in the target graph
                    sourceNode.Destroy();
                    nodeTransformations.Remove(sourceNode);
                } else {
                    // For all edges outgoing from this node in query graph, check if there is a corresponding edge in target graph
                    foreach (Edge queryEdge in queryNode.GetEdges().Values) {
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
                            // Delete that edge in the source graph.
                            sourceEdge.Destroy();
                        } else {
                            // Copy difference in attributes between query & target to source edge.
                            sourceEdge.SetAttributesUsingDifference(queryEdge, targetEdge);
                        }
                    }
                    // Copy difference in attributes between query & target to source node.
                    sourceNode.SetAttributesUsingDifference(queryNode, targetNode);
                }
            }
            /* Step 5: add new nodes to graph */
            Dictionary<int, Node> newNodes = new Dictionary<int, Node>();
            foreach (Node targetNode in target.GetNodes()) {
                if (!oldNodeIDs.Contains(targetNode.GetID())) {
                    Node sourceNode = new Node(source, source.GetNodes().Count);
                    sourceNode.SetAttributesUsingDifference(null, targetNode); // Copies attributes & attribute classes
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
                        if (targetEdge.EqualsOtherGraphEdge(queryEdge)) {
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
                    foreach (Node sourceNode in nodeTransformations.Keys) {
                        if (sourceNode["_grammar_query_id"] == id1.ToString()) {
                            sourceNode1 = sourceNode;
                        } else if (sourceNode["_grammar_query_id"] == id2.ToString()) {
                            sourceNode2 = sourceNode;
                        }
                        if (sourceNode1 != null && sourceNode2 != null) break;
                    }
                    Edge edge = new Edge(source, sourceNode1, sourceNode2, directed);
                    edge.SetAttributesUsingDifference(null, targetEdge);
                }
            }

            /* Step 7: Remove "_grammar_query_id" attribute */
            foreach (Node sourceNode in nodeTransformations.Keys) {
                sourceNode.RemoveAttribute("_grammar_query_id");
            }
            nodeTransformations.Clear();

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

        public void Destroy() {
            if (nodeTransformations == null) return;
            foreach (Node sourceNode in nodeTransformations.Keys) {
                sourceNode.RemoveAttribute("_grammar_query_id");
            }
            nodeTransformations.Clear();
            Source = null;
        }
    }
}
