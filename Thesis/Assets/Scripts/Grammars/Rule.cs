using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class Rule<T> where T : StructureModel {
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
        protected MethodInfo condition = null;
        protected MethodInfo dynamicProbability = null;
        protected MethodInfo controlledSelection = null;

        bool hasSelected;

        public Rule(T query, T target, double probability, MethodInfo condition = null,
            MethodInfo dynamicProbability = null, MethodInfo controlledSelection = null) {
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
                object[] parameters = new object[1];
                parameters[0] = this;
                bool result = (bool)condition.Invoke(null, parameters);
                return result;
            } else {
                return true;
            }
        }

        public double GetProbability(bool useDynamic = true) {
            if (dynamicProbability != null && useDynamic) {
                object[] parameters = new object[1];
                parameters[0] = this;
                double result = (double)dynamicProbability.Invoke(null, parameters);
                // Change probability or not?
                return result;
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
                    transformer.Select(controlledSelection, parameters);
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

        protected void InitStructureTransformer(T source) {
            Transformer = grammar.Transformer;
            transformer.Source = source;
        }

    }
}
