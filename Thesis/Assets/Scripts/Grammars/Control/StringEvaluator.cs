using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Text.RegularExpressions;
using Grammars.Events;

namespace Grammars {
    public static class StringEvaluator {
        public static List<AttributedElement> SelectElements(IElementContainer container, string selector) {
            List<AttributedElement> sourceList;
            string fromSelector = null;
            string whereSelector = null;
            if (selector == null || selector.Trim() == "") {
                return new List<AttributedElement>(container.GetElements());
            } else {
                string fromPattern = @"from \[(?<from>.+?)\]";
                string wherePattern = @"where \[(?<where>.+?)\]";
                Match fromMatch = Regex.Match(selector, fromPattern, RegexOptions.IgnoreCase);
                Match whereMatch = Regex.Match(selector, wherePattern, RegexOptions.IgnoreCase);
                if (fromMatch.Success) {
                    fromSelector = fromMatch.Groups["from"].Value.Trim();
                }
                if (whereMatch.Success) {
                    whereSelector = whereMatch.Groups["where"].Value.Trim();
                }
                if (fromSelector == null && whereSelector == null) {
                    whereSelector = selector.Trim();
                }
            }
            sourceList = container.GetElements(fromSelector);
            if (whereSelector == null) {
                return new List<AttributedElement>(sourceList);
            } else {
                return SelectElementsFromList(sourceList, whereSelector);
            }
        }

        public static List<AttributedElement> SelectElementsFromList(List<AttributedElement> sourceList, string selector) {
            // This uses Dynamic LINQ: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
            IQueryable<AttributedElement> filtered = sourceList.AsQueryable().Where(ParseExpression(selector));
            return new List<AttributedElement>(filtered);
        }

        private static string ParseExpression(string expression) {
            // attributes start with # and end with whitespace, #, ), +, -, (, *, or = (previously . was included)
            string expandedExpression = Regex.Replace(expression, @"Has\(#(?<attName>[^\s\#\)\+\-\(\*=]+)\)", "HasAttribute(\"${attName}\")");
            expandedExpression = Regex.Replace(expandedExpression, @"d\(#(?<attName>[^\s\#\)\+\-\(\*=]+)\)",
                "(HasAttribute(\"${attName}\") ? double.Parse(GetAttribute(\"${attName}\")) : -1)");
            expandedExpression = Regex.Replace(expandedExpression, @"#(?<attName>[^\s\#\)\+\-\(\*=]+)", "GetAttribute(\"${attName}\")");
            //UnityEngine.MonoBehaviour.print(expandedExpression);
            return expandedExpression;
        }

        public static string ParseMethodString(string expression, out string[] args) {
            expression = expression.Trim();
            if (expression.StartsWith("(") && expression.EndsWith(")")) {
                while (CheckBalancedParentheses(expression.Substring(1, expression.Length - 2))) {
                    expression = expression.Substring(1, expression.Length - 2).Trim();
                }
            }
            string left = null;
            string right = null;
            Match binaryOperatorMatch = Regex.Match(expression, @"^(?<left>.+)(?<operator>(\|\|)|(&&)|(\+)|(-)|(\*)|(/)|(==))(?<right>.+)$");
            if (binaryOperatorMatch.Success) {
                left = binaryOperatorMatch.Groups["left"].Value.Trim();
                right = binaryOperatorMatch.Groups["right"].Value.Trim();
                string op = binaryOperatorMatch.Groups["operator"].Value.Trim();
                if (CheckBalancedParentheses(left) && CheckBalancedParentheses(right)) {
                    args = new string[] { left, right };
                    if (op == "&&") return "And";
                    if (op == "||") return "Or";
                    if (op == "+") return "Sum";
                    if (op == "-") return "Difference";
                    if (op == "*") return "Multiply";
                    if (op == "/") return "Divide";
                    if (op == "==") return "Equality";
                }
            }
            if (expression.StartsWith("!")) {
                left = expression.Substring(1).Trim();
                args = new string[] { left };
                return "Not";
            }
            if (expression.StartsWith("$")) {
                left = expression.Substring(1).Trim();
                args = new string[] { left };
                return "Constant";
            }
            int currentArgIndex = 0;
            string methodName = "";
            args = null;
            Stack<int> startIndices = new Stack<int>();
            for (int i = 0; i < expression.Length; i++) {
                if (i == 0 || expression[i - 1] != '\\') {
                    if (expression[i] == '(') {
                        if (startIndices.Count == 0) {
                            Match m = Regex.Match(expression.Substring(0, i), @"(\w+)$");
                            if (m.Success) {
                                methodName = m.Value;
                            } else methodName = "";
                            currentArgIndex = i + 1;
                        }
                        startIndices.Push(i);
                    } else if (expression[i] == ')') {
                        if (startIndices.Count == 0) return null; // Brackets don't match
                        int startI = startIndices.Pop();
                        if (startIndices.Count == 0) {
                            if (methodName == "") {
                                string substring = expression.Substring(startI + 1, i - startI - 1);
                                substring = substring.Trim();
                                return ParseMethodString(substring, out args);
                            } else {
                                string arg = expression.Substring(currentArgIndex, i - currentArgIndex).Trim();
                                currentArgIndex = i + 1;
                                if (args == null) {
                                    if (arg == null || arg.Trim() == "") {
                                        args = new string[0];
                                    } else {
                                        args = new string[] { arg };
                                    }
                                } else {
                                    int amount = args.Length;
                                    string[] tempArgs = new string[amount + 1];
                                    for (int j = 0; j < amount; j++) {
                                        tempArgs[j] = args[j];
                                    }
                                    tempArgs[amount] = arg;
                                    args = tempArgs;
                                }
                                return methodName;
                            }
                        }
                    } else if (expression[i] == ',' && startIndices.Count == 1) {
                        string arg = expression.Substring(currentArgIndex, i - currentArgIndex).Trim();
                        currentArgIndex = i + 1;
                        if (args == null) {
                            args = new string[] { arg };
                        } else {
                            int amount = args.Length;
                            string[] tempArgs = new string[amount + 1];
                            for (int j = 0; j < amount; j++) {
                                tempArgs[j] = args[j];
                            }
                            tempArgs[amount] = arg;
                            args = tempArgs;
                        }
                    }
                }
            }
            args = null;
            return null; // Brackets don't match
        }

