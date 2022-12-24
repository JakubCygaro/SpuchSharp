using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;

namespace SpuchSharp.Instructions;

internal abstract class Instruction { }

internal abstract class Declaration : Instruction { }

internal sealed class Variable : Declaration
{
    public required string Name { get; set; }
    public required Value Value { get; set; }
}

internal sealed class Function : Declaration
{
    public required string Name { get; init; }
    public required Ty[] Args { get; init; }
    public required Expression[] Block { get; init; }

    public override string ToString()
    {
        StringBuilder sb = new(Name + "(");
        foreach (var arg in Args)
        {
            sb.Append(arg.Ident + ", ");
        }
        sb.Remove(sb.Length - 1, 1);
        sb.Append(")");
        return sb.ToString();
    }
}