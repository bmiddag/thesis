namespace Grammars.Events {
    // A delegate type for hooking up change notifications.
    public delegate void GrammarEventHandler(Task task);

	public interface IGrammarEventHandler {
        void HandleGrammarEvent(Task task);
        void SendGrammarEvent(Task task);
	}
}
