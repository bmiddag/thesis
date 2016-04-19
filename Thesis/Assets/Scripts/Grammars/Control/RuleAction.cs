using Grammars.Graphs;
using Grammars.Tiles;
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
    }
}
