﻿using System;
using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Abstract class for any element with attributes (metadata).
    /// </summary>
	public abstract class AttributedElement : IElementContainer {
		protected IDictionary<string, string> attributes;
        protected IDictionary<string, DynamicAttribute> dynamicAttributes;
        protected IDictionary<string, object> objectAttributes;
        protected IDictionary<string, List<AttributedElement>> links;
        protected HashSet<AttributeClass> classes;
        public event EventHandler AttributeChanged;
        protected bool postponeEvents;

        public abstract IElementContainer Container { get; }

        public AttributedElement() {
			attributes = new Dictionary<string, string>();
            dynamicAttributes = new Dictionary<string, DynamicAttribute>();
            objectAttributes = new Dictionary<string, object>();
            classes = new HashSet<AttributeClass>();
            links = new Dictionary<string, List<AttributedElement>>();
            postponeEvents = false;
		}

		public bool HasAttribute(string key) {
			return attributes.ContainsKey(key);
		}

        public bool MatchAttributes(AttributedElement el, Dictionary<string, IElementContainer> otherContainers=null, bool raw = false) {
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
                    // if (!HasAttribute(otherAtt) || GetAttribute(otherAtt, raw) != el.GetAttribute(otherAtt, raw)) {
                    if (GetAttribute(otherAtt, raw) != el.GetAttribute(otherAtt, raw)) {
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

        public string ParseRaw(string rawAttribute) {
            if (rawAttribute.StartsWith("from$")) {
                string elStr = rawAttribute.Substring(5);
                if (elStr.Contains("$")) {
                    // Return that element's attribute
                    string attStr = elStr.Substring(elStr.LastIndexOf("$") + 1);
                    elStr = elStr.Substring(0, elStr.LastIndexOf("$"));
                    List<AttributedElement> els = GetElements(elStr);
                    if (els.Count > 0) {
                        foreach (AttributedElement el in els) {
                            string att = el.GetAttribute(attStr);
                            if (att != null) return att;
                        }
                    }
                    return null;
                } else {
                    // Return whether that element exists
                    List<AttributedElement> els = GetElements(elStr);
                    return (els.Count > 0).ToString();
                }
            } else return rawAttribute;
        }

        public virtual string GetAttribute(string key, bool raw) {
            if (dynamicAttributes.ContainsKey(key) && !raw) {
                return ParseRaw(dynamicAttributes[key].GetAttributeValue());
            } else if (attributes.ContainsKey(key)) {
                if (!raw) return ParseRaw(attributes[key]);
                return attributes[key];
            } else if (key.StartsWith("from$")) {
                return ParseRaw(key);
            } else if (key.StartsWith("_link_")) {
                string rest = key.Substring(6);
                if (links.ContainsKey(rest) && links[rest] != null && links[rest].Count > 0) return "true";
                return "false";
            } else return null;
		}

        public IDictionary<string, List<AttributedElement>> GetLinks() {
            return new Dictionary<string, List<AttributedElement>>(links);
        }

        public bool HasLink(string type) {
            return links.ContainsKey(type) && links[type] != null && links[type].Count > 0;
        }

        public List<AttributedElement> GetLinkedElements(string type) {
            if (HasLink(type)) {
                return new List<AttributedElement>(links[type]);
            } else {
                return new List<AttributedElement>();
            }
        }

        public void AddLink(string type, AttributedElement el) {
            if (links.ContainsKey(type) && links[type] != null) {
                if(!links[type].Contains(el)) links[type].Add(el);
            } else {
                links[type] = new List<AttributedElement>();
                links[type].Add(el);
            }
        }

        public void RemoveLink(string type, AttributedElement el) {
            if (links.ContainsKey(type) && links[type] != null && links[type].Contains(el)) {
                links[type].Remove(el);
            }
        }

        public bool IsLinked(string type, AttributedElement el) {
            return (links.ContainsKey(type) && links[type] != null && links[type].Contains(el));
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
            if (dynamicAttributes.ContainsKey(key)) dynamicAttributes.Remove(key);
            if (objectAttributes.ContainsKey(key)) objectAttributes.Remove(key);
            if (value == null) {
                attributes.Remove(key);
                return;
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

        public bool HasObjectAttribute(string key) {
            return (objectAttributes.ContainsKey(key));
        }

        public virtual object GetObjectAttribute(string key) {
            if (objectAttributes.ContainsKey(key)) {
                return key;
            } else return null;
        }

        public void SetObjectAttribute(string key, object value, bool notify = true) {
            if (value != null && key != null) {
                string stringVal = "$obj$" + value.ToString();
                attributes[key] = stringVal;
                objectAttributes[key] = value;
                if (notify) OnAttributeChanged(EventArgs.Empty);
            }
        }

        public Dictionary<string, object> GetObjectAttributes() {
            return new Dictionary<string, object>(objectAttributes);
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
            // Copy attributes
            foreach (KeyValuePair<string, string> entry in newAtts) {
                if (entry.Key.StartsWith("_grammar_copy")) {
                    StructureModel copyFrom = (StructureModel)Container;
                    string identifier = entry.Value;
                    string match = null;
                    if (el != null && el.Container != null && identifier.StartsWith("(src)")) {
                        copyFrom = (StructureModel)(el.Container);
                        identifier = identifier.Substring(5);
                    }
                    if (identifier.StartsWith("(m:")) { // Copied attributes must contain following substring
                        identifier = identifier.Substring(3);
                        int length = identifier.IndexOf(")");
                        if (length > 0) {
                            match = identifier.Substring(0, length);
                            identifier = identifier.Substring(length+1);
                        }
                    }
                    AttributedElement copyEl = copyFrom.GetElement(identifier);
                    if (copyEl != null) {
                        IDictionary<string, string> copiedAtts = copyEl.GetNewAttributes(this, raw: raw);
                        if (copiedAtts != null) {
                            foreach (KeyValuePair<string, string> copiedEntry in copiedAtts) {
                                if (match == null || copiedEntry.Key.Contains(match)) {
                                    newAtts.Add(copiedEntry);
                                }
                            }
                        }
                    }
                }
            }
            if (!newAtts.ContainsKey("_grammar_keep")) {
                foreach (KeyValuePair<string, string> entry in newAtts) {
                    if (entry.Key.StartsWith("_grammar_")) newAtts.Remove(entry.Key);
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

                // object attributes
                Dictionary<string, object> newObjAtts = target.GetObjectAttributes();
                if (source != null) {
                    foreach (string key in source.GetObjectAttributes().Keys) {
                        newObjAtts.Remove(key);
                    }
                }

                // Remove, then add classes
                classes.ExceptWith(remCls);
                classes.UnionWith(newCls);

                // Remove, then add attributes
                foreach (string key in remAtts) {
                    attributes.Remove(key);
                }

                if (target.Container == Container) {
                    SetAttributes(newAtts, notify: false, copy: false);
                    foreach (KeyValuePair<string, object> obj in newObjAtts) {
                        SetObjectAttribute(obj.Key, obj.Value, notify: false);
                    }
                } else {
                    SetAttributes(newAtts, notify: false, copy: true);
                }
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

        public virtual List<AttributedElement> GetElements(string specifier = null) {
            IElementContainer subcontainer = null;
            string passSpecifier = specifier;
            if (specifier != null && specifier.Contains(".")) {
                string subcontainerStr = specifier.Substring(0, specifier.IndexOf("."));
                if (subcontainerStr.StartsWith("link_")) {
                    string linkSpecifier = subcontainerStr.Substring(5);
                    if (links.ContainsKey(linkSpecifier) && links[linkSpecifier] != null && links[linkSpecifier].Count > 0) {
                        subcontainer = links[linkSpecifier][0];
                    }
                } else if (GetObjectAttribute(specifier) != null && typeof(IElementContainer).IsAssignableFrom(GetObjectAttribute(specifier).GetType())) {
                    subcontainer = (IElementContainer)GetObjectAttribute(specifier);
                }
                if (subcontainer == null) {
                    switch (subcontainerStr) {
                        case "container":
                            subcontainer = Container; break;
                    }
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                List<AttributedElement> attrList = new List<AttributedElement>();
                if (specifier == "links") {
                    foreach (List<AttributedElement> list in links.Values) {
                        foreach (AttributedElement el in list) {
                            attrList.Add(el);
                        }
                    }
                } else if (specifier.StartsWith("links_")) {
                    string linkSpecifier = specifier.Substring(6);
                    if (links.ContainsKey(linkSpecifier) && links[linkSpecifier] != null && links[linkSpecifier].Count > 0) {
                        foreach (AttributedElement el in links[linkSpecifier]) {
                            attrList.Add(el);
                        }
                    }
                } else if (GetObjectAttribute(specifier) != null && typeof(AttributedElement).IsAssignableFrom(GetObjectAttribute(specifier).GetType())) {
                    attrList.Add((AttributedElement)GetObjectAttribute(specifier));
                } else attrList.Add(this);
                return attrList;
            }
        }

        public string this[string att] {
            get { return GetAttribute(att); }
            set { SetAttribute(att, value); }
        }
	}
}
