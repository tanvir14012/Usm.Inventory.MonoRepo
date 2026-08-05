# Parsing Algorithms

Reusable expression parsing and tree-building algorithms.

## Example

```csharp
var alg = ParsingAlgorithmsExtensions.CreateBuilder().Build();
var postfix = alg.ShuntingYard("3+4*2/(1-5)");
var result = alg.RecursiveDescentParse("2+3*4");
var expr = alg.BuildExpressionTree("5+3");
```

## Complexity

- Shunting Yard: `O(n)`
- Recursive Descent: `O(n)`
- Expression Tree: `O(n)`
- Postfix Evaluation: `O(n)`
