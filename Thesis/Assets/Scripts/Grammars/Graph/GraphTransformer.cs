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
            throw new NotImplementedException();
            /*
            Dormans (2010):
            Step 1: select ( ignore attributes starting with "_grammar_" but use those for additional conditions
                    e.g. Negative conditions like Adams p13)
            Step 2: number nodes accoding to query AFTER the complete structure has been found (i.e. at the tail of the recursion).
                    better do this as an attribute "_grammar_query_id"
            */
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
