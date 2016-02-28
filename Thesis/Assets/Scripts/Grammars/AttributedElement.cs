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

        public bool MatchAttributes(AttributedElement el) {
            IDictionary<string, string> dict = el.GetAttributes();
            if (dict == null || dict.Keys.Count == 0) return true;
            bool exactMatch = false;
            bool matchClasses = false;
            int count = 0;
            foreach (KeyValuePair<string, string> entry in dict) {
                if (entry.Key.StartsWith("_grammar_")) {
                    // Ignore _grammar_ attributes, but use them for selection properties
                    switch (entry.Key) {
                        case "_grammar_exactmatch":
                            exactMatch = true; break;
                        case "_grammar_matchclasses":
                            matchClasses = true; break;
                    }
                } else {
                    if (!(HasAttribute(entry.Key) && attributes[entry.Key] == entry.Value)) return false;
                    count++;
                }
            }
            if (exactMatch) {
                HashSet<string> keys = new HashSet<string>(attributes.Keys);
                keys.RemoveWhere(key => key.StartsWith("_grammar_"));
                if (keys.Count != count) return false;
            }
            if (matchClasses) {
                foreach (AttributeClass cl in classes) {
                    if (!el.GetAttributeClasses().Contains(cl)) return false;
                }
            }
            return true;
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

        public void RemoveAttribute(string key) {
            attributes.Remove(key);
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

        public HashSet<AttributeClass> GetNewAttributeClasses(AttributedElement el) {
            HashSet<AttributeClass> newAttCl = new HashSet<AttributeClass>(classes);
            if (el != null) {
                newAttCl.ExceptWith(el.GetAttributeClasses());
            }
            return newAttCl;
        }

        public HashSet<AttributeClass> GetRemovedAttributeClasses(AttributedElement el) {
            HashSet<AttributeClass> remAttCl;
            if (el != null) {
                remAttCl = new HashSet<AttributeClass>(el.GetAttributeClasses());
                remAttCl.ExceptWith(classes);
            } else {
                remAttCl = new HashSet<AttributeClass>();
            }
            return remAttCl;
        }

        /// <summary>
        /// Get attributes that are either new or different from the ones in another attributed element
        /// </summary>
        /// <param name="el">the attributed element to compare to</param>
        /// <returns>a dictionary of attributes and their values that are different</returns>
        public IDictionary<string, string> GetNewAttributes(AttributedElement el) {
            IDictionary<string, string> newAtts = new Dictionary<string, string>();
            if (el != null) {
                foreach (KeyValuePair<string, string> entry in attributes) {
                    if (el.GetAttribute(entry.Key) != attributes[entry.Key]) {
                        newAtts[entry.Key] = entry.Value;
                    }
                }
            }
            return newAtts;
        }

        /// <summary>
        /// Get attributes that were removed in this element compared to another attributed element
        /// </summary>
        /// <param name="el">the attributed element to compare to</param>
        /// <returns>the set of attributes that are not present in this element</returns>
        public HashSet<string> GetRemovedAttributes(AttributedElement el) {
            HashSet<string> remAtts;
            if (el != null) {
                remAtts = new HashSet<string>(el.GetAttributes().Keys);
                remAtts.ExceptWith(attributes.Keys);
            } else {
                remAtts = new HashSet<string>();
            }
            return remAtts;
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
