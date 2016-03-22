using System.Linq;
using System.Reflection;

namespace Grammars {
    public class RuleMatchSelector : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        public RuleMatchSelector(MethodInfo method, object rule = null) : base(method) {
            this.rule = rule;
        }

        public int Select(object matches) {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 2+argCount) {
                object[] parameters = new object[2+argCount];
                parameters[0] = rule;
                parameters[1] = matches;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 2] = arguments[i];
                }
                int result = (int)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static RuleMatchSelector FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo condition = typeof(RuleMatchSelector).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(int) && condition.GetParameters().Count() >= 2) {
                return new RuleMatchSelector(condition, rule);
            } else return null;
        }

        // Example match selection methods are listed here
    }
}
