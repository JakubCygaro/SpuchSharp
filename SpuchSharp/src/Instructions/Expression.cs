using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;
using SpuchSharp.Parsing;
using System.Text.Json.Serialization;

namespace SpuchSharp.Instructions;

internal abstract class Expression : Instruction 
{
    public abstract string Display();
    /// <summary>
    /// Ensures that all expressions in a complex expression are of expression type <c>T</c>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="complexExpression"></param>
    /// <returns></returns>
    public static bool EnsureAll<T>(ComplexExpression complexExpression)
        where T : Expression
    {
        if (complexExpression is not T)
            return false;
        if (complexExpression.Left is ComplexExpression complexLeft)
            return EnsureAll<T>(complexLeft);
        if (complexExpression.Right is ComplexExpression complexRight)
            return EnsureAll<T>(complexRight);
        return true;
    }

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
    public required Expression Left { get; init; } 
    public required Expression Right { get; init; }
    public static ComplexExpression From(
        Expression left,
        Operator op,
        Expression expr) => op switch
        {
            ValOperator vop => vop switch
            {
                Add => new AddExpr { Left = left, Right = expr },
                Sub => new SubExpr { Left = left, Right = expr },
                Mult => new MulExpr { Left = left, Right = expr },
                Div => new DivExpr { Left = left, Right = expr },
                _ => throw new System.Diagnostics.UnreachableException()
            },
            LogicOperator lop => lop switch
            {
                And => new AndExpr { Left = left, Right = expr },
                Or => new OrExpr { Left = left, Right = expr },
                Equality => new EqExpr { Left = left, Right = expr },
                InEquality => new InEqExpr { Left = left, Right = expr },
                Greater => new GreaterThanExpr { Left = left, Right = expr },
                Less => new LessThanExpr { Left = left, Right = expr },
                GreaterOrEq => new GreaterOrEqToExpr { Left = left, Right = expr },
                LessOrEq => new LessOrEqToExpr { Left = left, Right = expr },
                _ => throw new System.Diagnostics.UnreachableException()
            },
            _ => throw new ParserException("Unrecognized expression type? wtf?")
        };
}

abstract class ArthmetricExpression : ComplexExpression { }
sealed class AddExpr : ArthmetricExpression
{
    public override string Display() => $"[{Left.Display()} + {Right.Display()}]";
}
sealed class SubExpr : ArthmetricExpression
{
    public override string Display() => $"[{Left.Display()} - {Right.Display()}]";
}
sealed class MulExpr : ArthmetricExpression
{
    public override string Display() => $"[{Left.Display()} * {Right.Display()}]";
}
sealed class DivExpr : ArthmetricExpression
{
    public override string Display() => $"[{Left.Display()} / {Right.Display()}]";
}
abstract class LogicalExpression : ComplexExpression { }
sealed class AndExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} && {Right.Display()}]";
}
sealed class OrExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} || {Right.Display()}]";
}
sealed class EqExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} == {Right.Display()}]";
}
sealed class InEqExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} != {Right.Display()}]";
}
sealed class GreaterThanExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} > {Right.Display()}]";
}
sealed class LessThanExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} < {Right.Display()}]";
}
sealed class GreaterOrEqToExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} >= {Right.Display()}]";
}
sealed class LessOrEqToExpr : LogicalExpression
{
    public override string Display() => $"[{Left.Display()} <= {Right.Display()}]";
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
    public override string Display() => $"[{Val.Stringify()}]";
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
    public override string Display()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"[{Function.Stringify()}]");
        sb.Append('(');
        foreach(var expr in Args)
        {
            sb.Append(expr.Display() + " | ");
        }
        sb.Append(')');
        return sb.ToString();
    }
    public required Ident Function { get; init; }
    public required Expression[] Args { get; init; }
}
internal sealed class IdentExpression : SimpleExpression
{
    public override string Display() => $"[{Ident.Stringify()}]";
    public required Ident Ident { get; init; }
}
internal sealed class IndexerExpression : SimpleExpression
{
    public override string Display() => $"[{Ident.Stringify()}[{IndexExpression.Display()}]]";
    public required Ident Ident { get; init; }
    public required Expression IndexExpression { get; init; }
}

