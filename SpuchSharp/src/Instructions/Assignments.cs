using System;
using System.Collections.Generic;

namespace SpuchSharp.Instructions;

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