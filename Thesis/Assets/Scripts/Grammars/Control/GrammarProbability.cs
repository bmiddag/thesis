﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class GrammarProbability : MethodCaller {
        private object grammar;
        public object Grammar {
            get { return grammar; }
            set { grammar = value; }
        }

        public GrammarProbability(MethodInfo method = null, object grammar = null) : base(method) {
            this.grammar = grammar;
        }

        public double Calculate() {
            // Check method signature
            int argCount = arguments.Count;
            if (grammar != null && method != null && method.ReturnType == typeof(double) && method.GetParameters().Count() == 1 + argCount) {
                object[] parameters = new object[1 + argCount];
                parameters[0] = grammar;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 1] = arguments[i];
                }
                double result = (double)method.Invoke(null, parameters);
                return result;
            } else {
                return -1;
            }
        }

        public static GrammarProbability FromName<T>(string name, Grammar<T> grammar) where T : StructureModel {
            MethodInfo method = typeof(GrammarProbability).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(double) && method.GetParameters().Count() >= 1) {
                return new GrammarProbability(method, grammar);
            } else return null;
        }

        // ********************************************************************************************************************
        // Example fitness / probability condition methods are listed here
        // ********************************************************************************************************************

        public static double Multiply<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return prob1.Calculate() * prob2.Calculate();
        }

        public static double And<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return Multiply(grammar, prob1, prob2);
        }

        public static double Divide<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return prob1.Calculate() / prob2.Calculate();
        }

        public static double Sum<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return prob1.Calculate() + prob2.Calculate();
        }

        public static double Or<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return Sum(grammar, prob1, prob2);
        }

        public static double Difference<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return prob1.Calculate() - prob2.Calculate();
        }

        public static double Min<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return System.Math.Min(prob1.Calculate(), prob2.Calculate());
        }

        public static double Max<T>(Grammar<T> grammar, GrammarProbability prob1, GrammarProbability prob2) where T : StructureModel {
            return System.Math.Max(prob1.Calculate(), prob2.Calculate());
        }

        public static double Inverse<T>(Grammar<T> grammar, GrammarProbability prob) where T : StructureModel {
            return 1 - prob.Calculate();
        }

        public static double Not<T>(Grammar<T> grammar, GrammarProbability prob) where T : StructureModel {
            return Inverse(grammar, prob);
        }

        public static double Constant<T>(Grammar<T> grammar, double constant) where T : StructureModel {
            return constant;
        }

        public static double Bounds<T>(Grammar<T> grammar, GrammarProbability prob, double lower, double upper) where T : StructureModel {
            return System.Math.Min(System.Math.Max(lower, prob.Calculate()), upper);
        }

        public static double NormalBounds<T>(Grammar<T> grammar, GrammarProbability prob) where T : StructureModel {
            return System.Math.Min(System.Math.Max(0, prob.Calculate()), 1);
        }

        /// <summary>
        /// Count elements with a specific attribute or perform any aggregate operation on them.
        /// </summary>
        /// <typeparam name="T">The structure the grammar generates.</typeparam>
        /// <param name="grammar">The grammar</param>
        /// <param name="selector">The selector string (Dynamic LINQ query)</param>
        /// <param name="attrName">Attribute name. If left empty, all elements will be counted.</param>
        /// <param name="aggOperation">The aggregate operation to perform (e.g. COUNT, SUM, AVG, etc.)</param>
        /// <returns></returns>
        public static double AggregateOperation<T>(Grammar<T> grammar, string selector, string attrName, string aggOperation) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(grammar.Source, selector);
            double result;
            if (attrName == null || attrName.Trim() == "") {
                result = elements.Count;
            } else {
                result = StringEvaluator.AggregateAttribute(aggOperation, elements, attrName);
            }
            return result;
        }

        public static double SumAttribute<T>(Grammar<T> grammar, string selector, string attrName) where T : StructureModel {
            return AggregateOperation(grammar, selector, attrName, "SUM");
        }
    }
}