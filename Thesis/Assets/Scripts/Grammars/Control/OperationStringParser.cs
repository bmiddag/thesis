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
