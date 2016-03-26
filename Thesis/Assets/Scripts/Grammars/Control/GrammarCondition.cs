using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class GrammarCondition : MethodCaller {
        private object grammar;
        public object Grammar {
            get { return grammar; }
            set { grammar = value; }
        }

        public GrammarCondition(MethodInfo method = null, object grammar = null) : base(method) {
            this.grammar = grammar;
        }

        public bool Check() {
            // Check method signature
            int argCount = arguments.Count;
            if (grammar != null && method != null && method.ReturnType == typeof(bool) && method.GetParameters().Count() == 1 + argCount) {
                object[] parameters = new object[1 + argCount];
                parameters[0] = grammar;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                bool result = (bool)method.Invoke(null, parameters);
                return result;
            } else {
                return false;
            }
        }

        public static GrammarCondition FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo method = typeof(GrammarCondition).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(bool) && method.GetParameters().Count() >= 1) {
                return new GrammarCondition(method, grammar);
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

        // NOT-result of a grammar condition
        public static bool Not<T>(Grammar<T> grammar, GrammarCondition cond) where T : StructureModel {
            return !cond.Check();
        }

        /// <summary>
        /// Count elements with a specific attribute or perform any aggregate operation on them and compare it to a number
        /// </summary>
        /// <typeparam name="T">The structure the grammar generates.</typeparam>
        /// <param name="grammar">The grammar</param>
        /// <param name="selector">The selector string (Dynamic LINQ query)</param>
        /// <param name="attrName">Attribute name. If left empty, all elements will be counted.</param>
        /// <param name="aggOperation">The aggregate operation to perform (e.g. COUNT, SUM, AVG, etc.)</param>
        /// <param name="cmpOperation">A string representing the operation to compare the element count with the number, e.g. "==".</param>
        /// <param name="number">The number to compare the element count to.</param>
        /// <returns></returns>
        public static bool AggregateOperation<T>(Grammar<T> grammar, string selector, string attrName, string aggOperation, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(grammar, selector);
            double result;
            if (attrName == null || attrName.Trim() == "") {
                result = elements.Count;
            } else {
                result = StringEvaluator.AggregateAttribute(aggOperation, elements, attrName);
            }
            return StringEvaluator.Compare(cmpOperation, result, number);
        }

        public static bool CountElements<T>(Grammar<T> grammar, string selector, string cmpOperation, double number) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(grammar, selector);
            return StringEvaluator.Compare(cmpOperation, elements.Count, number);
        }

        public static bool SumAttribute<T>(Grammar<T> grammar, string selector, string attrName, string cmpOperation, double number) where T : StructureModel {
            return AggregateOperation(grammar, selector, attrName, "SUM", cmpOperation, number);
        }
    }
}