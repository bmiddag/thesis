using System;
using System.Collections.Generic;

namespace Grammars {
    public class Constraint<T> : AttributedElement
        where T : StructureModel {
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

        protected Dictionary<string, Rule<T>> rules;
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
            set { selector = value; }
        }
        protected bool findFirst = false;
        public bool FindFirst {
            get { return findFirst; }
            set { findFirst = value; }
        }

        public override IElementContainer Container {
            get { return grammar; }
        }

        public override string LinkType {
            get { return "constraint"; }
            set { }
        }

        public Constraint(Grammar<T> grammar, string name, bool active=true) {
            this.grammar = grammar;
            grammar.AddConstraint(name, this);
            condition = null;
            probabilityCalculator = null;
            rules = new Dictionary<string, Rule<T>>();
            this.active = active;
            this.name = name;
            probability = 1;
            valid = false;
        }

        public void AddRule(Rule<T> rule) {
            rules.Add(rule.Name, rule);
        }

        public void RemoveRule(Rule<T> rule) {
            if (rules.ContainsKey(rule.Name)) {
                rules.Remove(rule.Name);
            }
        }

        public Dictionary<string, Rule<T>> GetRules() {
            return new Dictionary<string, Rule<T>>(rules); // Return a copy of the rule list
        }

        public Rule<T> GetRule(string key) {
            if (rules.ContainsKey(key)) {
                return rules[key];
            } else return null;
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

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null) {
                switch (key) {
                    case "_probability":
                        result = GetProbability(false).ToString(); break;
                    case "_probability_dynamic":
                        result = GetProbability(true).ToString(); break;
                    case "_valid":
                        result = Valid.ToString(); break;
                    case "_active":
                        result = active.ToString(); break;
                    case "_name":
                        result = Name; break;
                }
            }
            return result;
        }

        public override List<AttributedElement> GetElements(string specifier = null) {
            IElementContainer subcontainer = null;
            string passSpecifier = specifier;
            /*if (specifier == null || specifier.Trim() == "") {
                subcontainer = grammar;
                passSpecifier = null;
            }*/
            if (specifier != null && specifier.Contains(".")) {
                string subcontainerStr = specifier.Substring(0, specifier.IndexOf("."));
                if (rules.ContainsKey(subcontainerStr)) {
                    subcontainer = rules[subcontainerStr];
                } else {
                    switch (subcontainerStr) {
                        case "task":
                            subcontainer = grammar.CurrentTask; break;
                        case "source":
                        case "grammar":
                            subcontainer = grammar; break;
                    }
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                List<AttributedElement> attrList = new List<AttributedElement>();
                if (rules.ContainsKey(specifier)) {
                    attrList.Add(rules[specifier]);
                } else {
                    switch (specifier) {
                        case "task":
                            if (grammar.CurrentTask != null) return grammar.CurrentTask.GetElements(); break;
                        case "task_structure":
                            if (grammar.CurrentTask != null) {
                                attrList.Add(grammar.CurrentTask);
                                return attrList;
                            }
                            break;
                        case "source":
                            if (grammar.Source != null) return grammar.Source.GetElements(); break;
                        case "source_structure":
                            if (grammar.Source != null) {
                                attrList.Add(grammar.Source);
                                return attrList;
                            }
                            break;
                        case "grammar":
                            attrList.Add(grammar);
                            return attrList;
                    }
                }
                return base.GetElements(specifier);
            }
        }

    }
}
