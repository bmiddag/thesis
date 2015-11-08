using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars {
	public abstract class AttributedElement {
		protected IDictionary<string, string> attributes;

		public AttributedElement() {
			attributes = new Dictionary<string, string>();
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
	}
}