        private static bool CheckBalancedParentheses(string expression) {
            Stack<char> parentheses = new Stack<char>();
            for (int i = 0; i < expression.Length; i++) {
                if (i == 0 || expression[i - 1] != '\\') {
                    if (expression[i] == '(') {
                        parentheses.Push('(');
                    } else if (expression[i] == ')') {
                        if (parentheses.Count == 0) return false;
                        parentheses.Pop();
                    }
                }
            }
            return (parentheses.Count == 0);
        }

        /// <summary>
        /// A "lite" version of ParseMethodCaller for dynamic attributes, when T is unknown and no grammar/rule is needed.
        /// </summary>
        /// <param name="methodString">a string specifying which method to call with which arguments</param>
        /// <param name="element">the attributed element the attribute belongs to</param>
        /// <param name="attName">the name of the attribute that is dynamic</param>
        /// <returns>a dynamic attribute object matching the arguments</returns>
        public static DynamicAttribute ParseDynamicAttribute(string methodString, AttributedElement element, string attName) {
            string[] args = null;
            string methodName = ParseMethodString(methodString, out args);
            if (methodName == null || methodName.Trim() == "") return null;
            DynamicAttribute caller = DynamicAttribute.FromName(methodName, element, attName);
            int defaultArgs = 2;
            if (caller == null) return null;
            if (caller.Method.GetParameters().Length != args.Length + defaultArgs) return null;
            for (int i = 0; i < args.Length; i++) {
                Type paramType = caller.Method.GetParameters()[i + defaultArgs].ParameterType;
                string arg = args[i].Trim();
                if (typeof(DynamicAttribute).IsAssignableFrom(paramType)) {
                    DynamicAttribute argCall = ParseDynamicAttribute(arg, element, attName);
                    caller.AddArgument(argCall);
                } else if (paramType == typeof(string)) {
                    if (((arg.StartsWith("\"") && arg.EndsWith("\""))) || ((arg.StartsWith("'") && arg.EndsWith("'")))) {
                        arg = arg.Substring(1, arg.Length - 2);
                    }
                    caller.AddArgument(arg);
                } else if (paramType == typeof(int)) {
                    int intArg = int.Parse(arg);
                    caller.AddArgument(intArg);
                } else if (paramType == typeof(double)) {
                    double doubleArg = double.Parse(arg);
                    caller.AddArgument(doubleArg);
                }
            }
            return caller;
        }

