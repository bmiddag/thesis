using System;
using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// A class of attributes that are inherited by every element of said class. They are copied into the element at load time.
    /// Note that an attribute class can also inherit attributes from multiple classes.
    /// </summary>
    public class AttributeClass : AttributedElement {
        string name;

        public override IElementContainer Container {
            get { return null; }
        }

        public override string LinkType {
            get { return "class"; }
            set { }
        }

        public AttributeClass(string name) : base() {
            this.name = name;
            if (name == null) this.name = "";
        }

        public string GetName() {
            return name;
        }

        // Setter isn't necessary since we won't be changing the name at runtime

        // ************************** CLASSES DICTIONARY ************************** \\

        private static Dictionary<string, AttributeClass> allClasses = null;
        public static Dictionary<string, AttributeClass> AllClasses {
            get {
                if (allClasses == null) {
                    allClasses = new Dictionary<string, AttributeClass>();
                }
                return allClasses;
            }
            set { allClasses = value; }
        }
        public static void Add(AttributeClass cl) {
            if (cl == null) return;
            string name = cl.GetName();
            if (allClasses == null) {
                allClasses = new Dictionary<string, AttributeClass>();
            }
            if (!allClasses.ContainsKey(name)) {
                allClasses.Add(name, cl);
            }
        }
        public static AttributeClass Get(string name) {
            if (name == null || name.Trim() == "") return null;
            if (AllClasses.ContainsKey(name)) {
                return allClasses[name];
            } else return null;
        }
        public static void Remove(string name) {
            if (name == null || name.Trim() == "") return;
            if (AllClasses.ContainsKey(name)) {
                allClasses.Remove(name);
            }
        }

        // ************************** EQUALITY TESTING ************************** \\
        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }
            AttributeClass c = obj as AttributeClass;
            if ((object)c == null) {
                return false;
            }
            return (name == c.GetName());
        }

        public bool Equals(AttributeClass c) {
            if ((object)c == null) {
                return false;
            }
            return (name == c.GetName());
        }

        public override int GetHashCode() {
            int hash = 87654321;
            hash ^= name.GetHashCode();
            return hash;
        }
    }
}
