using System;
using System.Collections.Generic;

namespace Grammars {
    /// <summary>
    /// Abstract class for transforming a substructure (e.g. subgraph, a group of tiles, etc.) into another structure.
    /// </summary>
	public interface IStructureTransformer<T> {
        /// <summary>
        /// The source structure.
        /// </summary>
        T Source {
            get;
            set;
        }

        /// <summary>
        /// Matches parts of the source structure against the query.
        /// </summary>
        /// <param name="query">A structure to find within the larger source structure</param>
        /// <returns>True if a substructure matching the query was found, false otherwise.</returns>
        bool Find(T query);

        /// <summary>
        /// Transforms a substructure into another 
        /// </summary>
        /// <param name="target">The structure that will replace the substructure found with Find</param>
        void Transform(T target);

        /// <summary>
        /// Removes any changes made by the structure transformer (e.g. adding transformer-specific attributes).
        /// </summary>
        void Destroy();
	}
}
