using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grammars {
    public class GrammarCondition {
        private MethodInfo method;
        public MethodInfo Method {
            get { return method; }
            set { method = value; }
        }
        private object grammar;
        public object Grammar {
            get { return grammar; }
            set { grammar = value; }
        }

        private List<object> arguments;
        public void AddArgument(object arg) {
            arguments.Add(arg);
        }

        public GrammarCondition(MethodInfo method = null, object grammar = null) {
            this.method = method;
            this.grammar = grammar;
            arguments = new List<object>();
        }

        public bool Check() {
            // Check method signature
            int argCount = arguments.Count;
            if (grammar != null && method != null && method.ReturnType == typeof(int) && method.GetParameters().Count() == 1+argCount) {
                object[] parameters = new object[1 + argCount];
                parameters[0] = grammar;
                for (int i = 0; i < argCount; i++) {
                    parameters[i+1] = arguments[i];
                }
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return false;
            }
        }

        public static GrammarCondition FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo condition = typeof(GrammarCondition).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (condition != null && condition.IsStatic && condition.ReturnType == typeof(bool) && condition.GetParameters().Count() >= 1) {
                return new GrammarCondition(condition, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example stop condition / constraint condition methods are listed here
        // ********************************************************************************************************************

        // Default stop condition (this is an example - not referred to in the actual grammar code)
        public static bool NoRuleFound<T>(Grammar<T> grammar) where T : StructureModel {
            if (grammar.NoRuleFound) {
                return true; // Stop (still undefined)
            } else {
                return false; // Continue
            }
        }

        // AND-result of 2 grammar conditions
        public static bool And<T>(Grammar<T> grammar, GrammarCondition cond1, GrammarCondition cond2) where T : StructureModel {
            return (cond1.Check() && cond2.Check());
        }

        // OR-result of 2 grammar conditions
        public static bool Or<T>(Grammar<T> grammar, GrammarCondition cond1, GrammarCondition cond2) where T : StructureModel {
            return (cond1.Check() || cond2.Check());
        }
        
        /// <summary>
        /// Count elements with a specific attribute and compare it to a number
        /// </summary>
        /// <typeparam name="T">The structure the grammar generates.</typeparam>
        /// <param name="grammar">The grammar</param>
        /// <param name="attrName">Attribute name. If left empty, all elements will be counted.</param>
        /// <param name="attrValue">Attribute value. If left empty, all values will be allowed.</param>
        /// <param name="operation">A string representing the operation to compare the element count with the number, e.g. "==".</param>
        /// <param name="number">The number to compare the element count to.</param>
        /// <returns></returns>
        public static bool CountElementsWithAttribute<T>(Grammar<T> grammar, string attrName, string attrValue, string operation, int number) where T : StructureModel {
            int count;
            if (attrName == null || attrName == "") {
                count = grammar.Source.GetElements().Count;
            } else if (attrValue == null || attrValue == "") {
                count = grammar.Source.GetElements().Where(el => el.HasAttribute(attrName)).Count();
            } else {
                count = grammar.Source.GetElements().Where(el => el.GetAttribute(attrName) == attrValue).Count();
            }
            string smallOp = operation.ToLowerInvariant();
            switch (operation) {
                case "equals":
                case "==":
                    return count == number;
                case ">=":
                    return count >= number;
                case "<=":
                    return count <= number;
                case "greater":
                case ">":
                    return count > number;
                case "smaller":
                case "<":
                    return count < number;
                default:
                    return count == number;
            }
        }
    }
}
