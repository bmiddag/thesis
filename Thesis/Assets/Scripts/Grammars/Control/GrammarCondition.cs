using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class GrammarCondition {
        private MethodInfo method;
        private object grammar;

        public GrammarCondition(MethodInfo method, object grammar) {
            this.method = method;
            this.grammar = grammar;
        }

        public bool Check() {
            // Check method signature
            if (method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 1) {
                object[] parameters = new object[1];
                parameters[0] = grammar;
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return false;
            }
        }

        public static GrammarCondition FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo condition = typeof(GrammarCondition).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(bool) && condition.GetParameters().Count() == 1) {
                return new GrammarCondition(condition, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example stop condition methods are listed here
        // ********************************************************************************************************************

        // Default stop condition (this is an example - not referred to in the actual grammar code)
        public static bool NoRuleFound<T>(Grammar<T> grammar) where T : StructureModel {
            if (grammar.NoRuleFound) {
                return true; // Stop (still undefined)
            } else {
                return false; // Continue
            }
        }
    }
}
