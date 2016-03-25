using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class GrammarRuleSelector : MethodCaller {
        private object grammar;
        public object Grammar {
            get { return grammar; }
            set { grammar = value; }
        }

        public GrammarRuleSelector(MethodInfo method, object grammar = null) : base(method) {
            this.grammar = grammar;
        }

        public int Select(object rules) {
            // Check method signature
            int argCount = arguments.Count;
            if (grammar != null && method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 2+argCount) {
                object[] parameters = new object[2+argCount];
                parameters[0] = grammar;
                parameters[1] = rules;
                for (int i = 0; i < argCount; i++) {
                    parameters[i+2] = arguments[i];
                }
                //int result = (int)method.Invoke(null, BindingFlags.Public | BindingFlags.Static | BindingFlags.ExactBinding, null, parameters, System.Globalization.CultureInfo.InvariantCulture);
                int result = (int)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static GrammarRuleSelector FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo method = typeof(GrammarRuleSelector).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(int) && method.GetParameters().Count() >= 2) {
                return new GrammarRuleSelector(method, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example rule selection methods are listed here
        // ********************************************************************************************************************

        // Default rule selection using probabilities
        public static int ProbabilityRuleSelection<T>(Grammar<T> grammar, List<Rule<T>> rules) where T : StructureModel {
            int ruleIndex = -1;
            Random random = new Random();
            List<Rule<T>> tempRules = new List<Rule<T>>(); // Make a temporary rule list. Impossible rules won't be added to this.

            double currentProbability = 0.0;
            double maxProbability = 0.0;
            List<double> probabilities = new List<double>();
            foreach (Rule<T> rule in rules) {
                double probability = rule.GetProbability();
                if (probability > 0) {
                    maxProbability += probability;
                    probabilities.Add(probability);
                    tempRules.Add(rule);
                }
            }
            while (tempRules.Count > 0 && ruleIndex == -1) {
                currentProbability = random.NextDouble() * maxProbability;
                int ruleToRemove = -1;
                for (int i = 0; i < tempRules.Count; i++) {
                    if (currentProbability < probabilities[i]) {
                        if (!tempRules[i].HasSelected()) {
                            // Rule query may fail, so try this first.
                            if (tempRules[i].Find(grammar.Source)) {
                                ruleIndex = i;
                            } else ruleToRemove = i;
                        } else ruleIndex = i;
                        break;
                    } else {
                        currentProbability -= probabilities[i];
                    }
                }
                if (ruleIndex != -1) { // Get index of rule in complete rule list
                    Rule<T> rule = tempRules[ruleIndex];
                    ruleIndex = rules.IndexOf(rule);
                }
                if (ruleToRemove != -1) {
                    maxProbability -= probabilities[ruleToRemove];
                    probabilities.RemoveAt(ruleToRemove);
                    tempRules.RemoveAt(ruleToRemove);
                }
            }
            return ruleIndex;
        }
    }
}
