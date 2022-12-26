using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;
using SpuchSharp.Parsing;

namespace SpuchSharp.Instructions;
internal abstract class Expression : Instruction { }

//declarations
internal abstract class Declaration : Expression { }
internal sealed class Variable : Declaration
{
    public required string Name { get; set; }
    public required Value Value { get; set; }
}

internal sealed class Function : Declaration
{
    public required string Name { get; init; }
    public required FunArg[] Args { get; init; }
    public required Expression[] Block { get; init; }

    public override string ToString()
    {
        StringBuilder sb = new(Name + "(");
        foreach (var arg in Args)
        {
            sb.Append(arg.Stringify() + ", ");
        }
        sb.Remove(sb.Length - 1, 1);
        sb.Append(")");
        return sb.ToString();
    }
}
//assignment
internal sealed class Assignment : Expression 
{
    public required Ident Left { get; set; }
    public required Value Right { get; set; }
}
/// <summary>
/// An expression that evaluates to a value
/// </summary>
/// <example>
/// 2 + 2;
/// a + b;
/// </example>
internal sealed class ValueExpr : Expression
{

}