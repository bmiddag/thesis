using Grammars.Graphs;
using Grammars.Tiles;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class RuleCondition : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        public RuleCondition(MethodInfo method, object rule = null) : base(method) {
            this.rule = rule;
        }

        public bool Check() {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1 + argCount) {
                object[] parameters = new object[1 + argCount];
                parameters[0] = rule;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return true;
            }
        }

        public static RuleCondition FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo method = typeof(RuleCondition).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(bool) && method.GetParameters().Count() >= 1) {
                return new RuleCondition(method, rule);
            } else return null;
        }

        // Example rule conditions are listed here

        // AND-result of 2 conditions
        public static bool And<T>(Rule<T> rule, RuleCondition cond1, RuleCondition cond2) where T : StructureModel {
            return (cond1.Check() && cond2.Check());
        }

        // OR-result of 2 conditions
        public static bool Or<T>(Rule<T> rule, RuleCondition cond1, RuleCondition cond2) where T : StructureModel {
            return (cond1.Check() || cond2.Check());
        }

        // NOT-result of a condition
        public static bool Not<T>(Rule<T> rule, RuleCondition cond) where T : StructureModel {
            return !cond.Check();
        }

        public static bool False<T>(Rule<T> rule) where T : StructureModel {
            return false;
        }

        public static bool True<T>(Rule<T> rule) where T : StructureModel {
            return true;
        }

        public static bool TraverserMatch<T>(Rule<T> rule, string traverser, string queryName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
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
            if (replies != null && replies.Count > 0 && replies[0] != null) {
                object match = replies[0];
                rule.SetObjectAttribute(traverser, match);
                if (typeof(IDictionary<string, AttributedElement>).IsAssignableFrom(match.GetType())) {
                    IDictionary<string, AttributedElement> matchDict = (IDictionary<string, AttributedElement>)match;
                    foreach (KeyValuePair<string, AttributedElement> pair in matchDict) {
                        rule.SetObjectAttribute(traverser + "_" + pair.Key, pair.Value);
                    }
                }
                return true;
            } else {
                rule.RemoveAttribute(traverser);
                //UnityEngine.MonoBehaviour.print("No match for: " + queryName);
                return false;
            }
        }

        public static bool TraverserFreeMatch<T>(Rule<T> rule, string traverser, string queryName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("query", queryName);
            parameters.Add("noCurrent", "true");
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
            if (replies != null && replies.Count > 0 && replies[0] != null) {
                object match = replies[0];
                rule.SetObjectAttribute(traverser, match);
                if (typeof(IDictionary<string, AttributedElement>).IsAssignableFrom(match.GetType())) {
                    IDictionary<string, AttributedElement> matchDict = (IDictionary<string, AttributedElement>)match;
                    foreach (KeyValuePair<string, AttributedElement> pair in matchDict) {
                        rule.SetObjectAttribute(traverser + "_" + pair.Key, pair.Value);
                    }
                }
                return true;
            } else {
                rule.RemoveAttribute(traverser);
                //UnityEngine.MonoBehaviour.print("No match for: " + queryName);
                return false;
            }
        }

        public static bool TaskMatch<T>(Rule<T> rule, string taskName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            return (grammar.CurrentTask != null && grammar.CurrentTask.Action == taskName);
        }

        public static bool RuleFound<T>(Rule<T> rule, string ruleName) where T : StructureModel {
            Grammar<T> grammar = rule.Grammar;
            Rule<T> newRule = grammar.GetRule(ruleName);
            if (grammar != null && newRule != null && newRule.HasSelected()) {
                return true;
            } else return false;
        }

        public static bool CheckAttribute<T>(Rule<T> rule, string selector, string attName, string value) where T : StructureModel {
            List<AttributedElement> els = rule.GetElements(selector);
            foreach (AttributedElement el in els) {
                string att = el.GetAttribute(attName);
                if (att != value) return false;
            }
            return true;
        }

        public static bool AggregateOperation<T>(Rule<T> rule, string selector, string attrName, string aggOperation, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(rule, selector);
            double result;
            if (attrName == null || attrName.Trim() == "") {
                result = elements.Count;
            } else {
                result = StringEvaluator.AggregateAttribute(aggOperation, elements, attrName);
            }
            return StringEvaluator.Compare(cmpOperation, result, number);
        }

        public static bool CountElements<T>(Rule<T> rule, string selector, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(rule, selector);
            return StringEvaluator.Compare(cmpOperation, elements.Count, number);
        }

        public static bool SumAttribute<T>(Rule<T> rule, string selector, string attrName, string cmpOperation, double number) where T : StructureModel {
            return AggregateOperation(rule, selector, attrName, "SUM", cmpOperation, number);
        }
    }
}
