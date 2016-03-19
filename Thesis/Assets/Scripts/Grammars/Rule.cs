using System;
using System.Collections.Generic;

namespace Grammars {
    public class Rule<T> : IElementContainer
        where T : StructureModel {
        protected Grammar<T> grammar;
        protected T query;
        protected T target;
        protected IStructureTransformer<T> transformer = null;
        public IStructureTransformer<T> Transformer {
            get {
                return transformer;
            }
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
        protected RuleProbability dynamicProbability = null;
        protected RuleMatchSelector controlledSelection = null;

        bool hasSelected;

        public Rule(T query, T target, double probability, RuleCondition condition = null,
            RuleProbability dynamicProbability = null, RuleMatchSelector controlledSelection = null) {
            this.query = query;
            this.target = target;
            this.probability = probability;
            this.condition = condition;
            this.dynamicProbability = dynamicProbability;
            this.controlledSelection = controlledSelection;
            hasSelected = false;
        }

        public bool CheckCondition() {
            if (condition != null) {
                return condition.Check();
            } else {
                return true;
            }
        }

        public double GetProbability(bool useDynamic = true) {
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
                if (controlledSelection != null) {
                    object[] parameters = new object[1];
                    parameters[0] = this;
                    transformer.Select(controlledSelection);
                } else {
                    transformer.Select();
                }
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
