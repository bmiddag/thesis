using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class GraphTransformer : IStructureTransformer<Graph> {
        Graph source = null;
        Dictionary<Node, Node> nodeTransformations; // Associates <node in source graph, node in query graph> with each other
        Graph query = null;

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

        public GraphTransformer() {
            nodeTransformations = null;
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
            this.query = query;
            List<Node> queryNodes = query.GetNodes().OrderByDescending(n => n.GetAttributes().Count).ToList(); // start with most specific node
            Dictionary<Node, Node> selection = new Dictionary<Node, Node>(); // source, query

            // Temporarily turn off events
            foreach (Node node in source.GetNodes()) {
                node.PostponeAttributeChanged(true);
            }
            foreach (Node startNode in source.GetNodes()) {
                if (!startNode.MatchAttributes(queryNodes[0])) continue;
                if (startNode.GetEdges().Count < queryNodes[0].GetEdges().Count) continue;
                // add number attribute
                startNode.SetAttribute("_grammar_query_id", queryNodes[0].GetID().ToString());
                selection.Add(startNode, queryNodes[0]);
                if (query.GetNodes().Count == 1) return true;
                bool found = _Find(startNode, queryNodes[0], selection);
                if (found) {
                    nodeTransformations = selection;
                    // Reactivate events
                    foreach (Node node in source.GetNodes()) {
                        node.PostponeAttributeChanged(false);
                    }
                    return true;
                }
                // remove number attribute
                startNode.RemoveAttribute("_grammar_query_id");
                selection.Remove(startNode);
            }
            return false;
        }

        /// <summary>
        /// Private helper method for Find. Contains the body of the subgraph matching algorithm.
        /// </summary>
        /// <param name="currentSourceNode">the node in the source graph that is currently observed</param>
        /// <param name="currentQueryNode">the node in the query graph that is currently observed</param>
        /// <param name="selection">a dictionary of nodes that have already been selected (Source node, Query node)</param>
        /// <returns>True if the path being followed in the query graph is a dead end (except for already marked nodes)</returns>
        private bool _Find(Node currentSourceNode, Node currentQueryNode, Dictionary<Node, Node> selection) {
            // Determine the most specific node(s) that will be best for querying first
            List<Node> queryNodes = new HashSet<Node>(currentQueryNode.GetEdges().Keys).Except(selection.Values)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            List<Node> sourceNodes = new HashSet<Node>(currentSourceNode.GetEdges().Keys).Except(selection.Keys)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            if (sourceNodes.Count < queryNodes.Count) return false;
            if (queryNodes.Count == 0) return true;
            Node queryNode = queryNodes.First();
            
            foreach (Node node in sourceNodes) {
                // Is node already marked? (shouldn't happen)
                if (node.HasAttribute("_grammar_query_id")) continue;
                // Compare node at the other end
                if (!node.MatchAttributes(queryNode)) continue; // Compare node attributes
                if (node.GetEdges().Count < queryNode.GetEdges().Count) continue; // Compare edge count
                // For all edges that node has to nodes already selected (including currentNode): compare edge
                HashSet<Node> adjacentMarkedNodes = new HashSet<Node>(node.GetEdges().Keys.Intersect(selection.Keys));
                foreach (Node markedNode in adjacentMarkedNodes) {
                    Node markedQueryNode = selection.Values.Where(n => (n.GetID().ToString() == markedNode["_grammar_query_id"])).First();
                    Edge sourceEdge = node.GetEdges()[markedNode];
                    Edge queryEdge = queryNode.GetEdges()[markedQueryNode];
                    if (queryEdge == null) continue;
                    if (sourceEdge.IsDirected() != queryEdge.IsDirected()) continue; // Directed?
                    if (sourceEdge.IsDirected()) { // If directed, same direction?
                        if ((sourceEdge.GetNode1() == node) != (queryEdge.GetNode1() == queryNode)) continue;
                    }
                }
                // add number attribute && add to selection
                node.SetAttribute("_grammar_query_id", queryNode.GetID().ToString());
                selection.Add(node, queryNode);
                if (query.GetNodes().Count == selection.Keys.Count) {
                    // Algorithm ends here
                    //if(nodeTransformations != null) nodeTransformations = new Dictionary<Node, Node>(selection);
                    return true;
                }
                bool found = _Find(node, queryNode, selection);
                if(found) {
                    // Copy selection and create remaining query/source nodes lists
                    Dictionary<Node, Node> selectionCopy = new Dictionary<Node, Node>(selection); // source, query

                    while (found) {
                        if (query.GetNodes().Count == selectionCopy.Keys.Count) {
                            // Algorithm ends here
                            //if (nodeTransformations != null) nodeTransformations = new Dictionary<Node, Node>(selectionCopy);
                            IEnumerable<KeyValuePair<Node, Node>> additionalSelection = selectionCopy.Except(selection);
                            foreach (KeyValuePair<Node, Node> add in additionalSelection) {
                                selection.Add(add.Key, add.Value);
                            }
                            return true;
                        }
                        found = _Find(currentSourceNode, currentQueryNode, selectionCopy);
                    }

                    // remove number attr from all elements in selectionCopy.except(selection)
                    IEnumerable<KeyValuePair<Node, Node>> wrongMarks = selectionCopy.Except(selection);
                    foreach (KeyValuePair<Node, Node> wrongMark in wrongMarks) {
                        wrongMark.Value.RemoveAttribute("_grammar_query_id");
                    }
                }
                // remove number attribute && remove from selection
                node.RemoveAttribute("_grammar_query_id");
                selection.Remove(node);
            }
            return false;
        }

        public void Transform(Graph target) {
            // Preliminary checks
            if (query == null || target == null) return;
            
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
                // Is this edge connected to a new node?
                if (!newNodes.ContainsKey(targetEdge.GetNode1().GetID()) && !newNodes.ContainsKey(targetEdge.GetNode2().GetID())) {
                    // If not, we should still check if it's nonexistent in the query graph.
                    foreach (Edge queryEdge in query.GetEdges()) {
                        if (targetEdge.EqualsOtherGraphEdge(queryEdge)) {
                            existingEdge = true;
                            break;
                        }
                    }
                }
                if (!existingEdge) {
                    int id1 = targetEdge.GetNode1().GetID();
                    int id2 = targetEdge.GetNode2().GetID();
                    bool directed = targetEdge.IsDirected();
                    Node sourceNode1 = null;
                    Node sourceNode2 = null;
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
    }
}
