using System;
using System.Collections.Generic;

namespace Grammars {
    public class Constraint<T> where T : StructureModel {
        protected Grammar<T> grammar;
        protected GrammarCondition condition;
        public GrammarCondition Condition {
            get { return condition; }
            set { condition = value; }
        }

        protected GrammarProbability probabilityCalculator;
        public GrammarProbability ProbabilityCalculator {
            get { return probabilityCalculator; }
            set { probabilityCalculator = value; }
        }

        protected double probability;
        public double Probability {
            get { return GetProbability(false); }
        }
        protected bool valid;
        public bool Valid {
            get { return valid; }
        }

        protected List<Rule<T>> rules;
        protected bool active;
        public bool Active {
            get { return active; }
            set { active = value; }
        }

        protected string name = null;
        public string Name {
            get { return name; }
        }

        // Rule selection
        protected GrammarRuleSelector selector = null;
        public GrammarRuleSelector Selector {
            get {
                if (selector == null) return grammar.RuleSelector;
                return selector;
            }
            set {
                selector = value;
            }
        }
        protected bool findFirst = false;
        public bool FindFirst {
            get {
                return findFirst;
            }
            set {
                findFirst = value;
            }
        }

        public Constraint(Grammar<T> grammar, string name, bool active=true) {
            this.grammar = grammar;
            grammar.AddConstraint(name, this);
            condition = null;
            probabilityCalculator = null;
            rules = new List<Rule<T>>();
            this.active = active;
            this.name = name;
            probability = 1;
            valid = false;
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

        /// <summary>
        /// Returns the probability that rules from this constraint should be applied
        /// </summary>
        /// <param name="recalculate"></param>
        /// <returns></returns>
        public double GetProbability(bool recalculate=true) {
            if (active) {
                if (recalculate) {
                    if (condition != null) valid = condition.Check();
                    if (valid) {
                        probability = 0;
                    } else {
                        if (probabilityCalculator != null) {
                            probability = probabilityCalculator.Calculate();
                        } else probability = 1;
                    }
                }
                return probability;
            } else return 0;
        }
    }
}
