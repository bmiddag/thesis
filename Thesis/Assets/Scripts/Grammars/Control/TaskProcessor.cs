using Grammars.Events;
using Grammars.Graphs;
using System.Linq;
using System.Reflection;

namespace Grammars {
    public class TaskProcessor : MethodCaller {
        private IGrammarEventHandler container;
        public IGrammarEventHandler Container {
            get { return container; }
            set { container = value; }
        }

        public TaskProcessor(MethodInfo method, IGrammarEventHandler container = null) : base(method) {
            this.container = container;
        }

        public void Process(Task t) {
            // Check method signature
            int argCount = arguments.Count;
            if (container != null && method != null && method.ReturnType == typeof(void) && method.GetParameters().Count() == 2+argCount) {
                object[] parameters = new object[2 + argCount];
                parameters[0] = container;
                parameters[1] = t;
                for (int i = 0; i < argCount; i++) {
                    parameters[i + 2] = arguments[i];
                }
                method.Invoke(null, parameters);
            }
        }

        public static TaskProcessor FromName(string name, IGrammarEventHandler container) {
            MethodInfo method = typeof(TaskProcessor).GetMethod(name);
            // Check method signature. Has to be static if created from here.
            if (method != null && method.IsStatic && method.ReturnType == typeof(void) && method.GetParameters().Count() >= 2) {
                return new TaskProcessor(method, container);
            } else return null;
        }

        // Example task processor methods are listed below
        
        public static void GraphTraverser_NextElement(IGrammarEventHandler container, Task task) {
            Traverser<Graph> traverser = (Traverser<Graph>)container;
            Graph source = traverser.Source;
            AttributedElement currentElement = traverser.CurrentElement;
        }
    }
}
