using System.Collections.Generic;
using System.Reflection;

namespace Grammars {
    public abstract class MethodCaller {
        protected MethodInfo method;
        public MethodInfo Method {
            get { return method; }
            set { method = value; }
        }

        protected List<object> arguments;
        public void AddArgument(object arg) {
            arguments.Add(arg);
        }

        public MethodCaller(MethodInfo method = null) {
            this.method = method;
            arguments = new List<object>();
        }
    }
}
