using System.Diagnostics;
using Usm.Shared.Patterns.Specification;
using Usm.Shared.Patterns.Specification.Extensions;

var adult = Specification<Person>.From(p => p.Age >= 18);
var active = Specification<Person>.From(p => p.IsActive);
var spec = adult.And(active);
var candidate = new Person(28, true);

Measure("Compile", 100_000, () => spec.Compile());
Measure("Evaluate", 1_000_000, () => spec.IsSatisfiedBy(candidate));
Measure("Expression", 100_000, () => spec.ToExpression());

static void Measure(string name, int iterations, Action action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();

    for (var i = 0; i < iterations; i++)
        action();

    sw.Stop();
    var after = GC.GetAllocatedBytesForCurrentThread();

    Console.WriteLine($"{name}: {sw.ElapsedMilliseconds} ms, alloc={(after - before):n0} bytes");
}

internal sealed record Person(int Age, bool IsActive);
