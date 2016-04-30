using Grammars.Graphs;
using Grammars.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class RuleAction : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        public RuleAction(MethodInfo method, object rule = null) : base(method) {
            this.rule = rule;
        }

        public void Execute() {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && method != null && method.ReturnType == typeof(void) && method.GetParameters().Count() == 1+argCount) {
                object[] parameters = new object[1+argCount];
                parameters[0] = rule;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                method.Invoke(null, parameters);
            }
        }

        public static RuleAction FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo method = typeof(RuleAction).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(void) && method.GetParameters().Count() >= 1) {
                return new RuleAction(method, rule);
            } else return null;
        }

        // Example rule actions are listed here
        public static void ConditionedAction<T>(Rule<T> rule, RuleCondition condition, RuleAction action) where T : StructureModel {
            if (condition == null || action == null) return;
            if (condition.Check()) {
                action.Execute();
            }
        }

        public static void TraverserNext<T>(Rule<T> rule, string traverser, string startSelector, string elementSelector, string connectionsSelector) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            Dictionary<string, string> stringParams = new Dictionary<string, string>();
            Dictionary<string, object> objectParams = new Dictionary<string, object>();
            if (startSelector != null && startSelector.Trim() != "") {
                List<AttributedElement> startEls = rule.GetElements(startSelector);
                if (startEls != null && startEls.Count > 0) {
                    objectParams.Add("start", startEls[0]);
                }
            }
            if (elementSelector != null && elementSelector.Trim() != "") {
                if (typeof(T) == typeof(Graph)) {
                    stringParams.Add("nodeSelector", elementSelector);
                } else if (typeof(T) == typeof(TileGrid)) {
                    stringParams.Add("tileSelector", elementSelector);
                }
            }
            if (connectionsSelector != null && connectionsSelector.Trim() != "") {
                if (typeof(T) == typeof(Graph)) {
                    stringParams.Add("edgeSelector", connectionsSelector);
                } else if (typeof(T) == typeof(TileGrid)) {
                    stringParams.Add("neighborSelector", connectionsSelector);
                }
            }

            if (grammar.GetListener(traverser) != null) {
                grammar.SendGrammarEvent("Next",
                    replyExpected: false,
                    source: grammar,
                    targets: new string[] { traverser },
                    stringParameters: stringParams,
                    objectParameters: objectParams);
            } else {
                grammar.SendGrammarEvent(traverser + ".Next",
                    replyExpected: false,
                    source: grammar,
                    targets: new string[] { "controller" },
                    stringParameters: stringParams,
                    objectParameters: objectParams);
            }
        }

        public static void RuleApply<T>(Rule<T> rule, string ruleName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            Rule<T> newRule = grammar.GetRule(ruleName);
            if (grammar != null && newRule != null && newRule.CheckCondition(overrideActive: true) && newRule.Find(grammar.Source)) {
                newRule.Apply(grammar.Source);
            }
        }

        public static void RuleFind<T>(Rule<T> rule, string ruleName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            Rule<T> newRule = grammar.GetRule(ruleName);
            if (grammar != null && newRule != null && newRule.CheckCondition(overrideActive: true)) {
                newRule.Find(grammar.Source);
            }
        }

        public static void RuleTransform<T>(Rule<T> rule, string ruleName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            Rule<T> newRule = grammar.GetRule(ruleName);
            if (grammar != null && newRule != null && newRule.HasSelected()) {
                newRule.Apply(grammar.Source);
            }
        }

        public static void SetConditionResult<T>(Rule<T> rule, string attName, RuleCondition cond) where T : StructureModel {
            if (cond == null || attName == null || attName.Trim() == "") return;
            bool success = cond.Check();
            rule.SetAttribute(attName, success.ToString().ToLowerInvariant());
        }

        public static void SetAttribute<T>(Rule<T> rule, string selector, string attName, string value) where T : StructureModel {
            if (attName == null || attName.Trim() == "" || value == null) return;
            List<AttributedElement> els = rule.GetElements(selector);
            foreach (AttributedElement el in els) {
                el.SetAttribute(attName, value);
            }
        }

        public static void CopyAttribute<T>(Rule<T> rule, string sel1, string att1, string sel2, string att2) where T : StructureModel {
            if (att1 == null || att1.Trim() == "" || att2 == null || att2.Trim() == "") return;
            List<AttributedElement> els1 = rule.GetElements(sel1);
            List<AttributedElement> els2 = rule.GetElements(sel2);
            if (els1.Count > 0 && els2.Count > 0) {
                object oatt = null;
                string satt = null;
                foreach (AttributedElement el1 in els1) {
                    object tempoatt = null;
                    string tempsatt = null;
                    tempoatt = el1.GetObjectAttribute(att1);
                    tempsatt = el1.GetAttribute(att1);
                    if (tempoatt != null) {
                        oatt = tempoatt;
                    } else if (tempsatt != null) {
                        satt = tempsatt;
                    }
                }
                foreach (AttributedElement el2 in els2) {
                    if (oatt != null) {
                        el2.SetObjectAttribute(att2, oatt);
                    }
                    if (satt != null) {
                        el2.SetAttribute(att2, satt);
                    }
                }
            }
        }

        public static void CreatePath<T>(Rule<T> rule, string startAtt, string endAtt, string startAtt2, string endAtt2, int width, string attName, string attValue) where T : StructureModel {
            try {
                if (startAtt == null || endAtt == null || startAtt.Trim() == "" || endAtt.Trim() == "") return;
                if (startAtt2 == null || endAtt2 == null || startAtt2.Trim() == "" || endAtt2.Trim() == "") return;
                if (typeof(T) != typeof(TileGrid)) return;
                object sO = rule.GetObjectAttribute(startAtt);
                object eO = rule.GetObjectAttribute(endAtt);
                object sO2 = rule.GetObjectAttribute(startAtt2);
                object eO2 = rule.GetObjectAttribute(endAtt2);
                if (sO == null || eO == null || sO.GetType() != typeof(Tile) || eO.GetType() != typeof(Tile)) return;
                if (sO2 == null || eO2 == null || sO2.GetType() != typeof(Tile) || eO2.GetType() != typeof(Tile)) return;
                Tile start = (Tile)sO;
                Tile end = (Tile)eO;
                Tile start2 = (Tile)sO2;
                Tile end2 = (Tile)eO2;
                TilePos startPos = start.GetIndices();
                TilePos endPos = end.GetIndices();
                TilePos startPos2 = start2.GetIndices();
                TilePos endPos2 = end2.GetIndices();
                int startXDiff = startPos2.x - startPos.x;
                int startYDiff = startPos2.y - startPos.y;
                int endXDiff = endPos2.x - endPos.x;
                int endYDiff = endPos2.y - endPos.y;
                int startRot = 0, endRot = 0;
                switch (startXDiff) {
                    case 0:
                        switch (startYDiff) {
                            case 1: startRot = 3; break;
                            case -1: startRot = 1; break;
                        }
                        break;
                    case 1:
                        startRot = 0; break;
                    case -1:
                        startRot = 2; break;
                }
                switch (endXDiff) {
                    case 0:
                        switch (endYDiff) {
                            case 1: endRot = 3; break;
                            case -1: endRot = 1; break;
                        }
                        break;
                    case 1:
                        endRot = 0; break;
                    case -1:
                        endRot = 2; break;
                }
                startPos.Rotation = startRot;
                endPos.Rotation = endRot;

                TileGrid grid = (TileGrid)start.Container;
                List<TilePos> poss = Tiles.Algorithms.ShortestFreePath(grid, startPos, endPos, width, returnAll: true);
                if (poss != null) {
                    foreach (TilePos pos in poss) {
                        Tile t = new Tile(grid, pos.x, pos.y);
                        t.SetAttribute(attName, attValue);
                    }
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogError(e.Message + e.StackTrace);
            }
        }
    }
}
