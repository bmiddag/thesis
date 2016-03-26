﻿using System;
using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Abstract class for any element with attributes (metadata).
    /// </summary>
	public abstract class AttributedElement {
		protected IDictionary<string, string> attributes;
        protected IDictionary<string, DynamicAttribute> dynamicAttributes;
        protected HashSet<AttributeClass> classes;
        public event EventHandler AttributeChanged;
        protected bool postponeEvents;

        public AttributedElement() {
			attributes = new Dictionary<string, string>();
            dynamicAttributes = new Dictionary<string, DynamicAttribute>();
            classes = new HashSet<AttributeClass>();
            postponeEvents = false;
		}

		public bool HasAttribute(string key) {
			return attributes.ContainsKey(key);
		}

        public bool MatchAttributes(AttributedElement el, bool raw=false) {
            if (el == null) return true;
            ICollection<string> otherKeys = el.GetAttributes(true).Keys;
            if (otherKeys == null || otherKeys.Count == 0) return true;
            bool exactMatch = el.HasAttribute("_grammar_exactmatch");
            bool matchClasses = el.HasAttribute("_grammar_matchclasses");
            bool noMatch = el.HasAttribute("_grammar_nomatch");
            int count = 0;
            bool attsMatched = true;
            foreach (string otherAtt in otherKeys) {
                if (otherAtt.StartsWith("_grammar_")) {
                    // Ignore _grammar_ attributes, but use them for selection properties
                } else {
                    if (!HasAttribute(otherAtt) || GetAttribute(otherAtt, raw) != el.GetAttribute(otherAtt, raw)) {
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

        public string GetAttribute(string key) {
            return GetAttribute(key, false);
        }

        public virtual string GetAttribute(string key, bool raw) {
            if (dynamicAttributes.ContainsKey(key) && !raw) {
                return dynamicAttributes[key].GetAttributeValue();
            } else if (attributes.ContainsKey(key)) {
                return attributes[key];
            } else return null;
		}

		public IDictionary<string, string> GetAttributes(bool raw=false) {
            Dictionary<string, string> dict = new Dictionary<string, string>(attributes);
            if (!raw) {
                foreach (string key in dynamicAttributes.Keys) {
                    dict[key] = dynamicAttributes[key].GetAttributeValue();
                }
            }
			return dict;
		}

		public void SetAttribute(string key, string value, bool notify=true, bool copy=false) {
            if (dynamicAttributes.ContainsKey(key)) {
                dynamicAttributes.Remove(key);
            }
            if (value.StartsWith("@_") || value.StartsWith("@+")) {
                // @ = Dynamic, _ = copies are not dynamic, + = copies are dynamic
                DynamicAttribute da = StringEvaluator.ParseDynamicAttribute(value.Substring(2), this, key);
                if (copy && value.StartsWith("@_")) {
                    value = da.GetAttributeValue();
                } else {
                    dynamicAttributes.Add(key, da);
                }
            }
			attributes[key] = value;
            if(notify) OnAttributeChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Used in special situations when the dynamic attribute cannot be parsed from a string. Note that this type of attribute cannot be copied!
        /// </summary>
        /// <param name="key">attribute key</param>
        /// <param name="da">dynamic attribute object</param>
        /// <param name="notify">send an attribute changed event?</param>
        public void SetDynamicAttribute(string key, DynamicAttribute da, bool notify = true) {
            if (da != null) {
                string value = "@>" + da.Method.Name;
                dynamicAttributes.Add(key, new DynamicAttribute(da, this));
                attributes[key] = value;
                if (notify) OnAttributeChanged(EventArgs.Empty);
            }
        }

        public void RemoveAttribute(string key, bool notify=true) {
            if (dynamicAttributes.ContainsKey(key)) dynamicAttributes.Remove(key);
            if (attributes.ContainsKey(key)) attributes.Remove(key);
            if(notify) OnAttributeChanged(EventArgs.Empty);
        }

		public void SetAttributes(IDictionary<string, string> dict, bool notify=true, bool copy=false) {
			if (dict != null) {
				foreach (KeyValuePair<string, string> entry in dict) {
                    SetAttribute(entry.Key, entry.Value, notify:false, copy:copy);
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
        public IDictionary<string, string> GetNewAttributes(AttributedElement el, bool raw=true) {
            IDictionary<string, string> newAtts = new Dictionary<string, string>();
            if (el != null) {
                foreach (KeyValuePair<string, string> entry in attributes) {
                    if (el.GetAttribute(entry.Key, raw) != GetAttribute(entry.Key, raw)) {
                        newAtts[entry.Key] = entry.Value;
                    }
                }
            } else {
                newAtts = GetAttributes(raw);
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
                remAtts = new HashSet<string>(el.GetAttributes(raw:true).Keys);
                remAtts.ExceptWith(attributes.Keys);
            } else {
                remAtts = new HashSet<string>();
            }
            return remAtts;
        }

        /// <summary>
        /// Change attributes based on the difference between two other attributed elements (used for e.g. grammar rules)
        /// </summary>
        /// <param name="source">The left-hand side element</param>
        /// <param name="target">The right-hand side element</param>
        public void SetAttributesUsingDifference(AttributedElement source, AttributedElement target, bool notify=true) {
            if (target == null) {
                classes.ExceptWith(source.GetAttributeClasses());
                foreach (string key in source.GetAttributes(true).Keys) {
                    RemoveAttribute(key, notify:false);
                }
            } else {
                HashSet<AttributeClass> newCls = target.GetNewAttributeClasses(source);
                HashSet<AttributeClass> remCls = target.GetRemovedAttributeClasses(source);
                IDictionary<string, string> newAtts = target.GetNewAttributes(source, raw:true);
                HashSet<string> remAtts = target.GetRemovedAttributes(source);

                // Remove, then add classes
                classes.ExceptWith(remCls);
                classes.UnionWith(newCls);

                // Remove, then add attributes
                foreach (string key in remAtts) {
                    attributes.Remove(key);
                }
                SetAttributes(newAtts, notify:false, copy:true);
            }
            if(notify) OnAttributeChanged(EventArgs.Empty);
        }

        public void AddAttributeClass(AttributeClass attClass, bool notify=true) {
            if (attClass != null && !attClass.Equals(this)) {
                classes.Add(attClass);
                // Add classes from attribute class as well (so that all classes are listed)
                HashSet<AttributeClass> classClasses = attClass.GetAttributeClasses();
                foreach (AttributeClass cl in classClasses) {
                    classes.Add(cl);
                }
                // Add attributes from top class only
                SetAttributes(attClass.GetAttributes(), notify);
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
