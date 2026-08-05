using Microsoft.Extensions.DependencyInjection;
using Usm.Shared.Patterns.Specification.Abstractions;
using Usm.Shared.Patterns.Specification.Builders;
using Usm.Shared.Patterns.Specification.Configuration;
using Usm.Shared.Patterns.Specification.Extensions;
using Xunit;

namespace Usm.Shared.Patterns.Specification.Tests;

public sealed class SpecificationTests
{
    [Fact]
    public void ComposesSpecificationsWithAndOrNot()
    {
        var adult = Specification<Person>.From(p => p.Age >= 18);
        var active = Specification<Person>.From(p => p.IsActive);
        var spec = adult.And(active.Not());

        Assert.False(spec.IsSatisfiedBy(new Person(25, true)));
        Assert.True(spec.IsSatisfiedBy(new Person(25, false)));
    }

    [Fact]
    public void ConvertsToExpressionAndCompiles()
    {
        var spec = Specification<Person>.From(p => p.Age >= 18);

        var expression = spec.ToExpression();
        var compiled = spec.Compile();

        Assert.True(expression.Compile()(new Person(18, true)));
        Assert.True(compiled(new Person(18, true)));
    }

    [Fact]
    public void FiltersEnumerableWithSpecification()
    {
        var people = new[]
        {
            new Person(17, true),
            new Person(22, true),
            new Person(19, false)
        };

        var adults = people.Where(Specification<Person>.From(p => p.Age >= 18)).ToArray();

        Assert.Equal(2, adults.Length);
    }

    [Fact]
    public void BuilderCreatesComposedSpecification()
    {
        var builder = new SpecificationBuilder<Person>();
        var spec = builder
            .Where(p => p.Age >= 18)
            .And(Specification<Person>.From(p => p.IsActive))
            .Build();

        Assert.True(spec.IsSatisfiedBy(new Person(18, true)));
        Assert.False(spec.IsSatisfiedBy(new Person(18, false)));
    }

    [Fact]
    public async Task EvaluatesAsyncSpecification()
    {
        var spec = Specification<Person>.FromAsync(static async (person, token) =>
        {
            await Task.Delay(1, token);
            return person.Age >= 18;
        });

        var result = await spec.IsSatisfiedByAsync(new Person(18, true));

        Assert.True(result);
        Assert.False(spec.CanEvaluateSynchronously);
    }

    [Fact]
    public async Task FiltersAsyncEnumerable()
    {
        async IAsyncEnumerable<Person> Source()
        {
            yield return new Person(17, true);
            yield return new Person(20, true);
            await Task.CompletedTask;
        }

        var spec = Specification<Person>.From(p => p.Age >= 18);
        var filtered = new List<Person>();

        await foreach (var person in Source().WhereAsync(spec))
            filtered.Add(person);

        Assert.Single(filtered);
    }

    [Fact]
    public void RegistersServicesInDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSpecificationFramework(options => options.CacheCompiledExpressions = true);

        using var provider = services.BuildServiceProvider();
        var compiler = provider.GetRequiredService<ISpecificationCompiler<Person>>();
        var factory = provider.GetRequiredService<ISpecificationFactory<Person>>();

        var spec = factory.From(p => p.Age >= 18);
        var compiled = compiler.Compile(spec);

        Assert.True(compiled(new Person(20, true)));
    }

    private sealed record Person(int Age, bool IsActive);
}
