using System;
using System.Collections.Generic;
using Grammars.Events;
using System.Threading;

namespace Grammars {
    public class Grammar<T> : IElementContainer, IGrammarEventHandler
        where T : StructureModel {
        protected Dictionary<string, IGrammarEventHandler> listeners;

        protected string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        protected bool threadStop = false;
        protected Thread taskThread;
        public Thread TaskThread {
            get { return taskThread; }
            set { taskThread = value; }
        }

        protected T source;
        public virtual T Source {
            get {
                return source;
            }
            set {
                if (source != value) {
                    source = value;
                    iteration = 0;
                }
            }
        }

        protected List<Rule<T>> rules;
        protected Rule<T> selectedRule = null;

        protected Dictionary<string, Constraint<T>> constraints;

        protected Type transformerType;
        public IStructureTransformer<T> Transformer {
            get {
                if (transformerType != null) {
                    return (IStructureTransformer<T>)(Activator.CreateInstance(transformerType));
                } else return null;
            }
            set {
                if (value != null) {
                    transformerType = value.GetType();
                } else {
                    transformerType = null;
                }
            }
        }

        protected Queue<Task> taskQueue = null;
        protected Task currentTask = null;
        public Task CurrentTask {
            get { return currentTask; }
            set {
                lock(taskQueue) {
                    if (!taskQueue.Contains(value) && value != null) {
                        taskQueue.Enqueue(value);
                        Monitor.Pulse(taskQueue);
                    }
                }
            }
        }
        
        /// <summary>
        /// If all rules sould execute Find (e.g. before checking probabilities), this should be true.
        /// Otherwise, Find should be executed when selecting the rule. If it fails, the rule should not be selected.
        /// </summary>
        protected bool findAllRules;
        public bool FindAllRules {
            get {
                return findAllRules;
            }
        }

        protected List<GrammarCondition> stopConditions;
        protected GrammarRuleSelector ruleSelectionController = null;
        public GrammarRuleSelector RuleSelector {
            get {
                return ruleSelectionController;
            }
            set {
                if (value != ruleSelectionController) {
                    iteration = 0;
                    noRuleFound = false;
                    ruleSelectionController = value;
                }
            }
        }
        protected int iteration;

        /// <summary>
        /// If no rule was found during this iteration, this should be marked true.
        /// </summary>
        protected bool noRuleFound;
        public bool NoRuleFound {
            get {
                return noRuleFound;
            }
        }

        public Grammar(string name, Type transformerType = null, GrammarRuleSelector ruleSelectionController = null,
            bool findAllRules = false, bool threaded = true) {
            this.name = name;
            this.transformerType = transformerType;
            this.ruleSelectionController = ruleSelectionController;
            this.findAllRules = findAllRules;
            stopConditions = new List<GrammarCondition>();
            constraints = new Dictionary<string, Constraint<T>>();
            rules = new List<Rule<T>>();
            iteration = 0;
            noRuleFound = false;
            listeners = new Dictionary<string, IGrammarEventHandler>();
            if (threaded) {
                taskThread = new Thread(() => Loop(1000));
                taskThread.Start();
            } else taskThread = null;
        }

        protected bool CheckStopCondition() { // TODO: Change to int?
            if (Source == null) return true;
            if (stopConditions != null && stopConditions.Count > 0) {
                int stop = -1; // index of failed stop condition
                for (int i = 0; i < stopConditions.Count; i++) {
                    if (stopConditions[i].Check()) {
                        stop = i;
                        break;
                    }
                }
                if (stop != -1) {
                    return true;
                } else return false;
            } else {
                return noRuleFound;
            }
        }

        protected bool SelectRule(List<Rule<T>> ruleSet, GrammarRuleSelector selectionHandler = null, bool findFirst = false) {
            if (Source == null) return false;
            selectedRule = null;
            int ruleIndex = -1;
            int tempRuleIndex = -1;
            foreach (Rule<T> rule in ruleSet) {
                rule.Deselect();
                if(findFirst) rule.Find(Source);
            }
            // Make a copy of the rule list without the ones that are certain to fail. 
            List<Rule<T>> tempRules = new List<Rule<T>>();
            foreach (Rule<T> rule in ruleSet) {
                if(rule.CheckCondition() && (!findFirst || rule.HasSelected())) {
                    tempRules.Add(rule);
                }
            }
            if (tempRules.Count == 0) {
                return false;
            }

            // Controlled rule selection
            if (selectionHandler != null) {
                tempRuleIndex = ruleSelectionController.Select(new List<Rule<T>>(tempRules));
                if (tempRuleIndex != -1) { // Get index of rule in complete rule list
                    Rule<T> rule = tempRules[tempRuleIndex];
                    ruleIndex = ruleSet.IndexOf(rule);
                    if (ruleIndex < 0 || ruleIndex >= ruleSet.Count) ruleIndex = -1;
                }
            }
            if (ruleIndex == -1) {
                // Default rule selection using probabilities. Is also used as fallback in case controlled rule selection doesn't work.
                GrammarRuleSelector defaultRuleSelection = GrammarRuleSelector.FromName("ProbabilityRuleSelection", this);
                tempRuleIndex = defaultRuleSelection.Select(new List<Rule<T>>(tempRules));
                if (tempRuleIndex != -1) { // Get index of rule in complete rule list
                    Rule<T> rule = tempRules[tempRuleIndex];
                    ruleIndex = ruleSet.IndexOf(rule);
                    if (ruleIndex < 0 || ruleIndex >= ruleSet.Count) ruleIndex = -1;
                }
            }

            if (ruleIndex != -1) {
                selectedRule = ruleSet[ruleIndex];
                return true;
            } else return false;
        }

        protected Constraint<T> CheckConstraints(List<Constraint<T>> checkedConstraints = null) {
            bool prioritize = false;
            Random random = new Random();
            List<Constraint<T>> failedConstraints = new List<Constraint<T>>();

            foreach (KeyValuePair<string, Constraint<T>> constraintPair in constraints) {
                Constraint<T> constraint = constraintPair.Value;
                if (checkedConstraints != null && checkedConstraints.Contains(constraint)) continue;
                // Calculate probability, but only if a rule was applied before this.
                if (checkedConstraints == null || checkedConstraints.Count == 0) constraint.GetProbability(true);
                if (constraint.Probability > 0) { // Constraint failed
                    if (constraint.Probability >= 1) {
                        if (!prioritize) {
                            failedConstraints.Clear();
                            prioritize = true;
                        }
                        failedConstraints.Add(constraint);
                    } else if (!prioritize) {
                        double randomProbability = random.NextDouble();
                        if (randomProbability < constraint.Probability) {
                            failedConstraints.Add(constraint);
                        }
                    }
                }
            }
            if (failedConstraints.Count > 0) {
                int index = random.Next(failedConstraints.Count);
                return failedConstraints[index];
            } else {
                return null;
            }
        }

        public virtual void Loop(int wait = 0) {
            while (!threadStop) {
                lock (taskQueue) {
                    while(taskQueue.Count == 0) Monitor.Wait(taskQueue);
                    Update();
                }
                if (wait > 0) Thread.Sleep(wait);
            }
        }

        public virtual void Update() {
            // Update task
            if (taskQueue.Count > 0) {
                currentTask = taskQueue.Peek();
            } else return;

            // Select rule
            bool foundRule = SelectRule(rules, ruleSelectionController, findAllRules);
            if (foundRule && selectedRule != null) {
                selectedRule.Apply(Source);
            }
            bool foundAny = foundRule;

            // Check constraints. If any has failed, a rule for that constraint is selected. Otherwise rule selection continues as normal.
            Constraint<T> selectedConstraint = CheckConstraints();
            List<Constraint<T>> checkedConstraints = new List<Constraint<T>>();
            //Random random = new Random();
            while (selectedConstraint != null) {
                bool foundConstraint = SelectRule(selectedConstraint.GetRules(), selectedConstraint.Selector, selectedConstraint.FindFirst);
                foundAny = foundAny || foundConstraint;
                if (foundConstraint && selectedRule != null) {
                    UnityEngine.MonoBehaviour.print("Rule found");
                    //if(random.NextDouble() < 0.4) return;
                    selectedRule.Apply(Source);
                    checkedConstraints.Clear(); // List is cleared so this could be an infinite loop if rules are written badly.
                    selectedConstraint = CheckConstraints(checkedConstraints);
                } else {
                    UnityEngine.MonoBehaviour.print("No rule found");
                    //if (random.NextDouble() < 0.4) return;
                    checkedConstraints.Add(selectedConstraint);
                    selectedConstraint = CheckConstraints(checkedConstraints);
                }
            }
            noRuleFound = !foundAny;
            bool stop = CheckStopCondition();
            if (stop) {
                // Transfer control to inter-grammar system
                Task completedTask = taskQueue.Dequeue();
                SendGrammarEvent("TaskCompleted",
                        replyExpected: false,
                        targets: new string[] { completedTask.Source.Name },
                        parameters: new object[] { completedTask });
                //UnityEngine.MonoBehaviour.print("STOP!");
            }
            iteration++;
        }

        public void AddRule(Rule<T> rule) {
            rules.Add(rule);
        }

        public void RemoveRule(Rule<T> rule) {
            if (rules.Contains(rule)) {
                rules.Remove(rule);
            }
        }

        public void AddStopCondition(GrammarCondition cond) {
            stopConditions.Add(cond);
        }

        public void RemoveStopCondition(GrammarCondition cond) {
            if (stopConditions.Contains(cond)) {
                stopConditions.Remove(cond);
            }
        }

        public List<Rule<T>> GetRules() {
            return new List<Rule<T>>(rules); // Return a copy of the rule list
        }

        public void AddConstraint(string name, Constraint<T> constraint) {
            constraints.Add(name, constraint);
        }

        public void RemoveConstraint(Constraint<T> constraint) {
            if (constraint == null) return;
            if (constraints.ContainsKey(constraint.Name) && constraints[constraint.Name] == constraint) {
                constraints.Remove(constraint.Name);
            }
        }

        public void RemoveConstraint(string name) {
            if (name == null) return;
            if (constraints.ContainsKey(name)) constraints.Remove(name);
        }

        public Dictionary<string, Constraint<T>> GetConstraints() {
            return new Dictionary<string, Constraint<T>>(constraints);
        }

        public virtual List<AttributedElement> GetElements(string specifier = null) {
            IElementContainer subcontainer = Source;
            string passSpecifier = specifier;
            if (specifier != null && specifier.Contains(".")) {
                string subcontainerStr = specifier.Substring(0,specifier.IndexOf("."));
                if (listeners.ContainsKey(subcontainerStr)) {
                    passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                    List<object> replies = SendGrammarEvent("GetElements",
                        replyExpected: true,
                        targets: new string[] { subcontainerStr },
                        parameters: new object[] { passSpecifier });
                    if (replies != null && replies.Count > 0) {
                        return (List<AttributedElement>)replies[0];
                    } else return null;
                }
                switch (subcontainerStr) {
                    case "source":
                    default:
                        subcontainer = Source; break;
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                return new List<AttributedElement>();
            }
        }

        public virtual void HandleGrammarEvent(Task task) {
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
                        lock(taskQueue) {
                            taskQueue.Enqueue(task);
                            Monitor.Pulse(taskQueue);
                        }
                        break;
                }
            }
        }

        public virtual List<object> SendGrammarEvent(Task task) {
            if (task == null) return null;
            if (task.Targets.Count == 0) return null;
            List<Thread> startedThreads = new List<Thread>();
            foreach (IGrammarEventHandler target in task.Targets) {
                startedThreads.Add(new Thread(() => target.HandleGrammarEvent(task)));
            }
            if (!task.ReplyExpected) {
                return null;
            } else {
                foreach (Thread thread in startedThreads) {
                    thread.Join();
                }
                if (task.ReplyCompleted) {
                    return task.Replies;
                } else return null;
            }
        }

        public List<object> SendGrammarEvent(string action, bool replyExpected = false,
            IGrammarEventHandler source = null, string[] targets = null, object[] parameters = null) {
            if (source == null) source = this;
            Task task = new Task(action, source);
            if (targets != null) {
                task.ReplyExpected = replyExpected;
                foreach (string tarStr in targets) {
                    IGrammarEventHandler target = listeners[tarStr];
                    if (target != null) {
                        task.AddTarget(target);
                    }
                }
            }
            return SendGrammarEvent(task);
        }

        public void AddListener(IGrammarEventHandler handler) {
            if (handler == null) return;
            listeners.Add(handler.Name, handler);
        }
    }
}
