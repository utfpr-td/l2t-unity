using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LatexMathExpressionRender {
    public static class Parser {
        public static Node parse(string expression) {
            var node = new Node() { expression = expression, op = expression };
            return parse(node);
        }
        private static Node parse(Node n) {
            var fracR = @"[+-]{0,1}\\frac";
            var sqrtR = @"[+-]{0,1}\\sqrt";
            var numbR = @"[+-]{0,1}(\d{1,},\d{1,}|\d{1,})";
            var variR = @"[+-]{0,1}[a-zA-Z]{1}";
            var matchFrac = Regex.Match(n.expression, fracR);
            var matchSqrt = Regex.Match(n.expression, sqrtR);
            var matchNumb = Regex.Match(n.expression, numbR);
            var matchVari = Regex.Match(n.expression, variR);
            if (n.expression[0] == '{') RemoveUnneededBraces(n);
            if (n.expression[0] == '(') ParseParenthesis(n);
            else if (matchFrac.Success && n.expression.IndexOf(matchFrac.ToString()) == 0) ParseFraction(n);
            else if (matchSqrt.Success && n.expression.IndexOf(matchSqrt.ToString()) == 0) ParseRoot(n);
            else if (matchNumb.Success && n.expression.IndexOf(matchNumb.ToString()) == 0) ParseNumber(n);
            else if (matchVari.Success && n.expression.IndexOf(matchVari.ToString()) == 0) ParseVariable(n);
            return n;
        }
        private static void RemoveUnneededBraces(Node n) {
            int indexOp = 0, indexEd = 0;
            string param;
            getParam(0, n.expression, out indexOp, out indexEd, out param);
            n.expression = string.Format("{0}{1}", param, n.expression.Substring(param.Length + 2));
        }
        private static void ParseParenthesis(Node n) {
            string param1, param2, param3, op1, remainingExpression;
            int indexOp1 = 0, indexEd1 = 0, indexOp2 = 0, indexEd2 = 0;
            getParam(0, n.expression, out indexOp1, out indexEd1, out param1, '(', ')');
            if (param1.Length + 2 == n.expression.Length) {
                n.op = "()";
                n.nodes = new List<Node> { parse(new Node { expression = param1, op = param1 }) };
            }
            else if (n.expression[param1.Length + 2] == '^') {
                if (n.expression[param1.Length + 3] == '{') {
                    getParam(param1.Length + 2, n.expression, out indexOp2, out indexEd2, out param2);
                }
                else if (n.expression[param1.Length + 3] == '(') {
                    getParam(param1.Length + 2, n.expression, out indexOp2, out indexEd2, out param2, '(', ')');
                    param2 = string.Format("({0})", param2);
                }
                else {
                    indexOp2 = param1.Length + 2;
                    indexEd2 = param1.Length + 3;
                    param2 = n.expression[param1.Length + 3].ToString();
                }
                if (n.expression.Length > indexEd2 + 1) {
                    switch (GetOperatorInfo(n.expression, indexEd2)) {
                        case OperatorInfo.Explicit:
                            op1 = n.expression[indexEd2 + 1].ToString();
                            param3 = n.expression.Substring(indexEd2 + 2);
                            break;
                        case OperatorInfo.Implicit:
                            op1 = "*";
                            param3 = n.expression.Substring(indexEd2 + 1);
                            break;
                        default: throw new System.Exception("Parse parenthesis power, operator not implemented");
                    }
                    remainingExpression = n.expression.Substring(0, indexEd2 + 1);
                    n.op = op1;
                    n.nodes = new List<Node> {
                        new Node { expression = remainingExpression, op = "^",
                            nodes = new List<Node> {
                                new Node { expression = string.Format("({0})", param1), op = "()",
                                    nodes = new List<Node> {
                                        parse(new Node { expression = param1, op = param1 })
                                    }
                                },
                                parse(new Node { expression = param2, op = param2 })
                            }
                        }
                    };
                    n.nodes.Add(parse(new Node { expression = param3, op = param3 }));
                }
                else {
                    n.op = "^";
                    n.nodes = new List<Node> {
                        new Node { expression = string.Format("({0})", param1), op = "()",
                            nodes = new List<Node> {
                                parse(new Node { expression = param1, op = param1 })
                            }
                        },
                        parse(new Node { expression = param2, op = param2 })
                    };
                }
            }
            else {
                switch (GetOperatorInfo(n.expression, indexEd1)) {
                    case OperatorInfo.Explicit:
                        param2 = n.expression.Substring(param1.Length + 3);
                        n.op = n.expression[param1.Length + 2].ToString();
                        n.nodes = new List<Node> {
                            parse(new Node { expression = param1, op = param1 }), 
                            parse(new Node { expression = param2, op = param2})
                        };
                        break;
                    case OperatorInfo.Implicit:
                        param2 = n.expression.Substring(param1.Length + 2);
                        n.op = "*";
                        n.nodes = new List<Node> {
                            parse(new Node { expression = param1, op = param1 }),
                            parse(new Node { expression = param2, op = param2})
                        };
                        break;
                    default: throw new System.Exception("Parse parenthesis, operator info not implemented");
                }
            }
        }
        private static void ParseVariable(Node n) {
            if (n.expression.Length > 1 && !(n.expression.Length == 2 && (n.expression[0] == '-' || n.expression[0] == '+'))) {
                int indexOp = 0, indexEd = 0, operatorIndex = 0, memberLength = 0;
                bool hasPower = false, baseHasCurlyBrackets = false, powerHasCurlyBrackets = false;
                string op = string.Empty, paramBase = string.Empty, paramPowe = string.Empty, member = string.Empty, remainingExpression = string.Empty;
                if (n.expression[0] == '{') {
                    baseHasCurlyBrackets = true;
                    getParam(0, n.expression, out indexOp, out indexEd, out paramBase);
                    operatorIndex = indexEd + 1;
                }
                else if (n.expression[0] == '-' || n.expression[0] == '+') {
                    paramBase = n.expression.Substring(0, 2);
                    operatorIndex = 2;
                }
                else {
                    paramBase = n.expression[0].ToString();
                    operatorIndex = 1;
                }
                switch (n.expression[operatorIndex]) {
                    case '+':
                    case '-':
                    case '=':
                    case '*':
                        op = n.expression[operatorIndex].ToString();
                        member = n.expression.Substring(0, operatorIndex);
                        remainingExpression = n.expression.Substring(operatorIndex + 1);
                        break;
                    case '^':
                        hasPower = true;
                        if (n.expression[operatorIndex + 1] == '{') {
                            powerHasCurlyBrackets = true;
                            getParam(operatorIndex + 1, n.expression, out indexOp, out indexEd, out paramPowe);
                        }
                        else paramPowe = n.expression[operatorIndex + 1].ToString();
                        break;
                    default:
                        op = "*";
                        n.implicitOp = true;
                        member = n.expression.Substring(0, operatorIndex);
                        remainingExpression = n.expression.Substring(operatorIndex);
                        break;
                }
                if (hasPower) {
                    memberLength = paramBase.Length + paramPowe.Length + 1 + (baseHasCurlyBrackets ? 2 : 0) + (powerHasCurlyBrackets ? 2 : 0);
                    if (n.expression.Length == memberLength) {
                        n.op = "^";
                        n.nodes = new List<Node> {
                            parse(new Node { expression = paramBase, op = paramBase }),
                            parse(new Node { expression = paramPowe, op = paramPowe })
                        };
                    }
                    else {
                        switch (GetOperatorInfo(n.expression, indexEd)) {
                            case OperatorInfo.Explicit:
                                n.op = n.expression[memberLength].ToString();
                                remainingExpression = n.expression.Substring(memberLength + 1);
                                break;
                            case OperatorInfo.Implicit:
                                n.op = "*";
                                n.implicitOp = true;
                                remainingExpression = n.expression.Substring(memberLength);
                                break;
                            default:
                                break;
                        }
                        n.nodes = new List<Node> {
                            new Node{ expression = string.Format("{0}^{1}", paramBase, paramPowe), op = "^", nodes = new List<Node>{
                                parse(new Node { expression = paramBase, op = paramBase }),
                                parse(new Node { expression = paramPowe, op = paramPowe })
                            } },
                            parse(new Node{ expression = remainingExpression, op = remainingExpression })
                        };
                    }
                }
                else {
                    n.op = op;
                    n.nodes = new List<Node> {
                        parse(new Node{ expression = member, op = member }),
                        parse(new Node{ expression = remainingExpression, op = remainingExpression })
                    };
                }
            }
            else {
                n.op = n.expression;
            }
        }
        private static void ParseNumber(Node n) {
            bool hasSign = n.expression[0] == '+' || n.expression[0] == '-';
            var firstNonNumber = Regex.Match(n.expression, @"[^\d]");
            if (hasSign) firstNonNumber = firstNonNumber.NextMatch();
            if (firstNonNumber.Success && n.expression[firstNonNumber.Index] == ',') firstNonNumber = firstNonNumber.NextMatch();
            if (firstNonNumber.Success) {
                if (n.expression[firstNonNumber.Index] == '^') {
                    int indexOp = 0, indexEd = 0;
                    string param;
                    if (n.expression[firstNonNumber.Index + 1] == '{') {
                        getParam(firstNonNumber.Index, n.expression, out indexOp, out indexEd, out param);
                    }
                    else {
                        param = n.expression[firstNonNumber.Index + 1].ToString();
                        indexEd = firstNonNumber.Index + 1;
                    }
                    if (n.expression.Length == indexEd + 1) {
                        n.op = "^";
                        var exp = n.expression.Substring(0, firstNonNumber.Index);
                        n.nodes = new List<Node> {
                            parse(new Node { expression = exp, op = exp  }),
                            parse(new Node { expression = param, op = param  }),
                        };
                    }
                    else {

                        var param1 = n.expression.Substring(0, firstNonNumber.Index);
                        var param2 = param;
                        n.nodes = new List<Node> {
                            new Node { expression = n.expression.Substring(0, indexEd + 1), op = "^",
                                nodes = new List<Node> {
                                    parse(new Node{ expression = param1, op = param1 }),
                                    parse(new Node{ expression = param2, op = param2 }),
                                },
                            }
                        };
                        var operatorInfo = GetOperatorInfo(n.expression, indexEd);
                        string remainder;
                        switch (operatorInfo) {
                            case OperatorInfo.Implicit:
                                remainder = n.expression.Substring(indexEd + 1);
                                n.nodes.Add(parse(new Node { expression = remainder, op = remainder }));
                                n.op = "*";
                                n.implicitOp = true;
                                break;
                            case OperatorInfo.Explicit:
                                remainder = n.expression.Substring(indexEd + 2);
                                n.nodes.Add(parse(new Node { expression = remainder, op = remainder }));
                                n.op = n.expression[indexEd + 1].ToString();
                                break;
                            default:
                                throw new System.Exception("Operator info not implemented");
                        }
                    }
                }
                else SplitExpression(n, firstNonNumber.Index - 1);
            }
            else {
                n.op = n.expression;
            }
        }
        private static void ParseRoot(Node n) {
            int indexOp1 = 0, indexEd1 = 0, indexOp2 = 0, indexEd2 = 0, paramsIndex = 0;
            string param1 = string.Empty, param2 = string.Empty, sign = string.Empty;
            if (n.expression[0] == '+' || n.expression[0] == '-') sign = n.expression[0].ToString();
            paramsIndex = sign.Length + @"\sqrt".Length;
            if (n.expression[paramsIndex] == '{') {
                getParam(0, n.expression, out indexOp1, out indexEd1, out param1);
            }
            else if (n.expression[paramsIndex] == '[') {
                getParam(0, n.expression, out indexOp1, out indexEd1, out param1, '[', ']');
                if (n.expression[indexEd1 + 1] == '{') {
                    getParam(indexEd1, n.expression, out indexOp2, out indexEd2, out param2);
                }
                else {
                    param2 = n.expression[indexEd1 + 1].ToString();
                    indexEd2 = indexEd1 + 1;
                }
            }
            else {
                param1 = n.expression[paramsIndex].ToString();
                indexEd1 = paramsIndex + 1;
            }
            if (indexEd2 == 0) {
                if (indexEd1 == n.expression.Length - 1) {
                    n.op = @"\sqrt";
                    n.sign = sign;
                    n.nodes = new List<Node> { new Node{ expression = param1, op = param1 }, };
                    parse(n.nodes[0]);
                }
                else if (n.expression[indexEd1] == '^') {
                    throw new System.Exception("not implemented \\sqrt{}^power");
                }
                else SplitExpression(n, indexEd1);
            }
            else if (n.expression[indexEd2] == '^') {
                throw new System.Exception("not implemented \\sqrt{}^power");
            }
            else if (indexEd2 == n.expression.Length - 1) {
                n.op = @"\sqrt";
                n.sign = sign;
                n.nodes = new List<Node> { new Node { expression = param2, op = param2 }, new Node { expression = param1, op = param1 }, };
                parse(n.nodes[0]);
                parse(n.nodes[1]);
            }
            else SplitExpression(n, indexEd1);
        }
        private static void ParseFraction(Node n) {
            int indexOp1 = 0, indexEd1 = 0, indexOp2 = 0, indexEd2 = 0;
            string param1 = string.Empty, param2 = string.Empty, sign = string.Empty;
            getParam(0, n.expression, out indexOp1, out indexEd1, out param1);
            if (n.expression[0] == '+' || n.expression[0] == '-') sign = n.expression[0].ToString();
            if (n.expression[indexEd1 + 1] == '{') getParam(indexEd1, n.expression, out indexOp2, out indexEd2, out param2);
            else {
                indexEd2 = indexEd1 + 1;
                param2 = n.expression[indexEd2].ToString();
            }
            if (indexEd2 == n.expression.Length - 1) {
                n.op = @"\frac";
                n.sign = sign;
                n.nodes = new List<Node> {
                    new Node{ expression = param1, op = param1 },
                    new Node{ expression = param2, op = param2 }
                };
                parse(n.nodes[0]);
                parse(n.nodes[1]);
            }
            else if (n.expression[indexEd2 + 1] == '^') {
                string param3 = string.Empty;
                if (n.expression[indexEd2 + 2] == '{') getParam(indexEd2, n.expression, out indexOp1, out indexEd1, out param3);
                else {
                    param3 = n.expression[indexEd2 + 2].ToString();
                    indexEd1 = indexEd2 + 2;
                }

                if (n.expression.Length == indexEd1 + 1) {
                    n.op = "^";
                    var n1 = parse(new Node { expression = param3, op = param3 });
                    n.nodes = new List<Node> {
                        new Node { expression = n.expression.Substring(0, indexEd2 + 1), op = "\\frac", sign = sign,
                            nodes = new List<Node>{
                                parse(new Node{ expression = param1, op = param1 }),
                                parse(new Node{ expression = param2, op = param2 }),
                            }
                        },
                        n1
                    };
                }
                else {
                    n.nodes = new List<Node> {
                    new Node { expression = n.expression.Substring(0, indexEd1 + 1), op = "^",
                        nodes = new List<Node> {
                            new Node { expression = n.expression.Substring(0, indexEd2), op = "\\frac", sign = sign,
                                nodes = new List<Node>{
                                        parse(new Node{ expression = param1, op = param1 }),
                                        parse(new Node{ expression = param2, op = param2 }),
                                    }
                                },
                                parse(new Node { expression = param3, op = param3 })
                            }
                        },
                    };
                    var operatorInfo = GetOperatorInfo(n.expression, indexEd1);
                    string remainder;
                    switch (operatorInfo) {
                        case OperatorInfo.Implicit:
                            remainder = n.expression.Substring(indexEd1 + 1);
                            n.nodes.Add(parse(new Node { expression = remainder, op = remainder }));
                            n.op = "*";
                            n.implicitOp = true;
                            break;
                        case OperatorInfo.Explicit:
                            remainder = n.expression.Substring(indexEd1 + 2);
                            n.nodes.Add(parse(new Node { expression = remainder, op = remainder }));
                            n.op = n.expression[indexEd1 + 1].ToString();
                            break;
                        default:
                            throw new System.Exception("Operator info not implemented");
                    }
                }
            }
            else SplitExpression(n, indexEd2);
        }
        private static void getParam(int startingIndex, string expression, out int indexOp, out int indexEd, out string param, char op = '{', char ed = '}') {
            indexOp = expression.IndexOf(op, startingIndex);
            indexEd = expression.IndexOf(ed, indexOp);
            var curreOp = indexOp;
            while ((curreOp = expression.IndexOf(op, curreOp + 1)) < indexEd && curreOp != -1) {
                indexEd = expression.IndexOf(ed, indexEd + 1);
            }
            param = expression.Substring(indexOp + 1, indexEd - indexOp - 1);
        }
        private static void SplitExpression(Node n, int index) {
            var sqrt = n.expression.Substring(0, index + 1);
            string op = string.Empty;
            string param2 = string.Empty;
            switch (n.expression[index + 1]) {
                case '+':
                case '-':
                case '=':
                case '*':
                    op = n.expression[index + 1].ToString();
                    param2 = n.expression.Substring(index + 2);
                    break;
                default:
                    op = "*";
                    param2 = n.expression.Substring(index + 1);
                    n.implicitOp = true;
                    break;
            }
            n.op = op;
            n.nodes = new List<Node> {
                    new Node{ expression = sqrt, op = sqrt },
                    new Node{ expression = param2, op = param2 }
                };
            parse(n.nodes[0]);
            parse(n.nodes[1]);

        }
        private static OperatorInfo GetOperatorInfo(string expression, int index) {
            switch (expression[index + 1]) {
                case '+':
                case '-':
                case '=':
                case '*':
                    return OperatorInfo.Explicit;
                default:
                    return OperatorInfo.Implicit;
            }

        }
        enum OperatorInfo { Explicit, Implicit };
    }
}
