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
internal sealed class ModuleDecl : Declaration 
{
    public required Ident Ident { get; init; }
}
internal sealed class UseStmt : Statement 
{
    public required Ident[] ModulePath { get; init; }
}
internal class Variable : Declaration
{
    public required string Name { get; set; }
    public required Expression Expr { get; set; }
}
internal sealed class Typed : Variable
{
    public required Ty Type { get; init; }
}
internal class ArrayDecl : Declaration
{
    public required string Name { get; set; }
    public required Expression ArrayExpression { get; set; } 
}
internal class TypedArrayDecl : ArrayDecl
{
    public required Ty Type { get; init; }
    public required List<Expression>? Sized { get; set; }
}

internal sealed class Function : Declaration
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
//assignment
internal sealed class Assignment : Statement
{
    public required AssignTarget Left { get; init; }
    public required Expression Expr { get; init; }
}
internal abstract class AssignTarget 
{
    public required Expression Target { get; init; }
}
internal sealed class ArrayIndexTarget : AssignTarget
{
    public required Expression IndexExpression { get; init; }
}
internal sealed class IdentTarget : AssignTarget
{

}
internal sealed class DeleteStatement : Statement
{
    public required Ident VariableIdent { get; init; }
}
internal sealed class ImportStatement : Statement
{
    public required string Path { get; init; }
}
internal sealed class ReturnStatement : Statement
{
    public required Expression Expr { get; init; }
}
internal sealed class IfStatement : Statement
{
    // if (<expr>) { }
    public required Expression Expr { get; init; }
    public required Instruction[] Block { get; init; }
    public required Instruction[]? ElseBlock { get; init; } = null;

}
internal sealed class LoopStatement : Statement
{
    public required Instruction[] Block { get; init; }
}
internal sealed class BreakStatement : Statement { }
internal sealed class SkipStatement : Statement { }

internal sealed class ForLoopStatement : Statement 
{
    public required Ident VariableIdent { get; init; } 
    public required Expression From { get; init; }
    public required Expression To { get; init; }
    public required Instruction[] Block { get; init; }
}
internal sealed class WhileStatement : Statement
{
    public required Expression Condition { get; init; }
    public required Instruction[] Block { get; init;}
}