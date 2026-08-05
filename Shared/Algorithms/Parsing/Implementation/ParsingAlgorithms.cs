using System.Linq.Expressions;
using System.Text;
using Usm.Shared.Algorithms.Parsing.Abstractions;

namespace Usm.Shared.Algorithms.Parsing.Implementation;

/// <summary>
/// Parsing and expression building algorithms.
/// </summary>
public sealed class ParsingAlgorithms : IParsingAlgorithms
{
    /// <inheritdoc />
    public string ShuntingYard(string infix)
    {
        ArgumentNullException.ThrowIfNull(infix);

        var stack = new Stack<char>();
        var output = new StringBuilder();
        var precedence = new Dictionary<char, int> { { '+', 1 }, { '-', 1 }, { '*', 2 }, { '/', 2 }, { '^', 3 } };
        var rightAssoc = new HashSet<char> { '^' };

        foreach (var token in Tokenize(infix))
        {
            if (char.IsDigit(token))
            {
                output.Append(token);
            }
            else if (precedence.ContainsKey(token))
            {
                while (stack.Count > 0 && precedence.ContainsKey(stack.Peek()) &&
                       (precedence[stack.Peek()] > precedence[token] ||
                        (precedence[stack.Peek()] == precedence[token] && !rightAssoc.Contains(token))))
                {
                    output.Append(stack.Pop());
                }
                stack.Push(token);
            }
            else if (token == '(')
            {
                stack.Push(token);
            }
            else if (token == ')')
            {
                while (stack.Count > 0 && stack.Peek() != '(')
                    output.Append(stack.Pop());
                if (stack.Count > 0)
                    stack.Pop();
            }
        }

        while (stack.Count > 0)
            output.Append(stack.Pop());

        return output.ToString();
    }

    /// <inheritdoc />
    public double EvaluatePostfix(string postfix)
    {
        ArgumentNullException.ThrowIfNull(postfix);

        var stack = new Stack<double>();
        foreach (var token in postfix)
        {
            if (char.IsDigit(token))
            {
                stack.Push(token - '0');
            }
            else if ("+-*/^".Contains(token))
            {
                var b = stack.Pop();
                var a = stack.Pop();
                stack.Push(token switch
                {
                    '+' => a + b,
                    '-' => a - b,
                    '*' => a * b,
                    '/' => a / b,
                    '^' => Math.Pow(a, b),
                    _ => throw new ArgumentException($"Unknown operator: {token}")
                });
            }
        }

        return stack.Pop();
    }

    /// <inheritdoc />
    public double RecursiveDescentParse(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        var parser = new RecursiveDescentParser(expr);
        return parser.Parse();
    }

    /// <inheritdoc />
    public Expression BuildExpressionTree(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        var parser = new ExpressionTreeBuilder(expr);
        return parser.Build();
    }

    private static List<char> Tokenize(string infix)
    {
        var tokens = new List<char>();
        foreach (var c in infix)
        {
            if (!char.IsWhiteSpace(c))
                tokens.Add(c);
        }
        return tokens;
    }

    private sealed class RecursiveDescentParser
    {
        private readonly string _expr;
        private int _pos;

        public RecursiveDescentParser(string expr) => _expr = expr;

        public double Parse() => ParseExpression();

        private double ParseExpression()
        {
            var result = ParseTerm();
            while (_pos < _expr.Length && (_expr[_pos] == '+' || _expr[_pos] == '-'))
            {
                var op = _expr[_pos++];
                var right = ParseTerm();
                result = op == '+' ? result + right : result - right;
            }
            return result;
        }

        private double ParseTerm()
        {
            var result = ParseFactor();
            while (_pos < _expr.Length && (_expr[_pos] == '*' || _expr[_pos] == '/'))
            {
                var op = _expr[_pos++];
                var right = ParseFactor();
                result = op == '*' ? result * right : result / right;
            }
            return result;
        }

        private double ParseFactor()
        {
            if (_expr[_pos] == '(')
            {
                _pos++;
                var result = ParseExpression();
                _pos++;
                return result;
            }

            var num = 0.0;
            while (_pos < _expr.Length && char.IsDigit(_expr[_pos]))
                num = num * 10 + (_expr[_pos++] - '0');
            return num;
        }
    }

    private sealed class ExpressionTreeBuilder
    {
        private readonly string _expr;
        private int _pos;

        public ExpressionTreeBuilder(string expr) => _expr = expr;

        public Expression Build() => BuildExpression();

        private Expression BuildExpression()
        {
            var result = BuildTerm();
            while (_pos < _expr.Length && (_expr[_pos] == '+' || _expr[_pos] == '-'))
            {
                var op = _expr[_pos++];
                var right = BuildTerm();
                result = op == '+' ? Expression.Add(result, right) : Expression.Subtract(result, right);
            }
            return result;
        }

        private Expression BuildTerm()
        {
            var result = BuildFactor();
            while (_pos < _expr.Length && (_expr[_pos] == '*' || _expr[_pos] == '/'))
            {
                var op = _expr[_pos++];
                var right = BuildFactor();
                result = op == '*' ? Expression.Multiply(result, right) : Expression.Divide(result, right);
            }
            return result;
        }

        private Expression BuildFactor()
        {
            if (_expr[_pos] == '(')
            {
                _pos++;
                var result = BuildExpression();
                _pos++;
                return result;
            }

            var num = 0.0;
            while (_pos < _expr.Length && char.IsDigit(_expr[_pos]))
                num = num * 10 + (_expr[_pos++] - '0');
            return Expression.Constant(num);
        }
    }
}
