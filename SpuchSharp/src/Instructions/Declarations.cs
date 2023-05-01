using SpuchSharp.Parsing;
using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Instructions;

internal abstract class Declaration : Statement
{
    public bool IsPublic { get; set; } = false;
}

internal sealed class ModuleDecl : Declaration
{
    public required Ident Ident { get; init; }
}
internal class VariableDecl : Declaration
{
    public required bool Const { get; set; }
    public required string Name { get; set; }
    public required Expression Expr { get; set; }
}
internal sealed class TypedVariableDecl : VariableDecl
{
    public required Ty Type { get; init; }
}
internal class ArrayDecl : Declaration
{
    public required bool Const { get; set; }
    public required string Name { get; set; }
    public required Expression ArrayExpression { get; set; }
}
internal class TypedArrayDecl : ArrayDecl
{
    public required Ty Type { get; init; }
    public required List<Expression>? Sized { get; set; }
}
internal sealed class FunctionDecl : Declaration
{
    public required Ident Name { get; init; }
    public required FunArg[] Args { get; init; }
    public required Instruction[] Block { get; init; }
    public required Ty ReturnTy { get; init; }

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