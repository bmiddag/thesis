using System;
using System.Collections.Generic;
using Util;

namespace Grammars.Events {
    public class Task : StructureModel {
        protected List<IGrammarEventHandler> targets;
        public List<IGrammarEventHandler> Targets {
            get { return new List<IGrammarEventHandler>(targets); }
            set { targets = value; }
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

        protected List<object> parameters;
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
        }

        public Task(string action = null, IGrammarEventHandler source = null) : base() {
            targets = new List<IGrammarEventHandler>();
            this.action = action;
            this.source = source;
		}

        // Task has no attributed elements, but should be used with grammars anyway
        public override List<AttributedElement> GetElements(string specifier = null) {
            List<AttributedElement> attrList = new List<AttributedElement>();
            return attrList;
        }
    }
}
