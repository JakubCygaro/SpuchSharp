using System;
using System.Collections.Generic;

namespace SpuchSharp.Instructions;


internal abstract class Assignment : Statement
{
    public required AssignTarget Left { get; init; }
    public required Expression Expr { get; init; }
}

internal sealed class RegularAssignment : Assignment { }
internal sealed class AddAssignment : Assignment { }
internal sealed class SubAssignment : Assignment { }
internal sealed class MulAssignment : Assignment { }
internal sealed class DivAssignment : Assignment { }
internal sealed class ModuloAssignment : Assignment { }
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