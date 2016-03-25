﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace Grammars {
    public static class OperationStringParser {
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
            //UnityEngine.MonoBehaviour.print(sourceList.Count);
            //UnityEngine.MonoBehaviour.print(whereSelector);
            if (whereSelector == null) {
                return new List<AttributedElement>(sourceList);
            } else {
                // This uses Dynamic LINQ: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
                IQueryable<AttributedElement> filtered = sourceList.AsQueryable().Where(ParseExpression(whereSelector));
                return new List<AttributedElement>(filtered);
            }
        }

        private static string ParseExpression(string expression) {
            // attributes start with # and end with whitespace, #, ), +, -, (, *, ., or =
            string expandedExpression = Regex.Replace(expression, @"Has\(#(?<attName>[^\s\#\)\+\-\(\*\.=]+)\)", "HasAttribute(\"${attName}\")");
            expandedExpression = Regex.Replace(expression, @"d\(#(?<attName>[^\s\#\)\+\-\(\*\.=]+)\)",
                "(HasAttribute(\"${attName}\") ? double.Parse(GetAttribute(\"${attName}\")) : -1)");
            expandedExpression = Regex.Replace(expandedExpression, @"#(?<attName>[^\s\#\)\+\-\(\*\.=]+)", "GetAttribute(\"${attName}\")");
            UnityEngine.MonoBehaviour.print(expandedExpression);
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
            Match andOrMatch = Regex.Match(expression, @"^(<?left>.+)(<?operator>(\|\|)|(&&))(<?right>.+)$");
            if (andOrMatch.Success) {
                left = andOrMatch.Groups["left"].Value.Trim();
                right = andOrMatch.Groups["right"].Value.Trim();
                string op = andOrMatch.Groups["operator"].Value.Trim();
                if (CheckBalancedParentheses(left) && CheckBalancedParentheses(right)) {
                    args = new string[] { left, right };
                    if (op == "&&") return "And";
                    if (op == "||") return "Or";
                }
            }
            if (expression.StartsWith("!")) {
                left = expression.Substring(1).Trim();
                args = new string[] { left };
                return "Not";
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
                                if (args == null) args = new string[0];
                                return methodName;
                            }
                        }
                    } else if (expression[i] == ',' && startIndices.Count == 1) {
                        string arg = expression.Substring(currentArgIndex, i - currentArgIndex - 1).Trim();
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
