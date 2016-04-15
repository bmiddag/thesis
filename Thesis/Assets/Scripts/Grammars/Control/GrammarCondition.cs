using Grammars.Graphs;
using Grammars.Tiles;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class GrammarCondition : MethodCaller {
        private object grammar;
        public object Grammar {
            get { return grammar; }
            set { grammar = value; }
        }

        public GrammarCondition(MethodInfo method = null, object grammar = null) : base(method) {
            this.grammar = grammar;
        }

        public bool Check() {
            // Check method signature
            int argCount = arguments.Count;
            if (grammar != null && method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1 + argCount) {
                object[] parameters = new object[1 + argCount];
                parameters[0] = grammar;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return false;
            }
        }

        public static GrammarCondition FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo method = typeof(GrammarCondition).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(bool) && method.GetParameters().Count() >= 1) {
                return new GrammarCondition(method, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example stop condition / constraint condition methods are listed here
        // ********************************************************************************************************************

        // Default stop condition (this is an example - not referred to in the actual grammar code)
        public static bool NoRuleFound<T>(Grammar<T> grammar) where T : StructureModel {
            if (grammar.NoRuleFound) {
                return true; // Stop (still undefined)
            } else {
                return false; // Continue
            }
        }

        // AND-result of 2 grammar conditions
        public static bool And<T>(Grammar<T> grammar, GrammarCondition cond1, GrammarCondition cond2) where T : StructureModel {
            return (cond1.Check() && cond2.Check());
        }

        // OR-result of 2 grammar conditions
        public static bool Or<T>(Grammar<T> grammar, GrammarCondition cond1, GrammarCondition cond2) where T : StructureModel {
            return (cond1.Check() || cond2.Check());
        }

        // NOT-result of a grammar condition
        public static bool Not<T>(Grammar<T> grammar, GrammarCondition cond) where T : StructureModel {
            return !cond.Check();
        }

        /// <summary>
        /// Count elements with a specific attribute or perform any aggregate operation on them and compare it to a number
        /// </summary>
        /// <typeparam name="T">The structure the grammar generates.</typeparam>
        /// <param name="grammar">The grammar</param>
        /// <param name="selector">The selector string (Dynamic LINQ query)</param>
        /// <param name="attrName">Attribute name. If left empty, all elements will be counted.</param>
        /// <param name="aggOperation">The aggregate operation to perform (e.g. COUNT, SUM, AVG, etc.)</param>
        /// <param name="cmpOperation">A string representing the operation to compare the element count with the number, e.g. "==".</param>
        /// <param name="number">The number to compare the element count to.</param>
        /// <returns></returns>
        public static bool AggregateOperation<T>(Grammar<T> grammar, string selector, string attrName, string aggOperation, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(grammar.Source, selector);
            double result;
            if (attrName == null || attrName.Trim() == "") {
                result = elements.Count;
            } else {
                result = StringEvaluator.AggregateAttribute(aggOperation, elements, attrName);
            }
            return StringEvaluator.Compare(cmpOperation, result, number);
        }

        public static bool CountElements<T>(Grammar<T> grammar, string selector, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(grammar.Source, selector);
            return StringEvaluator.Compare(cmpOperation, elements.Count, number);
        }

        public static bool SumAttribute<T>(Grammar<T> grammar, string selector, string attrName, string cmpOperation, double number) where T : StructureModel {
            return AggregateOperation(grammar, selector, attrName, "SUM", cmpOperation, number);
        }

        public static bool TraverserMatch<T>(Grammar<T> grammar, string traverser, string queryName) where T : StructureModel {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("query", queryName);
            List<object> replies;

            if (grammar.GetListener(traverser) != null) {
                replies = grammar.SendGrammarEvent("CheckMatch",
                    replyExpected: true,
                    source: grammar,
                    targets: new string[] { traverser },
                    stringParameters: parameters);
            } else {
                replies = grammar.SendGrammarEvent(traverser + ".CheckMatch",
                    replyExpected: true,
                    source: grammar,
                    targets: new string[] { "controller" },
                    stringParameters: parameters);
            }
            if (replies == null || replies.Count == 0) {
                // Error
                return false;
            } else if (replies[0] != null) {
                return true;
            } else return false;
        }

        public static bool TaskMatch<T>(Grammar<T> grammar, string taskName) where T : StructureModel {
            return (grammar.CurrentTask != null && grammar.CurrentTask.Action == taskName);
        }

        public static bool CountElementsGrouped<T>(Grammar<T> grammar, string groupName, string groupSelector, string inGroupSelector, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(grammar.Source, groupSelector);
            HashSet<HashSet<AttributedElement>> groups = new HashSet<HashSet<AttributedElement>>();
            if (elements != null && elements.Count > 0) {
                for(int i=0; i<elements.Count; i++) {
                    AttributedElement el = elements[i];
                    HashSet<HashSet<AttributedElement>> adjacentGroups = new HashSet<HashSet<AttributedElement>>();
                    // Check if already in group
                    bool groupFound = false;
                    foreach(HashSet<AttributedElement> group in groups) {
                        if (group.Contains(el)) {
                            groupFound = true;
                            break;
                        } else {
                            foreach (AttributedElement el2 in group) {
                                if (el2.GetType() == typeof(Node) && el.GetType() == typeof(Node)) {
                                    if (((Node)el2).GetEdges().Keys.Contains((Node)el)) {
                                        adjacentGroups.Add(group);
                                        break;
                                    }
                                } else if (el2.GetType() == typeof(Tile) && el.GetType() == typeof(Tile)) {
                                    if (((Tile)el2).GetNeighbors().Values.Contains((Tile)el)) {
                                        adjacentGroups.Add(group);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!groupFound) {
                        HashSet<AttributedElement> newGroup = new HashSet<AttributedElement>();
                        newGroup.Add(el);
                        if (adjacentGroups.Count > 0) {
                            // merge this one with all adjacent groups
                            foreach (HashSet<AttributedElement> group in adjacentGroups) {
                                groups.Remove(group);
                                newGroup.UnionWith(group);
                            }
                        }
                        groups.Add(newGroup);
                    }
                }
            }
            foreach (HashSet<AttributedElement> group in groups) {
                int count = group.Count;
                if (inGroupSelector != null && inGroupSelector.Trim() != "") {
                    List<AttributedElement> selected = StringEvaluator.SelectElementsFromList(new List<AttributedElement>(group), inGroupSelector);
                    if (selected != null) count = selected.Count;
                }
                bool countOkay = StringEvaluator.Compare(cmpOperation, count, number);
                if (!countOkay) {
                    grammar.SetObjectAttribute(groupName, group);
                    return false;
                }
            }

            return true;
        }
    }
}