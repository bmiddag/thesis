using System;
using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Abstract class for any class representing a complete structure model (e.g. Graph, TileGrid, etc.).
    /// </summary>
	public abstract class StructureModel : AttributedElement, IElementContainer {
        public event EventHandler StructureChanged;

        /// <summary>
        /// Call when there is a change in the structure (e.g. a node/edge is added/removed)
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected void OnStructureChanged(EventArgs e) {
            if (StructureChanged != null) {
                StructureChanged(this, EventArgs.Empty);
            }
        }

        public override IElementContainer Container {
            get {
                return this;
            }
        }

        public abstract List<AttributedElement> GetElements(string specifier = null);
	}
}
