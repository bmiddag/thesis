using System;
using System.Collections.Generic;

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
        
        /// <summary>
        /// If all rules sould execute Find (e.g. before checking probabilities), this should be true.
        /// Otherwise, Find should be executed when selecting the rule. If it fails, the rule should not be selected.
        /// </summary>
        protected bool findAllRules;
        public bool FindAllRules {
            get {
                return findAllRules;
            }
        }

        GrammarStopCondition stopCondition = null;
        GrammarRuleSelector controlledRuleSelection = null;
        int iteration;

        /// <summary>
        /// If no rule was found during this iteration, this should be marked true.
        /// </summary>
        protected bool noRuleFound;
        public bool NoRuleFound {
            get {
                return noRuleFound;
            }
        }

        public Grammar(GrammarStopCondition stopCondition = null, GrammarRuleSelector controlledRuleSelection = null, bool findAllRules = false) {
            this.stopCondition = stopCondition;
            this.controlledRuleSelection = controlledRuleSelection;
            this.findAllRules = findAllRules;
            iteration = 0;
            noRuleFound = false;
        }

        protected bool CheckStopCondition() { // TODO: Change to int?
            if (source == null) return true;
            if (stopCondition != null) {
                int result = stopCondition.Check();
                return result != 0;
            } else {
                return noRuleFound;
            }
        }

        protected void SelectRule(bool useControlled = true) {
            if (source == null) return;
            selectedRule = null;
            int ruleIndex = -1;
            int tempRuleIndex = -1;
            if (findAllRules) {
                foreach (Rule<T> rule in rules) {
                    rule.Find(source);
                }
            }
            // Make a copy of the rule list without the ones that are certain to fail. 
            List<Rule<T>> tempRules = new List<Rule<T>>();
            foreach (Rule<T> rule in rules) {
                if (!findAllRules || rule.HasSelected()) {
                    tempRules.Add(rule);
                }
            }
            // Controlled rule selection
            if (controlledRuleSelection != null && useControlled) {
                tempRuleIndex = controlledRuleSelection.Select(new List<Rule<T>>(tempRules));
                if (tempRuleIndex != -1) { // Get index of rule in complete rule list
                    Rule<T> rule = tempRules[tempRuleIndex];
                    ruleIndex = rules.IndexOf(rule);
                    if (ruleIndex < 0 || ruleIndex >= rules.Count) ruleIndex = -1;
                }
            }
            if (ruleIndex == -1) {
                // Default rule selection using probabilities. Is also used as fallback in case controlled rule selection doesn't work.
                GrammarRuleSelector defaultRuleSelection = GrammarRuleSelector.FromName("ProbabilityRuleSelection", this);
                tempRuleIndex = defaultRuleSelection.Select(new List<Rule<T>>(tempRules));
                if (tempRuleIndex != -1) { // Get index of rule in complete rule list
                    Rule<T> rule = tempRules[tempRuleIndex];
                    ruleIndex = rules.IndexOf(rule);
                    if (ruleIndex < 0 || ruleIndex >= rules.Count) ruleIndex = -1;
                }
            }
            
            if (ruleIndex != -1) {
                selectedRule = rules[ruleIndex];
            } else noRuleFound = true;
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
