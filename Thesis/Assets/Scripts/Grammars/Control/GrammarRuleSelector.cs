using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class GrammarRuleSelector {
        private MethodInfo method;
        private object grammar;

        public GrammarRuleSelector(MethodInfo method, object grammar) {
            this.method = method;
            this.grammar = grammar;
        }

        public int Select(object rules) {
            // Check method signature
            if (method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 2) {
                object[] parameters = new object[2];
                parameters[0] = grammar;
                parameters[1] = rules;
                int result = (int)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static GrammarRuleSelector FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo condition = typeof(GrammarRuleSelector).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(int) && condition.GetParameters().Count() == 2) {
                return new GrammarRuleSelector(condition, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example rule selection methods are listed here
        // ********************************************************************************************************************

        // Default rule selection using probabilities
        public static int ProbabilityRuleSelection<T>(Grammar<T> grammar, List<Rule<T>> rules) where T : StructureModel {
            int ruleIndex = -1;
            Random random = new Random();
            List<Rule<T>> tempRules = new List<Rule<T>>(rules); // Make a copy of the rule list where we can change elements

            double currentProbability = 0.0;
            double maxProbability = 0.0;
            List<double> probabilities = new List<double>();
            foreach (Rule<T> rule in tempRules) {
                double probability = rule.GetProbability();
                maxProbability += probability;
                probabilities.Add(probability);
            }
            while (tempRules.Count > 0 && ruleIndex == -1) {
                currentProbability = random.NextDouble() * maxProbability;
                int ruleToRemove = -1;
                for (int i = 0; i < tempRules.Count; i++) {
                    if (currentProbability < probabilities[i]) {
                        if (!grammar.FindAllRules) {
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
