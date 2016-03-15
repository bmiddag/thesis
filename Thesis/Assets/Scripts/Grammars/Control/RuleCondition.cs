using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class RuleCondition {
        private MethodInfo method;
        private object rule;

        public RuleCondition(MethodInfo method, object rule) {
            this.method = method;
            this.rule = rule;
        }

        public bool Check() {
            // Check method signature
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1) {
                object[] parameters = new object[1];
                parameters[0] = rule;
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return true;
            }
        }

        public static RuleCondition FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo condition = typeof(RuleCondition).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(bool) && condition.GetParameters().Count() == 1) {
                return new RuleCondition(condition, rule);
            } else return null;
        }

        // Example rule conditions are listed here
    }
}
