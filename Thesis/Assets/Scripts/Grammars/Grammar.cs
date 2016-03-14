using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class Grammar<T>
        where T : StructureModel {
        protected T source;
        public T Source {
            get {
                return source;
            }
            set {
                if (source != value) {
                    source = value;
                    iteration = 0;
                }
            }
        }

        protected List<Rule<T>> rules;
        Rule<T> selectedRule = null;

        protected Type transformerType;
        public IStructureTransformer<T> Transformer {
            get {
                if (transformerType != null) {
                    return (IStructureTransformer<T>)(Activator.CreateInstance(transformerType));
                } else return null;
            }
            set {
                if (value != null) {
                    transformerType = value.GetType();
                } else {
                    transformerType = null;
                }
            }
        }

        bool findAllRules;
        MethodInfo stopCondition = null;
        MethodInfo controlledRuleSelection = null;
        int iteration;
        bool noRuleFound;

        protected Random random;

        public Grammar(MethodInfo stopCondition = null, MethodInfo controlledRuleSelection = null, bool findAllRules = false) {
            this.stopCondition = stopCondition;
            this.controlledRuleSelection = controlledRuleSelection;
            this.findAllRules = findAllRules;
            iteration = 0;
            noRuleFound = false;
            random = new Random();
        }

        protected bool CheckStopCondition() { // TODO: Change to int?
            if (source == null) return true;
            if (stopCondition != null) {
                object[] parameters = new object[1];
                parameters[0] = this;
                bool result = (bool)stopCondition.Invoke(null, parameters);
                return result;
            } else {
                return noRuleFound;
            }
        }

        protected void SelectRule(bool useControlled = true) {
            if (source == null) return;
            selectedRule = null;
            int ruleIndex = -1;
            if (findAllRules) {
                foreach (Rule<T> rule in rules) {
                    rule.Find(source);
                }
            }
            if (controlledRuleSelection != null && useControlled) {
                object[] parameters = new object[1];
                parameters[0] = this;
                ruleIndex = (int)controlledRuleSelection.Invoke(null, parameters);
            } else {
                double currentProbability = 0.0;
                double maxProbability = 0.0;
                List<Rule<T>> tempRules = new List<Rule<T>>();
                List<double> probabilities = new List<double>();
                foreach (Rule<T> rule in rules) {
                    if (!findAllRules || rule.HasSelected()) {
                        tempRules.Add(rule);
                        double probability = rule.GetProbability();
                        maxProbability += probability;
                        probabilities.Add(probability);
                    }
                }
                while (tempRules.Count > 0 && ruleIndex == -1) {
                    currentProbability = random.NextDouble() * maxProbability;
                    int ruleToRemove = -1;
                    for (int i = 0; i < tempRules.Count; i++) {
                        if (currentProbability < probabilities[i]) {
                            if (!findAllRules) {
                                // Rule query may fail, so try this first.
                                if (tempRules[i].Find(source)) {
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
            }
            if (ruleIndex != -1 && ruleIndex < rules.Count) {
                selectedRule = rules[ruleIndex];
            } else {
                noRuleFound = true;
            }
        }

        public void Update() {
            SelectRule();
            if (!noRuleFound && selectedRule != null) {
                selectedRule.Apply(source);
            }
            bool stop = CheckStopCondition();
            if (stop) {
                // Transfer control to inter-grammar system
            }
            iteration++;
        }

        public void AddRule(Rule<T> rule) {
            rules.Add(rule);
        }

        public void RemoveRule(Rule<T> rule) {
            if (rules.Contains(rule)) {
                rules.Remove(rule);
            }
        }

        public List<Rule<T>> GetRules() {
            return new List<Rule<T>>(rules); // Return a copy of the rule list
        }
    }
}
