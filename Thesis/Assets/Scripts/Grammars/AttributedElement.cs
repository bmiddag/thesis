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
        protected bool postponeEvents;

        public AttributedElement() {
			attributes = new Dictionary<string, string>();
            classes = new HashSet<AttributeClass>();
            postponeEvents = false;
		}

		public bool HasAttribute(string key) {
			return attributes.ContainsKey(key);
		}

        public bool MatchAttributes<T>(AttributedElement el, Rule<T> rule) where T : StructureModel {
            if (el == null) return true;
            bool match = true;
            Dictionary<string, string> thisAtts = EvaluateDynamicAttributes(rule, true);
            Dictionary<string, string> otherAtts = el.EvaluateDynamicAttributes(rule, true);
            match = MatchAttributes(el);
            SetAttributes(thisAtts, false);
            el.SetAttributes(otherAtts, false);
            return match;
        }

        public Dictionary<string, string> EvaluateDynamicAttributes<T>(Rule<T> rule, bool evaluateDouble=true) where T : StructureModel {
            Dictionary<string, string> originalAttributes = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> attribute in attributes) {
                DynamicAttribute da = null;
                if (attribute.Value.StartsWith("@@") && evaluateDouble) {
                    da = (DynamicAttribute)StringEvaluator.ParseMethodCaller(attribute.Value.Substring(2), typeof(DynamicAttribute), null, rule, this, attribute.Key);
                } else if (attribute.Value.StartsWith("@") && (attribute.Value.Length < 2 || attribute.Value[1] != '@')) {
                    da = (DynamicAttribute)StringEvaluator.ParseMethodCaller(attribute.Value.Substring(1), typeof(DynamicAttribute), null, rule, this, attribute.Key);
                }
                if (da == null) continue;
                string newVal = da.GetAttributeValue();
                if (newVal == null) continue;
                originalAttributes.Add(attribute.Key, attribute.Value);
                attributes[attribute.Key] = newVal;
            }
            return originalAttributes;
        }

        public bool MatchAttributes(AttributedElement el) {
            if (el == null) return true;
            IDictionary<string, string> dict = el.GetAttributes();
            if (dict == null || dict.Keys.Count == 0) return true;
            bool exactMatch = el.HasAttribute("_grammar_exactmatch");
            bool matchClasses = el.HasAttribute("_grammar_matchclasses");
            bool noMatch = el.HasAttribute("_grammar_nomatch");
            int count = 0;
            bool attsMatched = true;
            foreach (KeyValuePair<string, string> entry in dict) {
                if (entry.Key.StartsWith("_grammar_")) {
                    // Ignore _grammar_ attributes, but use them for selection properties
                } else {
                    if (!HasAttribute(entry.Key) || attributes[entry.Key] != entry.Value) {
                        attsMatched = false;
                        if(!noMatch) return false;
                    } else if (exactMatch && noMatch) {
                        return false;
                    }
                    count++;
                }
            }
            if (noMatch && attsMatched) return false;
            if (exactMatch) {
                HashSet<string> keys = new HashSet<string>(attributes.Keys);
                keys.RemoveWhere(key => key.StartsWith("_grammar_"));
                if (!noMatch && keys.Count != count) return false;
                if (noMatch && keys.Count == count) return false;
            }
            if (matchClasses) {
                bool matched = true;
                foreach (AttributeClass cl in el.GetAttributeClasses()) {
                    if (!classes.Contains(cl)) {
                        matched = false;
                        if(!noMatch) return false;
                    }
                }
                if (noMatch && matched) return false;
            }
            return true;
        }

        public virtual string GetAttribute(string key) {
            if (attributes.ContainsKey(key)) {
                return attributes[key];
            } else return null;
		}

		public IDictionary<string, string> GetAttributes() {
			return attributes;
		}

		public void SetAttribute(string key, string value, bool notify=true) {
			attributes[key] = value;
            if(notify) OnAttributeChanged(EventArgs.Empty);
        }

        public void RemoveAttribute(string key, bool notify=true) {
            attributes.Remove(key);
            if(notify) OnAttributeChanged(EventArgs.Empty);
        }

		public void SetAttributes(IDictionary<string, string> dict, bool notify=true) {
			if (dict != null) {
				foreach (KeyValuePair<string, string> entry in dict) {
					attributes[entry.Key] = entry.Value;
				}
                if(notify) OnAttributeChanged(EventArgs.Empty);
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
            } else {
                newAtts = attributes;
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

        public void SetAttributesUsingDifference<T>(AttributedElement source, AttributedElement target, Rule<T> rule) where T : StructureModel {
            if (target == null) return;
            Dictionary<string, string> targetAtts = target.EvaluateDynamicAttributes(rule, false);
            SetAttributesUsingDifference(source, target);
            target.SetAttributes(targetAtts, false);
        }

        /// <summary>
        /// Change attributes based on the difference between two other attributed elements (used for e.g. grammar rules)
        /// </summary>
        /// <param name="source">The left-hand side element</param>
        /// <param name="target">The right-hand side element</param>
        public void SetAttributesUsingDifference(AttributedElement source, AttributedElement target) {
            HashSet<AttributeClass> newCls = target.GetNewAttributeClasses(source);
            HashSet<AttributeClass> remCls = target.GetRemovedAttributeClasses(source);
            IDictionary<string, string> newAtts = target.GetNewAttributes(source);
            HashSet<string> remAtts = target.GetRemovedAttributes(source);

            // Remove, then add classes
            classes.ExceptWith(remCls);
            classes.UnionWith(newCls);

            // Remove, then add attributes
            foreach (string key in remAtts) {
                attributes.Remove(key);
            }
            SetAttributes(newAtts);
            OnAttributeChanged(EventArgs.Empty);
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

        public void PostponeAttributeChanged(bool postpone) {
            if (postpone) {
                postponeEvents = true;
            } else if (postponeEvents) {
                postponeEvents = false;
                OnAttributeChanged(EventArgs.Empty);
            }
        }

        protected void OnAttributeChanged(EventArgs e) {
            if (AttributeChanged != null && !postponeEvents) {
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
