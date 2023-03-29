using SpuchSharp.Tokens;
using System;

namespace SpuchSharp.Instructions;

internal abstract class Statement : Instruction { }

internal sealed class UseStmt : Statement 
{
    public required Ident[] ModulePath { get; init; }
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