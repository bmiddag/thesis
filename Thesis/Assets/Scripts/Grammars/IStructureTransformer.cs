using System;
using System.Collections.Generic;
using System.Reflection;

namespace Grammars {
    /// <summary>
    /// Abstract class for transforming a substructure (e.g. subgraph, a group of tiles, etc.) into another structure.
    /// </summary>
	public interface IStructureTransformer<T> where T : StructureModel {
        /// <summary>
        /// The source structure.
        /// </summary>
        T Source {
            get;
            set;
        }

        /// <summary>
        /// Matches parts of the source structure against the query. Stores a list of all matches found.
        /// </summary>
        /// <param name="query">A structure to find within the larger source structure</param>
        /// <returns>True if a substructure matching the query was found, false otherwise.</returns>
        bool Find(T query);

        /// <summary>
        /// Selects one match from the list of matches and tags the selected elements appropriately.
        /// Selection is random by default but can be overwritten with controlledSelection.
        /// </summary>
        /// <param name="controlledSelection">A custom selection handler, returning the index of the selected match</param>
        /// <param name="parameters">Further parameters to pass to the custom selection handler</param>
        void Select(MethodInfo controlledSelection = null, object[] parameters = null);

        /// <summary>
        /// Transforms a substructure into another 
        /// </summary>
        /// <param name="target">The structure that will replace the substructure selected with Select</param>
        void Transform(T target);

        /// <summary>
        /// Removes any changes made by the structure transformer (e.g. adding transformer-specific attributes).
        /// </summary>
        void Destroy();
	}
}
