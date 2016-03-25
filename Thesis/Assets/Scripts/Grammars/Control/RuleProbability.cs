using System.Linq;
using System.Reflection;

namespace Grammars {
    public class RuleProbability : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        public RuleProbability(MethodInfo method, object rule = null) : base(method) {
            this.rule = rule;
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
            MethodInfo method = typeof(RuleProbability).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(double) && method.GetParameters().Count() >= 1) {
                return new RuleProbability(method, rule);
            } else return null;
        }

        // Example rule probability methods are listed here
    }
}
