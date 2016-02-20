using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Abstract class for any element with attributes (metadata).
    /// </summary>
	public abstract class AttributedElement {
		protected IDictionary<string, string> attributes;
        protected HashSet<AttributeClass> classes;

		public AttributedElement() {
			attributes = new Dictionary<string, string>();
            classes = new HashSet<AttributeClass>();
		}

		public bool hasAttribute(string key) {
			return attributes.ContainsKey(key);
		}

		public string getAttribute(string key) {
			return attributes[key];
		}

		public IDictionary<string, string> getAttributes() {
			return attributes;
		}

		public void setAttribute(string key, string value) {
			attributes[key] = value;
		}
		public void setAttributes(IDictionary<string, string> dict) {
			if (dict != null) {
				foreach (KeyValuePair<string, string> entry in dict) {
					attributes[entry.Key] = entry.Value;
				}
			}
		}

        public HashSet<AttributeClass> getAttributeClasses() {
            return classes;
        }

        public void addAttributeClass(AttributeClass attClass) {
            if (attClass != null && !attClass.Equals(this)) {
                classes.Add(attClass);
                setAttributes(attClass.getAttributes());
            }
        }
	}
}
