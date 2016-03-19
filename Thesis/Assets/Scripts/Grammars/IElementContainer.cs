using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Used for querying by element selector
    /// </summary>
	public interface IElementContainer {
        List<AttributedElement> GetElements(string specifier = null);
    }
}