        public static MethodCaller ParseMethodCaller<T>(string methodString, Type methodCallerType, Grammar<T> grammar = null, Rule<T> rule = null,
            AttributedElement element = null, string attName = null, IGrammarEventHandler container = null) where T : StructureModel {
            if (!typeof(MethodCaller).IsAssignableFrom(methodCallerType)) return null;
            string[] args = null;
            string methodName = ParseMethodString(methodString, out args);
            if (methodName == null || methodName.Trim() == "") return null;

            if (grammar == null && rule != null) grammar = rule.Grammar;

            MethodCaller caller = null;
            int defaultArgs = 0;
            if (methodCallerType == typeof(GrammarCondition)) {
                caller = GrammarCondition.FromName(methodName, grammar);
                defaultArgs = 1;
            } else if (methodCallerType == typeof(GrammarProbability)) {
                caller = GrammarProbability.FromName(methodName, grammar);
                defaultArgs = 1;
            } else if (methodCallerType == typeof(GrammarRuleSelector)) {
                caller = GrammarRuleSelector.FromName(methodName, grammar);
                defaultArgs = 2;
            } else if (methodCallerType == typeof(RuleCondition)) {
                caller = RuleCondition.FromName(methodName, rule);
                defaultArgs = 1;
            } else if (methodCallerType == typeof(RuleProbability)) {
                caller = RuleProbability.FromName(methodName, rule);
                defaultArgs = 1;
            } else if (methodCallerType == typeof(RuleMatchSelector)) {
                caller = RuleMatchSelector.FromName(methodName, rule);
                defaultArgs = 2;
            } else if (methodCallerType == typeof(DynamicAttribute)) {
                caller = DynamicAttribute.FromName(methodName, element, attName);
                defaultArgs = 2;
            } else if (methodCallerType == typeof(TaskProcessor)) {
                caller = TaskProcessor.FromName(methodName, container);
                defaultArgs = 2;
            }
            if (caller == null) throw new FormatException("Method was not found: " + methodString);
            if (caller.Method.GetParameters().Length != args.Length + defaultArgs) {
                throw new FormatException("Wrong number of arguments was specified: " + methodString +
                    ". Expected " + (caller.Method.GetParameters().Length - defaultArgs) + " arguments.");
            }
            for (int i = 0; i < args.Length; i++) {
                Type paramType = caller.Method.GetParameters()[i + defaultArgs].ParameterType;
                string arg = args[i].Trim();
                if (typeof(MethodCaller).IsAssignableFrom(paramType)) {
                    MethodCaller argCall = ParseMethodCaller(arg, paramType, grammar, rule, element, attName, container);
                    caller.AddArgument(argCall);
                } else if (paramType == typeof(string)) {
                    if (((arg.StartsWith("\"") && arg.EndsWith("\""))) || ((arg.StartsWith("'") && arg.EndsWith("'")))) {
                        arg = arg.Substring(1, arg.Length - 2);
                    }
                    caller.AddArgument(arg);
                } else if (paramType == typeof(int)) {
                    int intArg = int.Parse(arg);
                    caller.AddArgument(intArg);
                } else if (paramType == typeof(double)) {
                    double doubleArg = double.Parse(arg);
                    caller.AddArgument(doubleArg);
                }
            }
            return caller;
        }

        public static bool Compare(string operation, double number1, double number2) {
            string smallCmpOp = operation.ToLowerInvariant();
            switch (smallCmpOp) {
                case "equals":
                case "=":
                case "==":
                    return number1 == number2;
                case "!=":
                    return number1 != number2;
                case ">=":
                    return number1 >= number2;
                case "<=":
                    return number1 <= number2;
                case "greater":
                case ">":
                    return number1 > number2;
                case "smaller":
                case "<":
                    return number1 < number2;
                default:
                    return number1 == number2;
            }
        }

        public static double AggregateAttribute(string operation, List<AttributedElement> elements, string attrName) {
            string smallOp = operation.ToLowerInvariant();
            double result = 0;
            switch (smallOp) {
                case "count":
                    foreach (AttributedElement el in elements) {
                        if (el.HasAttribute(attrName)) {
                            result += 1.0;
                        }
                    }
                    break;
                case "sum":
                    foreach (AttributedElement el in elements) {
                        if (el.HasAttribute(attrName)) {
                            result += double.Parse(el.GetAttribute(attrName));
                        }
                    }
                    break;
                case "avg":
                    double count = 0;
                    foreach (AttributedElement el in elements) {
                        double tmp;
                        if (el.HasAttribute(attrName) && double.TryParse(el.GetAttribute(attrName), out tmp)) {
                            count += 1.0;
                            result += double.Parse(el.GetAttribute(attrName));
                        }
                    }
                    result /= count;
                    break;
                default:
                    result = elements.Count();
                    break;
            }
            return result;
        }
    }
}
