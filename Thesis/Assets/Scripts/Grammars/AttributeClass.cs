namespace Grammars {
    /// <summary>
    /// A class of attributes that are inherited by every element of said class. They are copied into the element at load time.
    /// Note that an attribute class can also inherit attributes from multiple classes.
    /// </summary>
    public class AttributeClass : AttributedElement {
        string name;

		public AttributeClass(string name) : base() {
            this.name = name;
		}

        public string getName() {
            return name;
        }

        // Setter isn't necessary since we won't be changing the name at runtime
	}
}
