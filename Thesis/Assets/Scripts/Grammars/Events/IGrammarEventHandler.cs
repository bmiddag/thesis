using System.Collections.Generic;

namespace Grammars.Events {
	public interface IGrammarEventHandler {
        string Name {
            get;
            set;
        }
        void HandleGrammarEvent(Task task);
        List<object> SendGrammarEvent(Task task);
        List<object> SendGrammarEvent(string action, bool replyExpected = false,
            IGrammarEventHandler source = null, string[] targets = null,
            Dictionary<string, string> stringParameters = null, Dictionary<string, object> objectParameters = null);
        void AddListener(IGrammarEventHandler handler);

    }
}
