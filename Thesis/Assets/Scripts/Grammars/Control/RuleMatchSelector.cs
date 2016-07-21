using Grammars.Graphs;
using Grammars.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class RuleMatchSelector : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        public RuleMatchSelector(MethodInfo method, object rule = null) : base(method) {
            this.rule = rule;
        }

        public int Select(object matches) {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 2+argCount) {
                object[] parameters = new object[2+argCount];
                parameters[0] = rule;
                parameters[1] = matches;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 2] = arguments[i];
                }
                int result = (int)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static RuleMatchSelector FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo method = typeof(RuleMatchSelector).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(int) && method.GetParameters().Count() >= 2) {
                return new RuleMatchSelector(method, rule);
            } else return null;
        }

        public static int PrioritizeTraverserGraphMatch<T>(Rule<T> rule, List<Dictionary<Node, Node>> matches) where T : StructureModel {
            int index = -1;
            if (rule != null && rule.Grammar.CurrentTask.Action == "GenerateNext") {
                // Quick fix for prioritizing current traverser position in matches
                List<Dictionary<Node, Node>> prioritizedMatches = new List<Dictionary<Node, Node>>();

                object currEl = rule.Grammar.CurrentTask.GetObjectAttribute("currentElement");
                if (currEl.GetType() == typeof(Edge)) {
                    Edge currEdge = (Edge)currEl;
                    foreach (Dictionary<Node, Node> match in matches) {
                        if (match.Keys.Where(n => (n == currEdge.GetNode1() || n == currEdge.GetNode2())).Count() > 0) {
                            prioritizedMatches.Add(match);
                        }
                    }
                } else if (currEl.GetType() == typeof(Node)) {
                    Node currNode = (Node)currEl;
                    foreach (Dictionary<Node, Node> match in matches) {
                        if (match.Keys.Where(n => n == currNode).Count() > 0) {
                            prioritizedMatches.Add(match);
                        }
                    }
                }
                if (prioritizedMatches.Count > 0) {
                    Random rnd = new Random();
                    int prioritizedIndex = rnd.Next(prioritizedMatches.Count);
                    index = matches.IndexOf(prioritizedMatches[prioritizedIndex]);
                    if (rule != null) rule.SetAttribute("traverserMatch", "true");
                } else {
                    Random rnd = new Random();
                    index = rnd.Next(matches.Count);
                    if (rule != null) rule.SetAttribute("traverserMatch", "false");
                }
            } else {
                Random rnd = new Random();
                index = rnd.Next(matches.Count);
                if (rule != null) rule.SetAttribute("traverserMatch", "false");
            }
            return index;
        }

        // Example match selection methods are listed here
        public static int MinimizeTileDistance<T>(Rule<T> rule, List<TilePos> matches, string tileSelector) where T : StructureModel {
            /*UnityEngine.MonoBehaviour.print("REACHED THIS POINT");
            UnityEngine.MonoBehaviour.print("MATCH COUNT: " + matches.Count);
            UnityEngine.MonoBehaviour.print("SELECTOR: " + tileSelector);
            if (tileSelector.Contains("from [grammar.source") &&
                (tileSelector.Contains("rule") || (tileSelector.IndexOf("grammar") != tileSelector.LastIndexOf("grammar")))) {
                foreach (AttributedElement sourceEl in rule.Grammar.GetElements()) {
                    sourceEl.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                    sourceEl.SetObjectAttribute("rule", rule, notify: false);
                }
            }*/
            List<AttributedElement> els = StringEvaluator.SelectElements(rule, tileSelector);
            /*if (tileSelector.Contains("from [grammar.source") &&
                (tileSelector.Contains("rule") || (tileSelector.IndexOf("grammar") != tileSelector.LastIndexOf("grammar")))) {
                foreach (AttributedElement sourceEl in rule.Grammar.GetElements()) {
                    sourceEl.RemoveObjectAttribute("grammar", notify: false);
                    sourceEl.RemoveObjectAttribute("rule", notify: false);
                }
            }
            UnityEngine.MonoBehaviour.print("SELECTED ELS: " + els.Count);*/
            //List<AttributedElement> els = rule.GetElements(tileSelector);
            List<TilePos> tilesPos = els.Where(e => e.GetType() == typeof(Tile)).Select(e => ((Tile)e).GetIndices()).ToList();
            if (tilesPos.Count > 0) {
                int meanX = (int)Math.Round(tilesPos.Select(t => t.x).Average());
                int meanY = (int)Math.Round(tilesPos.Select(t => t.y).Average());
                TilePos meanPos = new TilePos(meanX, meanY);
                int minDist = int.MaxValue;
                int ind = 0;
                for (int i = 0; i < matches.Count; i++) {
                    TilePos match = matches[i];
                    int dist = Tiles.Algorithms.Distance(match, meanPos);
                    if (dist < minDist) {
                        ind = i;
                        minDist = dist;
                    }
                }
                return ind;
            } else {
                Random rnd = new Random();
                int ind = rnd.Next(matches.Count);
                return ind;
            }
        }
    }
}
