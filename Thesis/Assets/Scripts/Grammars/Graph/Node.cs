using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grammars.Graph {
	public class Node {
		Dictionary<string, string> attributes = new Dictionary<string, string>();
		string name;

		public Node(string name) {
			this.name = name;
		}

		public bool hasAttribute(string key) {
			return attributes.ContainsKey(key);
		}

		public string getAttribute(string key) {
			return attributes[key];
		}

		public void setAttribute(string key, string value) {
			attributes[key] = value;
		}
	}
}
