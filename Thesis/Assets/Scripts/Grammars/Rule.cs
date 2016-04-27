using System;
using System.Collections.Generic;

namespace Grammars {
    public class Rule<T> : AttributedElement
        where T : StructureModel {
        public override string LinkType {
            get { return "rule"; }
            set { }
        }

        protected Grammar<T> grammar;
        public Grammar<T> Grammar {
            get { return grammar; }
        }

        protected T query;
        public T Query {
            get { return query; }
            set { query = value; }
        }

        protected T target;
        public T Target {
            get { return target; }
            set { target = value; }
        }

        protected IStructureTransformer<T> transformer = null;
        public IStructureTransformer<T> Transformer {
            get { return transformer; }
            set {
                if (transformer != value) {
                    if (transformer != null) {
                        transformer.Destroy();
                    }
                    transformer = value;
                }
            }
        }

        protected double probability;
        protected RuleCondition condition = null;
        public RuleCondition Condition {
            get { return condition; }
            set { condition = value; }
        }

        protected RuleProbability dynamicProbability = null;
        public RuleProbability DynamicProbability {
            get { return dynamicProbability; }
            set { dynamicProbability = value; }
        }
        protected RuleMatchSelector matchSelector = null;
        public RuleMatchSelector MatchSelector {
            get { return matchSelector; }
            set { matchSelector = value; }
        }

        protected List<RuleAction> actions = null;
        public List<RuleAction> Actions {
            get { return new List<RuleAction>(actions); }
        }

        protected bool hasSelected;
        protected bool active;

        protected int priority = 0;
        public int Priority {
            get { return priority; }
            set { priority = value; }
        }

        protected string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        public override IElementContainer Container {
            get { return Grammar; }
        }

        public Rule(Grammar<T> grammar, string name, double probability, int priority = 0, bool active = true, T query = null, T target = null,
            RuleCondition condition = null, RuleProbability dynamicProbability = null, RuleMatchSelector matchSelector = null) {
            this.grammar = grammar;
            this.name = name;
            this.query = query;
            this.target = target;
            this.probability = probability;
            this.condition = condition;
            this.dynamicProbability = dynamicProbability;
            this.matchSelector = matchSelector;
            this.active = active;
            this.priority = priority;
            actions = new List<RuleAction>();
            hasSelected = false;
        }

        public bool CheckCondition(bool overrideActive=false) {
            if (!active && !overrideActive) return false;
            if (condition != null) {
                return condition.Check();
            } else {
                return true;
            }
        }

        public double GetProbability(bool useDynamic = true, bool overrideActive = false) {
            if (!active &&!overrideActive) return 0;
            if (dynamicProbability != null && useDynamic) {
                double calculated = dynamicProbability.Calculate();
                if (calculated >= 0) return calculated;
            }
            return probability;
        }

        public bool Find(T source) {
            hasSelected = false;
            if (query == null && target == null && actions.Count > 0) {
                hasSelected = true;
                return true;
            }
            InitStructureTransformer(source);
            bool found = transformer.Find(query);
            if (found) {
                transformer.Select();
                hasSelected = true;
                return true;
            } else return false;
        }

        public bool Apply(T source, bool useExisting = true) {
            if (useExisting && transformer != null) {
                if(target != null) transformer.Transform(target);
                foreach (RuleAction act in actions) {
                    act.Execute();
                }
                return true;
            }
            bool found = Find(source);
            if (found) {
                if (transformer != null) {
                    //transformer.Select();
                    //hasSelected = true;
                    if(target != null) transformer.Transform(target);
                }
                foreach (RuleAction act in actions) {
                    act.Execute();
                }
                return true;
            } else {
                return false;
            }
        }

        public bool HasSelected() {
            return hasSelected;
        }

        public void Deselect() {
            hasSelected = false;
        }

        protected void InitStructureTransformer(T source) {
            Transformer = grammar.Transformer;
            transformer.Source = source;
            transformer.Rule = this;
        }

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null) {
                switch (key) {
                    case "_probability":
                        result = GetProbability(false).ToString(); break;
                    case "_probability_dynamic":
                        result = GetProbability(true).ToString(); break;
                    case "_priority":
                        result = priority.ToString(); break;
                    case "_initial":
                        result = (query == null).ToString(); break;
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
                switch (subcontainerStr) {
                    case "query":
                        subcontainer = query; break;
                    case "target":
                        subcontainer = target; break;
                    case "task":
                        subcontainer = grammar.CurrentTask; break;
                    case "matches":
                        if (hasSelected && transformer != null) {
                            IDictionary<string, AttributedElement> matches = transformer.SelectedMatch;
                            passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                            if (passSpecifier != null) {
                                if (passSpecifier.Contains(".")) {
                                    string passPassSpecifier = passSpecifier.Substring(passSpecifier.IndexOf(".") + 1);
                                    string subsubcontainerStr = passSpecifier.Substring(0, passSpecifier.IndexOf("."));
                                    if (matches.ContainsKey(subsubcontainerStr) && matches[subsubcontainerStr] != null) {
                                        return matches[subsubcontainerStr].GetElements(passPassSpecifier);
                                    }
                                } else if (matches.ContainsKey(passSpecifier)) {
                                    return matches[passSpecifier].GetElements();
                                }
                            }
                        }
                        break;
                    case "source":
                    case "grammar":
                        subcontainer = grammar; break;
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                return base.GetElements(specifier);
            }
        }

        public void AddAction(RuleAction act) {
            actions.Add(act);
        }

        public void RemoveAction(RuleAction act) {
            actions.Remove(act);
        }
    }
}
