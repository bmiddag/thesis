using System.Collections.Generic;
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

        // ********************************************************************************************************************
        // Example fitness / probability condition methods are listed here
        // ********************************************************************************************************************

        public static double Multiply<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return prob1.Calculate() * prob2.Calculate();
        }

        public static double And<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return Multiply(rule, prob1, prob2);
        }

        public static double Divide<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return prob1.Calculate() / prob2.Calculate();
        }

        public static double Sum<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return prob1.Calculate() + prob2.Calculate();
        }

        public static double Or<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return Sum(rule, prob1, prob2);
        }

        public static double Difference<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return prob1.Calculate() - prob2.Calculate();
        }

        public static double Min<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return System.Math.Min(prob1.Calculate(), prob2.Calculate());
        }

        public static double Max<T>(Rule<T> rule, RuleProbability prob1, RuleProbability prob2) where T : StructureModel {
            return System.Math.Max(prob1.Calculate(), prob2.Calculate());
        }

        public static double Inverse<T>(Rule<T> rule, RuleProbability prob) where T : StructureModel {
            return 1 - prob.Calculate();
        }

        public static double Not<T>(Rule<T> rule, RuleProbability prob) where T : StructureModel {
            return Inverse(rule, prob);
        }

        public static double Constant<T>(Rule<T> rule, double constant) where T : StructureModel {
            return constant;
        }

        public static double Bounds<T>(Rule<T> rule, RuleProbability prob, double lower, double upper) where T : StructureModel {
            return System.Math.Min(System.Math.Max(lower, prob.Calculate()), upper);
        }

        public static double NormalBounds<T>(Rule<T> rule, RuleProbability prob) where T : StructureModel {
            return System.Math.Min(System.Math.Max(0, prob.Calculate()), 1);
        }

        public static double AggregateOperation<T>(Rule<T> rule, string selector, string attrName, string aggOperation) where T : StructureModel {
            List<AttributedElement> elements = StringEvaluator.SelectElements(rule, selector);
            double result;
            if (attrName == null || attrName.Trim() == "") {
                result = elements.Count;
            } else {
                result = StringEvaluator.AggregateAttribute(aggOperation, elements, attrName);
            }
            return result;
        }

        public static double SumAttribute<T>(Rule<T> rule, string selector, string attrName) where T : StructureModel {
            return AggregateOperation(rule, selector, attrName, "SUM");
        }

        public static double Attribute<T>(Rule<T> rule, string selector, string attrName) where T : StructureModel {
            return AggregateOperation(rule, selector, attrName, "SUM");
        }

        public static double ElementCount<T>(Rule<T> rule, string selector, string attrName) where T : StructureModel {
            return AggregateOperation(rule, selector, attrName, "COUNT");
        }
    }
}
