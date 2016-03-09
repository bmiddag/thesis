using System.Collections;

namespace Demo {
    public interface IStructureRenderer {
        IElementRenderer CurrentElement {
            get;
        }

        IEnumerator SaveStructure();
        IEnumerator LoadStructure();
    }
}
