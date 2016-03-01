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
            if (source != null) {
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
                        return true;
                    }
                    // remove number attribute
                    startNode.RemoveAttribute("_grammar_query_id");
                    selection.Remove(startNode);
                }
                return true;
            } else return false;
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
            throw new NotImplementedException();
            // Preliminary checks
            if (query == null || target == null) return;
            /* Step 3: remove nodes & edges
               Step 4: replace numbered nodes by their equivalents (just change their attributes), and edges as well */
            Dictionary<Node, Node> nodeTransCopy = new Dictionary<Node, Node>(nodeTransformations);
            foreach (KeyValuePair<Node, Node> marking in nodeTransCopy) {
                Node matchingNode = target.GetNodeByID(marking.Value.GetID());
                if (matchingNode == null) { // If a node in the query doesn't exist in the target graph
                    marking.Key.Destroy();
                    nodeTransformations.Remove(marking.Key);
                } else {
                    // For all edges outgoing from this node in query graph, check if there is a corresponding edge in target graph
                    foreach (Edge edge in marking.Value.GetEdges().Values) {
                        Edge matchingEdge = null;
                        foreach (Edge targetEdge in matchingNode.GetEdges().Values) {
                            if (edge.EqualsOtherGraphEdge(targetEdge)) {
                                matchingEdge = targetEdge;
                                break;
                            }
                        }
                        if (matchingEdge == null) {
                            // Delete that edge in the source graph.
                            foreach (Edge sourceEdge in marking.Key.GetEdges().Values) {
                                if (edge.EqualsOtherGraphEdge(sourceEdge,false,true)) {
                                    sourceEdge.Destroy();
                                    break;
                                }
                            }
                        } else {
                            // Copy difference in attributes between query & target to source edge.
                        }
                    }
                    // Now for node business
                    // Copy difference in attributes between query & target to source node.
                }
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

            /* for every element:
            1. apply removed classes
            2. apply new classes
            3. apply removed attributes
            4. apply new attributes */
        }
    }
}
