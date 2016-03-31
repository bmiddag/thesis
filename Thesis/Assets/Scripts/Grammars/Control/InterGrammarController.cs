using Grammars.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Grammars.Control {
    public class InterGrammarController : Grammar<Task> {
        public override Task Source {
            get { return CurrentTask; }
            set { CurrentTask = value; }
        }

        public InterGrammarController(string name, GrammarRuleSelector ruleSelectionController = null, bool findAllRules = false)
            : base(name,
                  transformerType: typeof(TaskTransformer),
                  ruleSelectionController: ruleSelectionController,
                  findAllRules: findAllRules,
                  threaded: true) {

        }

        public override void Update() {
            UnityEngine.MonoBehaviour.print(currentTask);

            SelectRule(rules, ruleSelectionController, findAllRules);
            if (!noRuleFound && selectedRule != null) {
                selectedRule.Apply(source);
            }


            // Check constraints. If any has failed, a rule for that constraint is selected. Otherwise rule selection continues as normal.
            Constraint<Task> selectedConstraint = CheckConstraints();
            List<Constraint<Task>> checkedConstraints = new List<Constraint<Task>>();
            //Random random = new Random();
            while (selectedConstraint != null) {
                SelectRule(selectedConstraint.GetRules(), selectedConstraint.Selector, selectedConstraint.FindFirst);
                if (!noRuleFound && selectedRule != null) {
                    UnityEngine.MonoBehaviour.print("Rule found");
                    //if(random.NextDouble() < 0.4) return;
                    selectedRule.Apply(source);
                    checkedConstraints.Clear(); // List is cleared so this could be an infinite loop if rules are written badly.
                    selectedConstraint = CheckConstraints(checkedConstraints);
                } else {
                    UnityEngine.MonoBehaviour.print("No rule found");
                    //if (random.NextDouble() < 0.4) return;
                    checkedConstraints.Add(selectedConstraint);
                    selectedConstraint = CheckConstraints(checkedConstraints);
                }
            }
            bool stop = CheckStopCondition();
            if (stop) {
                // TODO: Transfer control to inter-grammar system
                UnityEngine.MonoBehaviour.print("STOP!");
            }
            iteration++;
        }

        public override void HandleGrammarEvent(Task task) {
            if (task == null) return;
            if (task.ReplyExpected) {
                switch (task.Action) {
                    case "GetElements":
                        if (task.Parameters.Count > 0 && task.Parameters[0].GetType() == typeof(string)) {
                            task.AddReply(GetElements((string)task.Parameters[0]));
                        } else {
                            task.AddReply(GetElements());
                        }
                        break;
                    case "Stop":
                        if (task.Targets.Contains(this)) {
                            lock (taskQueue) {
                                threadStop = true;
                                Monitor.Pulse(taskQueue);
                            }
                        }
                        break;
                    default:
                        break;
                }
            } else {
                switch (task.Action) {
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
