using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;

namespace SpuchSharp.Parsing;

internal sealed class FunArg : Token
{
    public override string Stringify() =>
        $"Function Argument Token: {Ty.Stringify()} {Name.Stringify()} ref: {Ref}";
    public required Ty Ty { get; init; }
    public required Ident Name { get; init; }
    public required bool Ref { get; init; }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
