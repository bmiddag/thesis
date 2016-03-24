using System;
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
            if (whereSelector == null) {
                return new List<AttributedElement>(sourceList);
            } else {
                // This uses Dynamic LINQ: http://weblogs.asp.net/scottgu/dynamic-linq-part-1-using-the-linq-dynamic-query-library
                IQueryable<AttributedElement> filtered = sourceList.AsQueryable().Where(selector);
                return new List<AttributedElement>(filtered);
            }
        }

        private static string ParseExpression(AttributedElement el, string expression) {
            return "";
        }

        private static bool ParseBooleanExpression(AttributedElement el, string expression) {
            expression = expression.Trim();
            if (expression.StartsWith("(") && expression.EndsWith(")")) {
                ParseBooleanExpression(el, expression.Substring(1, expression.Length - 2));
            }
            string comparisonPattern = @"(?<left.+>)(?<operator>(<=|>=|!=|==|=|>|<))(?<right.+>)";
            Match comparisonMatch = Regex.Match(expression, comparisonPattern, RegexOptions.IgnoreCase);
            if (comparisonMatch.Success) {
                return CompareStringExpressions(el, fromMatch.Groups["from"].Value.Trim();
            }
            return "";
        }

        private static bool CompareStringExpressions(AttributedElement el, string operation, string left, string right) {
            string leftResult = ParseExpression(el, left);
            string rightResult = ParseExpression(el, right);
            double lD, rD;
            bool lDbool = double.TryParse(leftResult, out lD);
            bool rDbool = double.TryParse(rightResult, out rD);

            string smallCmpOp = operation.ToLowerInvariant();
            switch (smallCmpOp) {
                case "equals":
                case "=":
                case "==":
                    if (lDbool && rDbool) {
                        return Compare(operation, lD, rD);
                    } else return leftResult == rightResult;
                case "!=":
                    if (lDbool && rDbool) {
                        return Compare(operation, lD, rD);
                    } else return leftResult != rightResult;
                default:
                    if (lDbool && rDbool) {
                        return Compare(operation, lD, rD);
                    } else return false;
            }
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
