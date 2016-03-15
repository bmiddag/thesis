using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class RuleMatchSelector {
        private MethodInfo method;
        private object rule;

        public RuleMatchSelector(MethodInfo method, object rule) {
            this.method = method;
            this.rule = rule;
        }

        public int Select(object matches) {
            // Check method signature
            if (method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 2) {
                object[] parameters = new object[2];
                parameters[0] = rule;
                parameters[1] = matches;
                int result = (int)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static RuleMatchSelector FromName<T>(string name, Rule<T> rule) where T : StructureModel {
            MethodInfo condition = typeof(RuleMatchSelector).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(int) && condition.GetParameters().Count() == 2) {
                return new RuleMatchSelector(condition, rule);
            } else return null;
        }

        // Example match selection methods are listed here
    }
}
