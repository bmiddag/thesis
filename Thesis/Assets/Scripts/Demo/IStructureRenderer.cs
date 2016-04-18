using Grammars;
using Grammars.Events;
using System.Collections;

namespace Demo {
    public interface IStructureRenderer {
        IElementRenderer CurrentElement {
            get;
        }

        StructureModel Source {
            get;
        }

        IGrammarEventHandler Grammar {
            get;
            set;
        }

        string Name {
            get;
        }

        IEnumerator SaveStructure();
        IEnumerator LoadStructure();

        IEnumerator GrammarStep();
    }
}
