using System.Linq.Expressions;

namespace Usm.Shared.Algorithms.Parsing.Abstractions;

/// <summary>
/// Represents parsing and expression building algorithms.
/// </summary>
public interface IParsingAlgorithms
{
    /// <summary>Converts infix to postfix using Shunting Yard.</summary>
    string ShuntingYard(string infix);

    /// <summary>Evaluates postfix expression.</summary>
    double EvaluatePostfix(string postfix);

    /// <summary>Parses expression recursively.</summary>
    double RecursiveDescentParse(string expr);

    /// <summary>Builds expression tree from tokens.</summary>
    Expression BuildExpressionTree(string expr);
}
