using SpuchSharp.Parsing;
using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Instructions;

internal abstract class Statement : Instruction { }
internal abstract class Declaration : Statement { }
internal sealed class Variable : Declaration
{
    public required string Name { get; set; }
    public required Value Value { get; set; }
}

internal sealed class Function : Declaration
{
    public required Ident Name { get; init; }
    public required FunArg[] Args { get; init; }
    public required Instruction[] Block { get; init; }

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
internal sealed class Assignment : Statement
{
    public required Ident Left { get; set; }
    public required Expression Expr { get; set; }
}
