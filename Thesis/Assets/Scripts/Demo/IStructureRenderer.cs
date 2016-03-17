using Grammars;
using System.Collections;

namespace Demo {
    public interface IStructureRenderer {
        IElementRenderer CurrentElement {
            get;
        }

        StructureModel Source {
            get;
        }

        object Grammar {
            get;
            set;
        }

        IEnumerator SaveStructure();
        IEnumerator LoadStructure();

        IEnumerator GrammarStep();
    }
}
