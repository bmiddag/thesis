using System.Collections.Generic;

namespace Grammars.Events {
    public class Task : StructureModel {
        protected string structureName = null;
        public override string LinkType {
            get { return structureName; }
            set { structureName = value; }
        }

        protected List<IGrammarEventHandler> targets;
        public List<IGrammarEventHandler> Targets {
            get { return new List<IGrammarEventHandler>(targets); }
            set { targets = value; }
        }
        public void AddTarget(IGrammarEventHandler target) {
            targets.Add(target);
        }
        public void RemoveTarget(IGrammarEventHandler target) {
            targets.Remove(target);
        }
        public void ClearTargets() {
            targets.Clear();
        }

        protected IGrammarEventHandler source;
        public IGrammarEventHandler Source {
            get { return source; }
            set { source = value; }
        }

        protected string action;
        public string Action {
            get { return action; }
            set { action = value; }
        }

        protected bool replyExpected;
        public bool ReplyExpected {
            get { return replyExpected; }
            set { replyExpected = value; }
        }
        
        public bool ReplyCompleted {
            get { return replies.Count >= targets.Count; }
        }

        protected List<object> replies;
        public List<object> Replies {
            get { return new List<object>(replies); }
            set { replies = value; }
        }
        public void AddReply(object reply) {
            replies.Add(reply);
        }
        public void RemoveReply(object reply) {
            replies.Remove(reply);
        }
        public void ClearReplies() {
            replies.Clear();
        }

        /*protected List<object> parameters;
        public List<object> Parameters {
            get { return new List<object>(parameters); }
            set { parameters = value; }
        }
        public void AddParameter(object param) {
            parameters.Add(param);
        }
        public void RemoveParameter(object param) {
            parameters.Remove(param);
        }
        public void ClearParameters() {
            parameters.Clear();
        }*/

        public Task(string action = null, IGrammarEventHandler source = null) : base() {
            targets = new List<IGrammarEventHandler>();
            this.action = action;
            this.source = source;
            replyExpected = false;
            replies = new List<object>();
		}

        public override AttributedElement GetElement(string identifier) {
            return this;
        }
    }
}
