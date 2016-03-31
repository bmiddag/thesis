using System;

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

        public AttributeClass(string name) : base() {
            this.name = name;
            if (name == null) this.name = "";
        }

        public string GetName() {
            return name;
        }

        // Setter isn't necessary since we won't be changing the name at runtime

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
