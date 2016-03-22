using System.Linq;
using System.Reflection;

namespace Grammars {
    public class RuleCondition : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        public RuleCondition(MethodInfo method, object rule = null) : base(method) {
            this.rule = rule;
        }

        public bool Check() {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1+argCount) {
                object[] parameters = new object[1+argCount];
                parameters[0] = rule;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return true;
            }
        }

        public static RuleCondition FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo condition = typeof(RuleCondition).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(bool) && condition.GetParameters().Count() >= 1) {
                return new RuleCondition(condition, rule);
            } else return null;
        }

        // Example rule conditions are listed here
    }
}
