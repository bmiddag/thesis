using System;
using System.Collections.Generic;
using System.Linq;

namespace Grammars.Tiles {
    public class Algorithms {
        public static int Distance(Tile t1, Tile t2) {
            if (t1 == null || t2 == null) return -1;
            if (t1.Container != t2.Container) return -1;
            TilePos t1Pos = t1.GetIndices();
            TilePos t2Pos = t2.GetIndices();
            return Math.Abs(t1Pos.x - t2Pos.x) + Math.Abs(t1Pos.y - t2Pos.y);
        }

        public static List<Tile> ShortestPath(Tile start, Tile end, int maxLen = 0, string tileCondition = null) {
            if (start == null || end == null) return null;
            if (tileCondition != null && tileCondition.Trim() != "") {
                List<AttributedElement> startEndCheck = new List<AttributedElement>();
                startEndCheck.Add(start);
                startEndCheck.Add(end);
                List<AttributedElement> res = StringEvaluator.SelectElementsFromList(startEndCheck, tileCondition);
                if (res.Count != 2) return null;
            }
            List<Tile> path = new List<Tile>();
            Dictionary<Tile, int> visited = new Dictionary<Tile, int>();
            visited.Add(start, 0);
            path.Add(start);
            List<Tile> shortestPath = _ShortestPath(path, end, null, visited, maxLen: maxLen, tileCondition: tileCondition);
            return shortestPath;
        }

        private static List<Tile> _ShortestPath(List<Tile> curPath, Tile end, List<Tile> shortestPath, Dictionary<Tile, int> visited, int maxLen = 0, string tileCondition = null) {
            if (curPath == null || curPath.Count == 0) return null;
            int moves = curPath.Count;
            if (maxLen > 0 && moves >= maxLen) return null;
            Tile curTile = curPath.Last();

            if (curTile.IsAdjacent(end) && (!visited.ContainsKey(end) || visited[end] > moves)) {
                curPath.Add(end);
                shortestPath = new List<Tile>(curPath);
                curPath.Remove(end);
                return shortestPath;
            }

            List<AttributedElement> targets = curTile.GetNeighbors()
                .Select(p => (AttributedElement)(p.Value))
                .ToList();
            if (tileCondition != null && tileCondition.Trim() != "") {
                targets = StringEvaluator.SelectElementsFromList(targets, tileCondition);
            }
            List<Tile> tileTargets = targets.Select(e => (Tile)e).OrderBy(t => Distance(t,end)).ToList();
            foreach (Tile target in tileTargets) {
                if (!curPath.Contains(target) && (!visited.ContainsKey(target) || visited[target] > moves)) {
                    visited[target] = moves;
                    curPath.Add(target);
                    List<Tile> newShortest = _ShortestPath(curPath, end, null, visited, maxLen, tileCondition);
                    if (newShortest != null && (shortestPath == null || newShortest.Count < shortestPath.Count)) shortestPath = newShortest;
                    curPath.Remove(target);
                }
            }
            return shortestPath;
        }

    }
}
