using System.Linq;
using System.Reflection;

namespace Grammars {
    public class DynamicAttribute : MethodCaller {
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

        public DynamicAttribute(MethodInfo method, object rule = null, AttributedElement element = null, string attName = null) : base(method) {
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

        public static DynamicAttribute FromName<T>(string name, Rule<T> rule, AttributedElement element, string attName) where T : StructureModel {
            MethodInfo method = typeof(DynamicAttribute).GetMethod(name);
            if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(string) && method.GetParameters().Count() >= 3) {
                return new DynamicAttribute(method, rule, element, attName);
            } else return null;
        }

        // Example attribute modifiers are listed here


    }
}
