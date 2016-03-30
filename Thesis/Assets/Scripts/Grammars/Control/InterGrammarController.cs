using Grammars.Events;
using System;
using System.Collections.Generic;

namespace Grammars.Control {
    public class InterGrammarController : Grammar<Task> {
        public InterGrammarController() {
            throw new NotImplementedException();
        }

        public override void Update() {
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
            throw new NotImplementedException();
        }

        public override void SendGrammarEvent(Task task) {
            throw new NotImplementedException();
        }

        public override List<AttributedElement> GetElements(string specifier = null) {
            throw new NotImplementedException();
        }
    }
}
