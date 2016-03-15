using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class RuleProbability {
        private MethodInfo method;
        private object rule;

        public RuleProbability(MethodInfo method, object rule) {
            this.method = method;
            this.rule = rule;
        }

        public double Calculate() {
            // Check method signature
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1) {
                object[] parameters = new object[1];
                parameters[0] = rule;
                double result = (double)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static RuleProbability FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo condition = typeof(RuleProbability).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(double) && condition.GetParameters().Count() == 1) {
                return new RuleProbability(condition, rule);
            } else return null;
        }

        // Example rule probability methods are listed here
    }
}
