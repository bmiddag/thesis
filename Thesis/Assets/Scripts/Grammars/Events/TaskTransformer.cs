﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Grammars.Events {
    public class TaskTransformer : IStructureTransformer<Task> {
        Task source = null;
        Task query = null;
        bool findFirst = false;
        public bool FindFirst {
            get { return findFirst; }
            set { findFirst = value; }
        }

        private Rule<Task> rule = null;
        public Rule<Task> Rule {
            get { return rule; }
            set { rule = value; }
        }

        private List<Match> matches = null;
        private Match selectedMatch = null;

        public Task Source {
            get {
                return source;
            }
            set {
                if (source != value) {
                    if (matches != null) {
                        matches.Clear();
                        matches = null;
                    }
                    selectedMatch = null;
                    query = null;
                    source = value;
                }
            }
        }

        public IDictionary<string, AttributedElement> SelectedMatch {
            get {
                Dictionary<string, AttributedElement> dict = new Dictionary<string, AttributedElement>();
                if (source == null || query == null || selectedMatch == null || !selectedMatch.Success) return dict;
                dict.Add("query", source);
                return dict;
            }
        }

        protected Traverser<Task> traverser = null;
        public Traverser<Task> Traverser {
            get { return traverser; }
            set { traverser = value; }
        }

        public TaskTransformer() {
            selectedMatch = null;
            matches = null;
            findFirst = false;
            rule = null;
            traverser = null;
        }

        public void Destroy() {
            Source = null;
        }

        public bool Find(Task query) {
            if (source == null) return false;
            matches = null;
            selectedMatch = null;
            this.query = query;
            string pattern;
            if (query == null || query.Action == null || query.Action.Trim() == "") {
                pattern = @"*";
            } else {
                pattern = query.Action;
            }
            if (query != null && query.Source != null) {
                if (query.Source != source.Source) return false;
            }
            if (!MatchAttributes(source, query)) return false;
            /*if (query != null && query.Parameters.Count > 0) {
                List<object> sourceParams = source.Parameters;
                List<object> queryParams = query.Parameters;
                if (queryParams.Count > sourceParams.Count) return false;
                for (int i = 0; i < queryParams.Count; i++) {
                    if (sourceParams[i] == null) {
                        return false;
                    } else if (queryParams[i] != null && !sourceParams[i].Equals(queryParams[i])) {
                        return false;
                    }
                }
            }*/
            Match match = Regex.Match(source.Action, pattern);
            bool found = false;
            while (match.Success) {
                if (matches == null) matches = new List<Match>();
                found = true;
                matches.Add(match);
                match = match.NextMatch();
            }
            return found;
        }

        public void Select() {
            if (matches == null) return;
            if (matches.Count > 0) {
                int index = -1;
                if (rule != null && rule.MatchSelector != null) {
                    index = rule.MatchSelector.Select(matches);
                }
                if (index == -1) {
                    Random rnd = new Random();
                    index = rnd.Next(matches.Count);
                }
                selectedMatch = matches.ElementAt(index);
            } else {
                selectedMatch = null;
            }
        }

        public void Transform(Task target) {
            if (source == null || target == null || selectedMatch == null) return;

            if (target.Action != null && target.Action.Trim() != "") {
                string queryPattern;
                if (query == null || query.Action == null || query.Action.Trim() == "") {
                    queryPattern = @"(*)";
                } else {
                    queryPattern = query.Action;
                }
                // Selectedmatch is ignored for now -- all matches are replaced
                string newTask = Regex.Replace(source.Action, queryPattern, target.Action);
                source.Action = newTask;
            }
            source.Targets = target.Targets;
            if (target.Source != null) {
                source.Source = target.Source;
            }
            SetAttributesUsingDifference(source, query, target);
            /*if (target.Parameters.Count > 0) {
                List<object> sourceParams = source.Parameters;
                List<object> targetParams = target.Parameters;
                if (query != null) {
                    List<object> extraSourceParams = source.Parameters.GetRange(query.Parameters.Count, source.Parameters.Count - query.Parameters.Count);
                    targetParams.AddRange(extraSourceParams);
                    source.Parameters = targetParams;
                } else {
                    targetParams.AddRange(sourceParams);
                    source.Parameters = targetParams;
                }
            }*/
        }

        protected bool MatchAttributes(AttributedElement source, AttributedElement query) {
            if (source == null || query == null) return false;
            if (rule != null) {
                source.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                source.SetObjectAttribute("rule", rule, notify: false);
                bool match = source.MatchAttributes(query);
                source.RemoveObjectAttribute("grammar", notify: false);
                source.RemoveObjectAttribute("rule", notify: false);
                return match;
            } else return source.MatchAttributes(query);
        }

        protected void SetAttributesUsingDifference(AttributedElement source, AttributedElement query, AttributedElement target) {
            if (source == null || target == null) return;
            if (rule != null) {
                source.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                source.SetObjectAttribute("rule", rule, notify: false);
                source.SetAttributesUsingDifference(query, target, notify: false);
                source.RemoveObjectAttribute("grammar", notify: false);
                source.RemoveObjectAttribute("rule", notify: false);
            } else source.SetAttributesUsingDifference(query, target, notify: false);
        }
    }
}
