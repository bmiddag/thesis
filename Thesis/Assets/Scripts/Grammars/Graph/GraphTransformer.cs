using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class GraphTransformer : IStructureTransformer<Graph> {
        Graph source = null;
		HashSet<Node> selectedNodes;
		HashSet<Edge> selectedEdges;

        public Graph Source {
            get {
                return source;
            }
            set {
                if (source != value) {
                    selectedEdges.Clear();
                    selectedNodes.Clear();
                    source = value;
                }
            }
        }

        public GraphTransformer() {
			selectedNodes = new HashSet<Node>();
			selectedEdges = new HashSet<Edge>();
		}

		public HashSet<Node> GetSelectedNodes() {
			return selectedNodes;
		}

		public HashSet<Edge> GetSelectedEdges() {
			return selectedEdges;
		}

        public bool Find(Graph query) {
            /* Step 1: select ( ignore attributes starting with "_grammar_" but use those for additional conditions
                       e.g. Negative conditions like Adams p13).
               Step 2: Number nodes according to query (using the attribute "_grammar_query_id") */
            if (source == null || source.GetNodes().Count == 0) return false;
            if (query == null || query.GetNodes().Count == 0) return false;
            List<Node> queryNodes = query.GetNodes().OrderByDescending(n => n.GetAttributes().Count).ToList(); // start with most specific node
            Dictionary<Node, Node> selection = new Dictionary<Node, Node>(); // query, source
            if (source != null) {
                foreach (Node startNode in source.GetNodes()) {
                    if (!startNode.MatchAttributes(queryNodes[0])) continue;
                    if (startNode.GetEdges().Count < queryNodes[0].GetEdges().Count) continue;
                    // add number attribute
                    startNode.SetAttribute("_grammar_query_id", queryNodes[0].GetID().ToString());
                    selection.Add(queryNodes[0], startNode);
                    if (query.GetNodes().Count == 1) return true;
                    bool found = _Find(query, startNode, queryNodes[0], selection);
                    if (found) return true;
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
        /// <param name="query">the graph query</param>
        /// <param name="currentSourceNode">the node in the source graph that is currently observed</param>
        /// <param name="currentQueryNode">the node in the query graph that is currently observed</param>
        /// <param name="selection">a dictionary of nodes that have already been selected (Query node, Source node)</param>
        /// <returns>True if the path being followed in the query graph is a dead end (except for already marked nodes)</returns>
        private bool _Find(Graph query, Node currentSourceNode, Node currentQueryNode, Dictionary<Node, Node> selection) {
            // Determine the most specific node(s) that will be best for querying first
            List<Node> queryNodes = new HashSet<Node>(currentQueryNode.GetEdges().Keys).Except(selection.Keys)
                .OrderByDescending(n => n.GetAttributes().Count).ToList();
            List<Node> sourceNodes = new HashSet<Node>(currentSourceNode.GetEdges().Keys).Except(selection.Values)
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
                HashSet<Node> adjacentMarkedNodes = new HashSet<Node>(node.GetEdges().Keys.Intersect(selection.Values));
                foreach (Node markedNode in adjacentMarkedNodes) {
                    Node markedQueryNode = selection.Keys.Where(n => (n.GetID().ToString() == markedNode["_grammar_query_id"])).First();
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
                selection.Add(queryNode, node);
                if (query.GetNodes().Count == selection.Keys.Count) return true;
                bool found = _Find(query, node, queryNode, selection);
                if(found) {
                    // Copy selection and create remaining query/source nodes lists
                    Dictionary<Node, Node> selectionCopy = new Dictionary<Node, Node>(selection); // query, source

                    while (found) {
                        if (query.GetNodes().Count == selectionCopy.Keys.Count) return true;
                        found = _Find(query, currentSourceNode, currentQueryNode, selectionCopy);
                    }

                    // remove number attr from all elements in selectionCopy.except(selection)
                    IEnumerable<KeyValuePair<Node, Node>> wrongMarks = selectionCopy.Except(selection);
                    foreach (KeyValuePair<Node, Node> wrongMark in wrongMarks) {
                        wrongMark.Value.RemoveAttribute("_grammar_query_id");
                    }
                }
                // remove number attribute && remove from selection
                node.RemoveAttribute("_grammar_query_id");
                selection.Remove(queryNode);
            }
            return false;
        }

        public void Transform(Graph target) {
            throw new NotImplementedException();

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
