using System;
using System.Collections.Generic;

namespace Grammars {
    public class Constraint<T> where T : StructureModel {
        protected Grammar<T> grammar;
        protected List<GrammarCondition> conditions;
        protected List<Rule<T>> rules;
        protected bool active;
        public bool Active {
            get {
                return active;
            }
            set {
                active = value;
            }
        }

        // Rule selection
        protected GrammarRuleSelector selector;
        public GrammarRuleSelector Selector {
            get {
                return selector;
            }
            set {
                selector = value;
            }
        }
        protected bool findFirst;
        public bool FindFirst {
            get {
                return findFirst;
            }
            set {
                findFirst = value;
            }
        }

        public Constraint(Grammar<T> grammar, bool active=true) {
            this.grammar = grammar;
            grammar.AddConstraint(this);
            conditions = new List<GrammarCondition>();
            rules = new List<Rule<T>>();
            this.active = active;
            //failedConditions = new List<int>();
        }

        public void AddCondition(GrammarCondition cond) {
            conditions.Add(cond);
        }

        public void RemoveCondition(GrammarCondition cond) {
            if (conditions.Contains(cond)) {
                conditions.Remove(cond);
            }
        }

        public List<GrammarCondition> GetConditions() {
            return new List<GrammarCondition>(conditions); // Return a copy of the conditions
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

        public bool Check() {
            if (active) {
                //failedConditions.Clear();
                bool failed = false;
                for (int i = 0; i < conditions.Count; i++) {
                    if (!conditions[i].Check()) {
                        failed = true;
                        //failedConditions.Add(i);
                    }
                }
                //if (failedConditions.Count > 0) {
                if (failed) {
                    return false;
                } else return true;
            } else return true;
        }
    }
}
