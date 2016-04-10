using System.Collections.Generic;
using System.Linq;

namespace Grammars.Graphs {
    public class Algorithms {
        public static int Distance(Node start, Node end, int maxLen = 0, string nodeCondition = null) {
            List<Node> shortestPath = ShortestPath(start, end, maxLen, nodeCondition);
            if (shortestPath == null || shortestPath.Count == 0) return -1;
            return shortestPath.Count-1;
        }

        public static List<Node> ShortestPath(Node start, Node end, int maxLen = 0, string nodeCondition = null) {
            if (start == null || end == null) return null;
            if (nodeCondition != null && nodeCondition.Trim() != "") {
                List<AttributedElement> startEndCheck = new List<AttributedElement>();
                startEndCheck.Add(start);
                startEndCheck.Add(end);
                List<AttributedElement> res = StringEvaluator.SelectElementsFromList(startEndCheck, nodeCondition);
                if (res.Count != 2) return null;
            }
            List<Node> path = new List<Node>();
            Dictionary<Node, int> visited = new Dictionary<Node, int>();
            visited.Add(start, 0);
            path.Add(start);
            List<Node> shortestPath = _ShortestPath(path, end, null, visited, maxLen: maxLen, nodeCondition: nodeCondition);
            return shortestPath;
        }

        private static List<Node> _ShortestPath(List<Node> curPath, Node end, List<Node> shortestPath, Dictionary<Node, int> visited, int maxLen = 0, string nodeCondition = null) {
            if (curPath == null || curPath.Count == 0) return null;
            int moves = curPath.Count;
            if (maxLen > 0 && moves >= maxLen) return null;
            Node curNode = curPath.Last();

            if (curNode.GetEdges().ContainsKey(end)
                && (!curNode.GetEdges()[end].IsDirected() || curNode.GetEdges()[end].GetNode1() == curNode)
                && (!visited.ContainsKey(end) || visited[end] > moves)) {
                curPath.Add(end);
                shortestPath = new List<Node>(curPath);
                curPath.Remove(end);
                return shortestPath;
            }

            List<AttributedElement> targets = curNode.GetEdges()
                .Where(p => p.Value != null && (!p.Value.IsDirected() || p.Value.GetNode1() == curNode))
                .Select(p => (AttributedElement)(p.Key))
                .ToList();
            if (nodeCondition != null && nodeCondition.Trim() != "") {
                targets = StringEvaluator.SelectElementsFromList(targets, nodeCondition);
            }
            List<Node> nodeTargets = targets.Select(e => (Node)e).ToList();
            foreach (Node target in nodeTargets) {
                if (!curPath.Contains(target) && (!visited.ContainsKey(target) || visited[target] > moves)) {
                    visited[target] = moves;
                    curPath.Add(target);
                    List<Node> newShortest = _ShortestPath(curPath, end, null, visited, maxLen, nodeCondition);
                    if (newShortest != null && (shortestPath == null || newShortest.Count < shortestPath.Count)) shortestPath = newShortest;
                    curPath.Remove(target);
                }
            }
            return shortestPath;
        }

    }
}
