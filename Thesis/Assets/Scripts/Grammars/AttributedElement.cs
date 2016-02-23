using System;
using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Abstract class for any element with attributes (metadata).
    /// </summary>
	public abstract class AttributedElement {
		protected IDictionary<string, string> attributes;
        protected HashSet<AttributeClass> classes;
        public event EventHandler AttributeChanged;

        public AttributedElement() {
			attributes = new Dictionary<string, string>();
            classes = new HashSet<AttributeClass>();
		}

		public bool HasAttribute(string key) {
			return attributes.ContainsKey(key);
		}

		public string GetAttribute(string key) {
			return attributes[key];
		}

		public IDictionary<string, string> GetAttributes() {
			return attributes;
		}

		public void SetAttribute(string key, string value) {
			attributes[key] = value;
            OnAttributeChanged(EventArgs.Empty);
        }
		public void SetAttributes(IDictionary<string, string> dict) {
			if (dict != null) {
				foreach (KeyValuePair<string, string> entry in dict) {
					attributes[entry.Key] = entry.Value;
				}
                OnAttributeChanged(EventArgs.Empty);
			}
		}

        public HashSet<AttributeClass> GetAttributeClasses() {
            return classes;
        }

        public void AddAttributeClass(AttributeClass attClass) {
            if (attClass != null && !attClass.Equals(this)) {
                classes.Add(attClass);
                // Add classes from attribute class as well (so that all classes are listed)
                HashSet<AttributeClass> classClasses = attClass.GetAttributeClasses();
                foreach (AttributeClass cl in classClasses) {
                    classes.Add(cl);
                }
                // Add attributes from top class only
                SetAttributes(attClass.GetAttributes());
            }
        }

        protected void OnAttributeChanged(EventArgs e) {
            if (AttributeChanged != null) {
                AttributeChanged(this, EventArgs.Empty);
            }
        }

        public string this[string att] {
            get {
                return GetAttribute(att);
            }
            set {
                SetAttribute(att, value);
            }
        }
	}
}
