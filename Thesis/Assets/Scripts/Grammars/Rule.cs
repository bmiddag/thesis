using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class Rule<T, U>
        where T : StructureModel
        where U : IStructureTransformer<T>, new() {
        //Grammar<T> grammar;

        T query;
        T target;
        U transformer = default(U);

        public double probability;

        MethodInfo condition = null;
        MethodInfo dynamicProbability = null;

        public Rule(T query, T target, double probability, MethodInfo condition = null, MethodInfo dynamicProbability = null) {
            this.query = query;
            this.target = target;
            this.probability = probability;
            this.condition = condition;
            this.dynamicProbability = dynamicProbability;
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
            InitStructureTransformer(source);
            bool found = transformer.Find(query);
            return found;

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

        protected void InitStructureTransformer(T source) {
            if (transformer != null) {
                transformer.Destroy();
            }
            transformer = new U();
            transformer.Source = source;
        }

    }
}
