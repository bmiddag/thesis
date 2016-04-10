using Grammars.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Grammars.Control {
    public class InterGrammarController : Grammar<Task> {
        public override Task Source {
            get {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (taskThread != null && IsTaskThread()) {
                    return CurrentTask;
                } else if (threadTaskDict.ContainsKey(id)) {
                    return threadTaskDict[id];
                } else return null;
            }
            set {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (taskThread != null && IsTaskThread()) {
                    CurrentTask = value;
                } else {
                    threadTaskDict[id] = value;
                }
            }
        }

        Dictionary<int, Task> threadTaskDict;

        public bool IsTaskThread() {
            int id = Thread.CurrentThread.ManagedThreadId;
            return (taskThread.ManagedThreadId == id);
        }

        public InterGrammarController(string name, GrammarRuleSelector ruleSelectionController = null, bool findAllRules = false)
            : base(name,
                  transformerType: typeof(TaskTransformer),
                  ruleSelectionController: ruleSelectionController,
                  findAllRules: findAllRules,
                  threaded: true) {
            threadTaskDict = new Dictionary<int, Task>();
        }

        public override void Update() {
            UnityEngine.MonoBehaviour.print(currentTask);

            if (IsTaskThread()) {
                // Update task
                if (taskQueue.Count > 0) {
                    currentTask = taskQueue.Peek();
                } else return;
            }

            // Select rule
            bool foundRule = SelectRule(new List<Rule<Task>>(rules.Values), ruleSelectionController, findAllRules);
            if (foundRule && selectedRule != null) {
                selectedRule.Apply(Source);
            }
            bool foundAny = foundRule;

            // Check constraints. If any has failed, a rule for that constraint is selected. Otherwise rule selection continues as normal.
            Constraint<Task> selectedConstraint = CheckConstraints();
            List<Constraint<Task>> checkedConstraints = new List<Constraint<Task>>();
            //Random random = new Random();
            while (selectedConstraint != null) {
                bool foundConstraint = SelectRule(new List<Rule<Task>>(selectedConstraint.GetRules().Values), selectedConstraint.Selector, selectedConstraint.FindFirst);
                foundAny = foundAny || foundConstraint;
                if (foundConstraint && selectedRule != null) {
                    selectedRule.Apply(Source);
                    checkedConstraints.Clear(); // List is cleared so this could be an infinite loop if rules are written badly.
                    selectedConstraint = CheckConstraints(checkedConstraints);
                } else {
                    checkedConstraints.Add(selectedConstraint);
                    selectedConstraint = CheckConstraints(checkedConstraints);
                }
            }
            noRuleFound = !foundAny;
            /*bool stop = CheckStopCondition();
            if (stop) {
                Task completedTask = taskQueue.Dequeue();
                SendGrammarEvent("TaskCompleted",
                        replyExpected: false,
                        targets: new string[] { completedTask.Source.Name },
                        parameters: new object[] { completedTask });
            }*/
            Task translatedTask = Source;
            if (IsTaskThread()) {
                taskQueue.Dequeue();
            }
            Source = null;
            SendGrammarEvent(translatedTask);
            iteration++;
        }

        public override void HandleGrammarEvent(Task task) {
            if (task == null) return;
            if (GetTaskProcessor(task.Action) != null) {
                GetTaskProcessor(task.Action).Process(task);
            } else if (task.ReplyExpected) {
                List<AttributedElement> els;
                switch (task.Action) {
                    case "GetElements":
                        els = GetElements(task.GetAttribute("specifier")); // doesn't matter if specifier = null :)
                        task.AddReply(els);
                        break;
                    case "GetStructure":
                        els = GetElements(task.GetAttribute("specifier"));
                        if (els != null && els.Count > 0 && els[0] != null && typeof(StructureModel).IsAssignableFrom(els[0].GetType())) {
                            task.AddReply(els[0]);
                        } else task.AddReply(null);
                        break;
                    default:
                        break;
                }
            } else {
                switch (task.Action) {
                    case "Stop":
                        if (task.Targets.Contains(this)) {
                            lock (taskQueue) {
                                threadStop = true;
                                Monitor.Pulse(taskQueue);
                            }
                        }
                        break;
                    default:
                        lock (taskQueue) {
                            taskQueue.Enqueue(task);
                            Monitor.Pulse(taskQueue);
                        }
                        break;
                }
            }
        }
    }
}
