using Usm.Shared.Algorithms.Parsing.Extensions;
using Xunit;

namespace Usm.Shared.Algorithms.Parsing.Tests;

public sealed class ParsingAlgorithmsTests
{
    [Fact]
    public void ShuntingYardConvertsInfixToPostfix()
    {
        var alg = ParsingAlgorithmsExtensions.CreateBuilder().Build();
        var postfix = alg.ShuntingYard("3+4*2/(1-5)");
        Assert.NotEmpty(postfix);
    }

    [Fact]
    public void EvaluatesPostfixExpression()
    {
        var alg = ParsingAlgorithmsExtensions.CreateBuilder().Build();
        var result = alg.EvaluatePostfix("32+4*");
        Assert.Equal(20, result);
    }

    [Fact]
    public void RecursiveDescentParses()
    {
        var alg = ParsingAlgorithmsExtensions.CreateBuilder().Build();
        var result = alg.RecursiveDescentParse("2+3*4");
        Assert.Equal(14, result);
    }

    [Fact]
    public void BuildsExpressionTree()
    {
        var alg = ParsingAlgorithmsExtensions.CreateBuilder().Build();
        var expr = alg.BuildExpressionTree("5+3");
        Assert.NotNull(expr);
    }
}
