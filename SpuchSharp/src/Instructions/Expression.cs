using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;
using SpuchSharp.Parsing;

namespace SpuchSharp.Instructions;

internal abstract class Expression : Instruction 
{
    public abstract string Display();

}
/// <summary>
/// An Expression that consists of two expressions with an operator in between
/// </summary>
/// <example>
/// <code>
/// 2 + 2 - 3 * 6;
/// </code>
/// </example>
internal abstract class ComplexExpression : Expression 
{
    public override string Display() => $"{Left.Display()} {Op} {Expr.Display()}";
    public required SimpleExpression Left { get; init; } 
    public required Operator Op { get; init; }
    public required Expression Expr { get; init; }
}

internal abstract class SimpleExpression : Expression { }
/// <summary>
/// An expression that basically is a single value
/// </summary>
/// <example>
/// <code>
/// 2;
/// </code>
/// </example>
internal sealed class ValueExpression : SimpleExpression
{
    public override string Display() => $"{Val.Stringify()}";
    public required Value Val { get; init; }
}
/// <summary>
/// An expression that calls a function
/// </summary>
/// <example>
/// <code>
/// bar();
/// </code>
/// </example>
internal sealed class CallExpression : SimpleExpression
{
    public override string Display() => $"{Function.Stringify()}";
    public required Ident Function { get; init; }
    public required Expression[] Args { get; init; }
}
internal sealed class IdentExpression : SimpleExpression
{
    public override string Display() => $"{Ident.Stringify()}";
    public required Ident Ident { get; init; }
}

