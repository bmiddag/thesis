﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        public static List<TilePos> ShortestFreePath(TileGrid grid, TilePos start, TilePos end, int width, int maxLen = 0, bool returnAll = false) {
            if (start == null || end == null) return null;
            
            List<TilePos> path = new List<TilePos>();
            Dictionary<TilePos, int> visited = new Dictionary<TilePos, int>();
            TilePos curTile = start;
            int rot = curTile.Rotation;
            curTile.Rotation = rot;
            visited.Add(start, 0);
            path.Add(start);
            List<TilePos> shortestPath = _ShortestFreePath(grid, path, end, null, visited, width, maxLen: maxLen);
            if (!returnAll || shortestPath == null) {
                if(shortestPath != null && shortestPath.Count > 0) shortestPath.RemoveAt(0);
                return shortestPath;
            } else {
                if (shortestPath.Count > 0) shortestPath.RemoveAt(0);
                List<TilePos> allTilePos = new List<TilePos>();
                foreach (TilePos pos in shortestPath) {
                    int rota = pos.Rotation;
                    for (int i = 0; i < width; i++) {
                        TilePos tempRPos = ConvertRot(new TilePos(i, 0, rotation: 0), rota);
                        TilePos tempPos = new TilePos(pos.x + tempRPos.x, pos.y + tempRPos.y, rota);
                        allTilePos.Add(tempPos);
                    }
                }
                return allTilePos;
            }
        }

        private static List<TilePos> _ShortestFreePath(TileGrid grid, List<TilePos> curPath, TilePos end, List<TilePos> shortestPath, Dictionary<TilePos, int> visited, int width, int maxLen = 0) {
            if (curPath == null || curPath.Count == 0) return null;
            int moves = curPath.Count;
            if (maxLen > 0 && moves >= maxLen) return null;
            TilePos curTile = curPath.Last();
            int xDist = end.x - curTile.x;
            int yDist = end.y - curTile.y;
            int rot = curTile.Rotation;
            int endRot = end.Rotation;
            TilePos rotDist = ConvertRot(new TilePos(xDist, yDist, 0), rot);


            UnityEngine.MonoBehaviour.print("looking at: " + curTile.x + "#" + curTile.y + " - rot: " + rot);
            UnityEngine.MonoBehaviour.print("rotdist: " + rotDist.x + "#" + rotDist.y + " - rot: " + rot);

            List<int> rotations = new List<int>();
            //rotations.Add(rot);
            if (rotDist.x > 0 && rotDist.y <= width) { // Right (and maybe behind you)
                rotations.Add(1);
                rotations.Add(0);
                rotations.Add(-1);
            } else if (rotDist.x < 0 && rotDist.y <= 1) { // Left (and maybe behind you)
                rotations.Add(-1);
                rotations.Add(0);
                rotations.Add(1);
            } else if (rotDist.y > 0) { // Directly in front of you
                rotations.Add(0);
                rotations.Add(-1);
                rotations.Add(1);
            } else { // Directly behind you
                rotations.Add(1);
                rotations.Add(0);
                rotations.Add(-1);
            }

            foreach (int rRot in rotations) {
                TilePos newPos = null;
                bool widthCheck = true;
                if (rRot == 0) {
                    TilePos newRPos = ConvertRot(new TilePos(0, 1, rotation: 0), rot);
                    newPos = new TilePos(curTile.x + newRPos.x, curTile.y + newRPos.y, rot);
                    if (!IsInBounds(grid, newPos)) continue;
                    for (int i = 0; i < width; i++) {
                        TilePos tempRPos = ConvertRot(new TilePos(i, 0, rotation: 0), rot);
                        TilePos tempPos = new TilePos(newPos.x + tempRPos.x, newPos.y + tempRPos.y, rot);
                        if (!IsInBounds(grid, tempPos) || grid.GetTile(tempPos) != null) widthCheck = false;
                    }
                } else if (rRot == 1) {
                    TilePos newRPos = ConvertRot(new TilePos(0, width, rotation: 0), rot);
                    newPos = new TilePos(curTile.x + newRPos.x, curTile.y + newRPos.y, rot + rRot);
                    UnityEngine.MonoBehaviour.print("Rotate right? " + newPos.x + "#" + newPos.y + " - rot: " + (rot + rRot));
                    if (!IsInBounds(grid, newPos)) continue;
                    for (int j = 0; j < width; j++) {
                        for (int i = 0; i < width; i++) {
                            TilePos tempRPos = ConvertRot(new TilePos(i, j, rotation: 0), rot + rRot);
                            TilePos tempPos = new TilePos(newPos.x + tempRPos.x, newPos.y + tempRPos.y, rot + rRot);
                            if (!IsInBounds(grid, tempPos) || grid.GetTile(tempPos) != null) widthCheck = false;
                        }
                    }
                } else if (rRot == -1) {
                    TilePos newRPos = ConvertRot(new TilePos(width-1, 1, rotation: 0), rot);
                    newPos = new TilePos(curTile.x + newRPos.x, curTile.y + newRPos.y, rot + rRot);
                    UnityEngine.MonoBehaviour.print("Rotate left? " + newPos.x + "#" + newPos.y + " - rot: " + (rot + rRot));
                    if (!IsInBounds(grid, newPos)) continue;
                    for (int j = 0; j < width; j++) {
                        for (int i = 0; i < width; i++) {
                            TilePos tempRPos = ConvertRot(new TilePos(i, j, rotation: 0), rot + rRot);
                            TilePos tempPos = new TilePos(newPos.x + tempRPos.x, newPos.y + tempRPos.y, rot + rRot);
                            if (!IsInBounds(grid, tempPos) || grid.GetTile(tempPos) != null) widthCheck = false;
                        }
                    }
                }
                if (end.Equals(newPos) && GetRot(rot + rRot) == endRot) {
                    // end code here
                    if ((!visited.ContainsKey(end) || visited[end] > moves)) {
                        //curPath.Add(end);
                        shortestPath = new List<TilePos>(curPath);
                        //curPath.Remove(end);
                        return shortestPath;
                    }
                } else if (widthCheck == true) {
                    // this is a good target
                    if (!curPath.Contains(newPos) && (!visited.ContainsKey(newPos) || visited[newPos] > moves)) {
                        visited[newPos] = moves;
                        curPath.Add(newPos);
                        Thread.Sleep(250);
                        Tile t = new Tile(grid, newPos.x, newPos.y);
                        List<TilePos> newShortest = _ShortestFreePath(grid, curPath, end, null, visited, width, maxLen: maxLen);
                        if (newShortest != null && (shortestPath == null || newShortest.Count < shortestPath.Count)) shortestPath = newShortest;
                        t.Destroy();
                        curPath.Remove(newPos);
                    }
                }
            }
            return shortestPath;
        }

        private static int GetRot(int rot) {
            return (rot % 4 + 4) % 4;
        }

        private static TilePos ConvertRot(TilePos pos, int rot) {
            if (pos == null) return null;
            if (rot < 0 || rot > 3) rot = GetRot(rot);
            int x = pos.x;
            int y = pos.y;
            int newX = x, newY = y;
            switch (rot) {
                case 0: newX = x; newY = y; break;
                case 1: newX = -y; newY = x; break;
                case 2: newX = -x; newY = -y; break;
                case 3: newX = y; newY = -x; break;
            }
            return new TilePos(newX, newY, rot);
        }

        private static bool IsInBounds(TileGrid grid, TilePos pos) {
            if (pos == null || grid == null) return false;
            int x = pos.x, y = pos.y;
            if (x < 0 || x >= grid.GetGridSize().x) return false;
            if (y < 0 || y >= grid.GetGridSize().y) return false;
            return true;
        }

    }
}
