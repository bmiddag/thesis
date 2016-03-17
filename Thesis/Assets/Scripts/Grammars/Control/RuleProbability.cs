using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class RuleProbability {
        private MethodInfo method;
        public MethodInfo Method {
            get { return method; }
            set { method = value; }
        }
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        private List<object> arguments;
        public void AddArgument(object arg) {
            arguments.Add(arg);
        }

        public RuleProbability(MethodInfo method, object rule = null) {
            this.method = method;
            this.rule = rule;
            arguments = new List<object>();
        }

        public double Calculate() {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1+argCount) {
                object[] parameters = new object[1 + argCount];
                parameters[0] = rule;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                double result = (double)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static RuleProbability FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo condition = typeof(RuleProbability).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(double) && condition.GetParameters().Count() >= 1) {
                return new RuleProbability(condition, rule);
            } else return null;
        }

        // Example rule probability methods are listed here
    }
}
