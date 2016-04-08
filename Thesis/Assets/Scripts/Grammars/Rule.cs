using System;
using System.Collections.Generic;

namespace Grammars {
    public class Rule<T> : IElementContainer
        where T : StructureModel {
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

        protected bool hasSelected;
        protected bool active;

        protected int priority = 0;
        public int Priority {
            get { return priority; }
            set { priority = value; }
        }
        

        public Rule(Grammar<T> grammar, double probability, int priority = 0, bool active = true, T query = null, T target = null,
            RuleCondition condition = null, RuleProbability dynamicProbability = null, RuleMatchSelector matchSelector = null) {
            this.grammar = grammar;
            this.query = query;
            this.target = target;
            this.probability = probability;
            this.condition = condition;
            this.dynamicProbability = dynamicProbability;
            this.matchSelector = matchSelector;
            this.active = active;
            hasSelected = false;
        }

        public bool CheckCondition() {
            if (!active) return false;
            if (condition != null) {
                return condition.Check();
            } else {
                return true;
            }
        }

        public double GetProbability(bool useDynamic = true) {
            if (!active) return 0;
            if (dynamicProbability != null && useDynamic) {
                double calculated = dynamicProbability.Calculate();
                if (calculated >= 0) return calculated;
            }
            return probability;
        }

        public bool Find(T source) {
            hasSelected = false;
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
                transformer.Transform(target);
                return true;
            }
            bool found = Find(source);
            if (found) {
                transformer.Transform(target);
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

        public List<AttributedElement> GetElements(string specifier = null) {
            IElementContainer subcontainer = grammar;
            string passSpecifier = specifier;
            if (specifier != null && specifier.Contains(".")) {
                string subcontainerStr = specifier.Substring(0, specifier.IndexOf("."));
                switch (subcontainerStr) {
                    case "query":
                        subcontainer = query; break;
                    case "target":
                        subcontainer = target; break;
                    case "task":
                        subcontainer = grammar.CurrentTask; break;
                    case "source":
                    case "grammar":
                    default:
                        subcontainer = grammar; break;
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                return new List<AttributedElement>();
            }
        }
    }
}
