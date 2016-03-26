using System.Linq;
using System.Reflection;

namespace Grammars {
    public class DynamicAttribute : MethodCaller {
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

        public DynamicAttribute(MethodInfo method, AttributedElement element = null, string attName = null) : base(method) {
            this.element = element;
            this.attName = attName;
        }

        public DynamicAttribute(DynamicAttribute dynAtt, AttributedElement element) : base(dynAtt.Method) {
            this.element = element;
            attName = dynAtt.AttributeName;
        }

        public string GetAttributeValue() {
            // Check method signature
            int argCount = arguments.Count;
            if (element != null && attName != null && method != null &&
                method.ReturnType == typeof(string) && method.GetParameters().Count() == 2 + argCount) {
                object[] parameters = new object[2 + argCount];
                parameters[0] = element;
                parameters[1] = attName;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 2] = arguments[i];
                }
                string result = (string)method.Invoke(null, parameters);
                return result;
            } else {
                return null; // Should be caught. Method string will then remain unparsed.
            }
        }

        public static DynamicAttribute FromName(string name, AttributedElement element, string attName) {
            MethodInfo method = typeof(DynamicAttribute).GetMethod(name);
            //if (method != null) method = method.MakeGenericMethod(typeof(T));
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(string) && method.GetParameters().Count() >= 2) {
                return new DynamicAttribute(method, element, attName);
            } else return null;
        }

        // Example/test attribute modifiers are listed here
        public static string Multiply(AttributedElement element, string attName, double number1, double number2) {
            return (number1 * number2).ToString();
        }

        public static string NumberOfAttributes(AttributedElement element, string attName) {
            return (element.GetAttributes(raw: true).Count-1).ToString();
        }

    }
}
