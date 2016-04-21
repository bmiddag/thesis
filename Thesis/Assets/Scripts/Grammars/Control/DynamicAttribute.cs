using System;
using System.Collections.Generic;
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
                /*if (method.IsGenericMethodDefinition) {
                    List<Type> methodTypeArguments = new List<Type>(method.GetGenericArguments().Where(t => t.IsGenericParameter));
                    Dictionary<Type, Type> typeDict = new Dictionary<Type, Type>();
                    for (int i = 0; i < argCount + 2; i++) {
                        if (method.GetParameters()[i].ParameterType.IsGenericTypeDefinition && parameters[i] != null) {
                            List<Type> genericTypeArgs = new List<Type>(method.GetParameters()[i].ParameterType.GetGenericArguments().Where(t => t.IsGenericParameter));
                            List<Type> typeArgs = new List<Type>(parameters[i].GetType().GetGenericArguments().Where(t => t.IsGenericParameter));
                            for (int j = 0; j < genericTypeArgs.Count; j++) {
                                if (genericTypeArgs[j].IsGenericTypeDefinition && !typeArgs[j].IsGenericTypeDefinition) {
                                    typeDict.Add(genericTypeArgs[j], typeArgs[j]);
                                }
                            }
                        }
                    }
                    Type[] newMethodTypeArgs = new Type[methodTypeArguments.Count];
                    for (int i = 0; i < methodTypeArguments.Count; i++) {
                        if (typeDict.ContainsKey(methodTypeArguments[i])) {
                            newMethodTypeArgs[i] = typeDict[methodTypeArguments[i]];
                        } else return null;
                    }
                    MethodInfo newMethod = method.MakeGenericMethod(newMethodTypeArgs);
                    if (newMethod != null && !newMethod.IsGenericMethodDefinition) method = newMethod;
                }*/
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
        public static string Constant(AttributedElement element, string attName, string constant) {
            return constant;
        }

        public static string Multiply(AttributedElement element, string attName, DynamicAttribute att1, DynamicAttribute att2) {
            string str1 = att1.GetAttributeValue();
            string str2 = att2.GetAttributeValue();
            double d1, d2;
            if (double.TryParse(str1, out d1) && double.TryParse(str2, out d2)) {
                return (d1 * d2).ToString();
            } else if (double.TryParse(str1, out d1)) {
                return d1.ToString();
            } else if (double.TryParse(str2, out d2)) {
                return d2.ToString();
            } else return "";
        }

        public static string Sum(AttributedElement element, string attName, DynamicAttribute att1, DynamicAttribute att2) {
            string str1 = att1.GetAttributeValue();
            string str2 = att2.GetAttributeValue();
            double d1, d2;
            if (double.TryParse(str1, out d1) && double.TryParse(str2, out d2)) {
                return (d1 + d2).ToString();
            } else if (double.TryParse(str1, out d1)) {
                return d1.ToString();
            } else if (double.TryParse(str2, out d2)) {
                return d2.ToString();
            } else return "";
        }

        public static string ReadAttribute(AttributedElement element, string attName, string elSel, string attribute) {
            List<AttributedElement> elements = element.GetElements(elSel);
            if (elements.Count > 0) {
                foreach (AttributedElement el in elements) {
                    string att = el.GetAttribute(attribute);
                    if (att != null) return att;
                }
            }
            return "";
        }

        public static string NumberOfAttributes(AttributedElement element, string attName) {
            return (element.GetAttributes(raw: true).Count).ToString();
        }

        public static string PartOfGroup(AttributedElement element, string attName, string groupSelector, string groupName) {
            object sourceObj = element.GetObjectAttribute("_grammar_matching");
            if (sourceObj == null || !typeof(AttributedElement).IsAssignableFrom(sourceObj.GetType())) return "false";
            AttributedElement sourceEl = (AttributedElement)sourceObj;

            List<AttributedElement> els = sourceEl.GetElements(groupSelector);
            if (els == null || els.Count == 0) return "false";
            AttributedElement groupEl = els[0];
            object group = groupEl.GetObjectAttribute(groupName);
            if (group == null) return "false";

            if (typeof(IEnumerable<AttributedElement>).IsAssignableFrom(group.GetType())) {
                IEnumerable<AttributedElement> grouplist = (IEnumerable<AttributedElement>)group;
                if (grouplist.Contains(sourceEl)) return "_grammar_automatch";
            }
            return "false";
        }

    }
}
