using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class GrammarStopCondition {
        private MethodInfo method;
        private object grammar;

        public GrammarStopCondition(MethodInfo method, object grammar) {
            this.method = method;
            this.grammar = grammar;
        }

        public int Check() {
            // Check method signature
            if (method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 1) {
                object[] parameters = new object[1];
                parameters[0] = grammar;
                int result = (int)method.Invoke(null, parameters);
                return result;
            } else {
                return 0; // Continue
            }
        }

        public static GrammarStopCondition FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo condition = typeof(GrammarStopCondition).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(int) && condition.GetParameters().Count() == 1) {
                return new GrammarStopCondition(condition, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example stop condition methods are listed here
        // ********************************************************************************************************************

        // Default stop condition (this is an example - not referred to in the actual grammar code)
        public static int NoRuleFound<T>(Grammar<T> grammar) where T : StructureModel {
            if (grammar.NoRuleFound) {
                return -1; // Stop (still undefined)
            } else {
                return 0; // Continue
            }
        }
    }
}
