using System;
using System.Collections.Generic;
using Grammars.Events;
using System.Threading;

namespace Grammars {
    public class Grammar<T> : AttributedElement, IGrammarEventHandler
        where T : StructureModel {
        protected Dictionary<string, IGrammarEventHandler> listeners;

        protected string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        protected bool paused = false;
        protected bool threadStop = false;
        protected Thread taskThread;
        public Thread TaskThread {
            get { return taskThread; }
            set { taskThread = value; }
        }

        protected T source;
        public virtual T Source {
            get { return source; }
            set {
                if (source != value) {
                    source = value;
                    iteration = 0;
                }
            }
        }

        protected Dictionary<string, Rule<T>> rules;
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
                        Monitor.PulseAll(taskQueue);
                    }
                }
            }
        }

        protected Dictionary<string, TaskProcessor> taskProcessors;
        
        /// <summary>
        /// If all rules sould execute Find (e.g. before checking probabilities), this should be true.
        /// Otherwise, Find should be executed when selecting the rule. If it fails, the rule should not be selected.
        /// </summary>
        protected bool findAllRules;
        public bool FindAllRules {
            get { return findAllRules; }
        }

        protected List<GrammarCondition> stopConditions;
        protected Dictionary<GrammarCondition, string> stopEvents;
        protected GrammarRuleSelector ruleSelectionController = null;
        public GrammarRuleSelector RuleSelector {
            get { return ruleSelectionController; }
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
            get { return noRuleFound; }
        }

        public override IElementContainer Container {
            get { return this; }
        }

        public override string LinkType {
            get { return "grammar"; }
            set { }
        }

        public Grammar(string name, Type transformerType = null, GrammarRuleSelector ruleSelectionController = null,
            bool findAllRules = false, bool threaded = true) {
            this.name = name;
            this.transformerType = transformerType;
            this.ruleSelectionController = ruleSelectionController;
            this.findAllRules = findAllRules;
            stopConditions = new List<GrammarCondition>();
            stopEvents = new Dictionary<GrammarCondition, string>();
            constraints = new Dictionary<string, Constraint<T>>();
            rules = new Dictionary<string, Rule<T>>();
            iteration = 0;
            noRuleFound = false;
            listeners = new Dictionary<string, IGrammarEventHandler>();
            taskQueue = new Queue<Task>();
            taskProcessors = new Dictionary<string, TaskProcessor>();
            if (threaded) {
                taskThread = new Thread(() => Loop(100));
                taskThread.Start();
            } else taskThread = null;
        }

        protected string CheckStopCondition() {
            if (Source == null) return "TaskCompleted";
            if (stopConditions != null && stopConditions.Count > 0) {
                int stop = -1; // index of failed stop condition
                for (int i = 0; i < stopConditions.Count; i++) {
                    if (stopConditions[i].Check()) {
                        stop = i;
                        break;
                    }
                }
                if (stop != -1 || noRuleFound) {
                    if (stop != -1 && stopEvents.ContainsKey(stopConditions[stop])) {
                        return stopEvents[stopConditions[stop]];
                    } else return "TaskCompleted";
                } else return null;
            } else if (noRuleFound) {
                return "TaskCompleted";
            } else return null;
        }

        protected bool SelectRule(List<Rule<T>> ruleSet, GrammarRuleSelector selectionHandler = null, bool findFirst = false) {
            if (Source == null) return false;
            selectedRule = null;
            int ruleIndex = -1;
            int tempRuleIndex = -1;
            foreach (Rule<T> rule in ruleSet) {
                rule.Deselect();
                if (findFirst && rule.CheckCondition()) rule.Find(Source);
            }
            // Make a copy of the rule list without the ones that are certain to fail. 
            List<Rule<T>> tempRules = new List<Rule<T>>();
            foreach (Rule<T> rule in ruleSet) {
                if ((findFirst || rule.CheckCondition()) && (!findFirst || rule.HasSelected())) {
                    tempRules.Add(rule);
                    //UnityEngine.MonoBehaviour.print("[" + Name + "]: Rule matches: " + rule.Name);
                }
            }
            if (tempRules.Count == 0) {
                UnityEngine.MonoBehaviour.print("[" + Name + "]: No rules were matched");
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
                // Test code for evaluating constraints v
                //while (paused) Monitor.Wait(taskQueue);
                /*if (constraints.ContainsKey("constraint3_shortpath") && failedConstraints.Contains(constraints["constraint3_shortpath"])) {
                    paused = true;
                    while (paused) Monitor.Wait(taskQueue);
                    paused = true;
                }*/
                // Test code for evaluating constraints ^
                int index = random.Next(failedConstraints.Count);
                return failedConstraints[index];
            } else {
                return null;
            }
        }

        public virtual void Loop(int wait = 0) {
            try {
                while (!threadStop) {
                    lock (taskQueue) {
                        while (taskQueue.Count == 0 || paused) Monitor.Wait(taskQueue);
                        if(!threadStop) Update();
                    }
                    if (wait > 0) Thread.Sleep(wait);
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogError(e.Message + e.StackTrace);
            }
        }

        public virtual void Update() {
            // Update task
            if (taskQueue.Count > 0 && !paused) {
                currentTask = taskQueue.Peek();
            } else return;

            //UnityEngine.MonoBehaviour.print("[" + Name + "]: Starting update");
            //if (source != null) UnityEngine.MonoBehaviour.print("[" + Name + "]: Source count: " + source.GetElements().Count);
            // Select rule
            bool foundRule = SelectRule(new List<Rule<T>>(rules.Values), ruleSelectionController, findAllRules);
            if (foundRule && selectedRule != null) {
                UnityEngine.MonoBehaviour.print("[" + Name + "]: Applying rule: " + selectedRule.Name);
                selectedRule.Apply(Source);
            }
            bool foundAny = foundRule;

            // Check constraints. If any has failed, a rule for that constraint is selected. Otherwise rule selection continues as normal.
            Constraint<T> selectedConstraint = CheckConstraints();
            List<Constraint<T>> checkedConstraints = new List<Constraint<T>>();
            //Random random = new Random();
            while (selectedConstraint != null) {
                bool foundConstraint = SelectRule(new List<Rule<T>>(selectedConstraint.GetRules().Values), selectedConstraint.Selector, selectedConstraint.FindFirst);
                foundAny = foundAny || foundConstraint;
                if (foundConstraint && selectedRule != null) {
                    //if(random.NextDouble() < 0.4) return;
                    UnityEngine.MonoBehaviour.print("[" + Name + "]: Applying constraint rule: " + selectedRule.Name);
                    selectedRule.Apply(Source);
                    checkedConstraints.Clear(); // List is cleared so this could be an infinite loop if rules are written badly.
                    selectedConstraint = CheckConstraints(checkedConstraints);
                } else {
                    //if (random.NextDouble() < 0.4) return;
                    checkedConstraints.Add(selectedConstraint);
                    selectedConstraint = CheckConstraints(checkedConstraints);
                }
            }
            noRuleFound = !foundAny;
            string stopString = CheckStopCondition();
            bool stop = (stopString != null);
            if (stop) {
                // Transfer control to inter-grammar system
                Task completedTask = taskQueue.Dequeue();
                if (completedTask.Action != "GenerateNext") {
                    completedTask.SetAttribute("completed", "true");
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters.Add("specifier", completedTask);
                    SendGrammarEvent(stopString,
                            replyExpected: false,
                            targets: new string[] { completedTask.Source.Name },
                            objectParameters: parameters);
                } else {
                    lock (completedTask) {
                        completedTask.SetAttribute("completed", "true");
                        Monitor.PulseAll(completedTask);
                    }
                }
            }
            iteration++;
        }

        public void AddRule(Rule<T> rule) {
            rules.Add(rule.Name, rule);
        }

        public void RemoveRule(Rule<T> rule) {
            if (rules.ContainsKey(rule.Name)) {
                rules.Remove(rule.Name);
            }
        }

        public void AddStopCondition(GrammarCondition cond, string eventName=null) {
            stopConditions.Add(cond);
            if (eventName != null && eventName.Trim() != "") {
                stopEvents[cond] = eventName;
            }
        }

        public void RemoveStopCondition(GrammarCondition cond) {
            if (stopConditions.Contains(cond)) {
                stopConditions.Remove(cond);
            }
            if (stopEvents.ContainsKey(cond)) {
                stopEvents.Remove(cond);
            }
        }

        public Dictionary<string, Rule<T>> GetRules() {
            return new Dictionary<string, Rule<T>>(rules); // Return a copy of the rule list
        }

        public Rule<T> GetRule(string key) {
            if (rules.ContainsKey(key)) {
                return rules[key];
            } else return null;
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

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null) {
                switch (key) {
                    case "_iteration":
                        result = iteration.ToString(); break;
                    case "_name":
                        result = name; break;
                    case "_rules_count":
                        result = rules.Count.ToString(); break;
                }
            }
            return result;
        }

        public override List<AttributedElement> GetElements(string specifier = null) {
            IElementContainer subcontainer = null;
            string passSpecifier = specifier;
            /*if (specifier == null || specifier.Trim() == "") {
                subcontainer = Source;
                passSpecifier = null;
            }*/
            if (specifier != null && specifier.Contains(".")) {
                string subcontainerStr = specifier.Substring(0,specifier.IndexOf("."));
                if (listeners.ContainsKey(subcontainerStr)) {
                    passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("specifier", passSpecifier);
                    List<object> replies = SendGrammarEvent("GetElements",
                        replyExpected: true,
                        targets: new string[] { subcontainerStr },
                        stringParameters: parameters);
                    if (replies != null && replies.Count > 0) {
                        return (List<AttributedElement>)replies[0];
                    } else return null;
                } else if (rules.ContainsKey(subcontainerStr)) {
                    subcontainer = rules[subcontainerStr];
                } else {
                    switch (subcontainerStr) {
                        case "task":
                            subcontainer = CurrentTask; break;
                        case "source":
                            subcontainer = Source; break;
                    }
                }
                passSpecifier = specifier.Substring(specifier.IndexOf(".") + 1);
                // Add other possibilities?
            }
            if (subcontainer != null) {
                return subcontainer.GetElements(passSpecifier);
            } else {
                List<AttributedElement> attrList = new List<AttributedElement>();
                if (rules.ContainsKey(specifier)) {
                    attrList.Add(rules[specifier]);
                } else {
                    switch (specifier) {
                        case "task":
                            if(CurrentTask != null) return CurrentTask.GetElements(); break;
                        case "task_structure":
                            if (CurrentTask != null) {
                                attrList.Add(currentTask);
                                return attrList;
                            }
                            break;
                        case "source":
                            if(Source != null) return Source.GetElements(); break;
                        case "source_structure":
                            if (Source != null) {
                                attrList.Add(Source);
                                return attrList;
                            }
                            break;
                    }
                    return base.GetElements(specifier);
                }
                return attrList;
            }
        }

        public virtual void HandleGrammarEvent(Task task) {
            if (task == null) return;
            UnityEngine.MonoBehaviour.print("[" + name + "]" + " Received event: " + task.Action);
            try {
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
                        case "GenerateNext":
                            lock (task) {
                                lock (taskQueue) {
                                    taskQueue.Enqueue(task);
                                    Monitor.PulseAll(taskQueue);
                                }
                                while (!task.HasAttribute("completed") && taskQueue.Count > 0) Monitor.Wait(task);
                            }
                            task.AddReply("completed");
                            break;
                        case "GrammarPause":
                            lock (taskQueue) {
                                paused = !paused;
                                Monitor.PulseAll(taskQueue);
                            }
                            task.AddReply("completed");
                            break;
                        default:
                            break;
                    }
                } else {
                    switch (task.Action) {
                        case "Stop":
                            if (!threadStop) {
                                if (task.Targets.Contains(this)) {
                                    lock (taskQueue) {
                                        threadStop = true;
                                        Monitor.PulseAll(taskQueue);
                                    }
                                }
                                SendGrammarEvent("Stop",
                                    source: this,
                                    targets: new List<string>(listeners.Keys).ToArray());
                            }
                            break;
                        default:
                            lock (taskQueue) {
                                taskQueue.Enqueue(task);
                                Monitor.PulseAll(taskQueue);
                            }
                            break;
                    }
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogError(e.Message + e.StackTrace);
            }
        }

        public virtual List<object> SendGrammarEvent(Task task) {
            if (task == null) return null;
            UnityEngine.MonoBehaviour.print("[" + name + "]" + " Sending event: " + task.Action);
            if (task.Targets.Count == 0) return null;
            List<Thread> startedThreads = new List<Thread>();
            foreach (IGrammarEventHandler target in task.Targets) {
                Thread t = new Thread(() => target.HandleGrammarEvent(task));
                startedThreads.Add(t);
                t.Start();
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
            IGrammarEventHandler source = null, string[] targets = null,
            Dictionary<string, string> stringParameters = null, Dictionary<string, object> objectParameters = null) {
            if (source == null) source = this;
            Task task = new Task(action, source);
            if (targets != null) {
                task.ReplyExpected = replyExpected;
                foreach (string tarStr in targets) {
                    if (listeners.ContainsKey(tarStr)) {
                        IGrammarEventHandler target = listeners[tarStr];
                        if (target != null) {
                            task.AddTarget(target);
                        }
                    }
                }
            }
            if (stringParameters != null) {
                foreach (KeyValuePair<string, string> pair in stringParameters) {
                    task.SetAttribute(pair.Key, pair.Value);
                }
            }
            if (objectParameters != null) {
                foreach (KeyValuePair<string, object> pair in objectParameters) {
                    task.SetObjectAttribute(pair.Key, pair.Value);
                }
            }
            return SendGrammarEvent(task);
        }

        public void AddListener(IGrammarEventHandler handler, string name = null) {
            if (handler == null) return;
            if (name == null) name = handler.Name;
            listeners.Add(name, handler);
        }

        public void AddTaskProcessor(string eventName, TaskProcessor tProc) {
            taskProcessors.Add(eventName, tProc);
        }

        public void RemoveTaskProcessor(string eventName) {
            if (taskProcessors.ContainsKey(eventName)) {
                taskProcessors.Remove(eventName);
            }
        }

        public TaskProcessor GetTaskProcessor(string eventName) {
            if (taskProcessors.ContainsKey(eventName)) {
                return taskProcessors[eventName];
            } else return null;
        }

        public IGrammarEventHandler GetListener(string name) {
            if (listeners.ContainsKey(name)) {
                return listeners[name];
            } else return null;
        }
    }
}
