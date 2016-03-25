using System.Linq;
using System.Reflection;

namespace Grammars {
    public class AttributeModifier : MethodCaller {
        private object rule;
        public object Rule {
            get { return rule; }
            set { rule = value; }
        }

        private AttributedElement element;
        public AttributedElement Element {
            get { return element; }
            set { element = value; }
        }

        private string attName;
        public string AttributeName {
            get { return attName; }
            set { attName = value; }
        }

        public AttributeModifier(MethodInfo method, object rule = null, AttributedElement element = null, string attName = null) : base(method) {
            this.rule = rule;
            this.element = element;
            this.attName = attName;
        }

        public string GetAttributeValue() {
            // Check method signature
            int argCount = arguments.Count;
            if (rule != null && element != null && attName != null && method != null &&
                method.ReturnType == typeof(string) && method.GetParameters().Count() == 3 + argCount) {
                object[] parameters = new object[3 + argCount];
                parameters[0] = rule;
                parameters[1] = element;
                parameters[2] = attName;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 3] = arguments[i];
                }
                string result = (string)method.Invoke(null, parameters);
                return result;
            } else {
                return null; // Should be caught. Method string will then remain unparsed.
            }
        }

        public static AttributeModifier FromName<T>(string name, Rule<T> rule, AttributedElement element, string attName, object[] parameters = null) where T : StructureModel {
            MethodInfo modifier = typeof(AttributeModifier).GetMethod(name);
            if (modifier != null) modifier = modifier.MakeGenericMethod(typeof(T));

            int args = 0;
            if (parameters != null) args += parameters.Length;

            // Check method signature. Has to be static if created from here.
            if (modifier != null && modifier.IsStatic && modifier.ReturnType == typeof(string) && modifier.GetParameters().Count() >= 3 + args) {
                AttributeModifier attMod = new AttributeModifier(modifier, rule, element, attName);
                if (parameters != null) {
                    foreach (object param in parameters) {
                        attMod.AddArgument(param);
                    }
                }
                return attMod;
            } else return null;
        }

        public static AttributeModifier Parse<T>(string methodString, Rule<T> rule, AttributedElement element, string attName) where T : StructureModel {
            string[] args = null;
            string methodName = OperationStringParser.ParseMethodString(methodString, out args);
            if (methodName == null || methodName.Trim() == "") {
                return null;
            } else {
                AttributeModifier mod = FromName(methodName, rule, element, attName);
                if (mod.Method.GetParameters().Length != args.Length + 3) return null;
                for (int i = 0; i < args.Length; i++) {
                    if (mod.Method.GetParameters()[i+3].ParameterType == typeof(AttributeModifier)) {
                        AttributeModifier argMod = Parse(args[i], rule, element, attName);
                        mod.AddArgument(argMod);
                    } else if (mod.Method.GetParameters()[i+3].ParameterType == typeof(GrammarCondition)) {
                        GrammarCondition grCond = GrammarCondition.Parse(args[i], rule.Grammar);
                        mod.AddArgument(grCond);
                    } else if (mod.Method.GetParameters()[i+3].ParameterType == typeof(GrammarProbability)) {
                        GrammarProbability argProb = GrammarProbability.Parse(args[i], rule.Grammar);
                        mod.AddArgument(argProb);
                    }
                }
                return mod;
            }
        }

        // Example attribute modifiers are listed here


    }
}
