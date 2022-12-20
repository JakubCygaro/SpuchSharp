using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class SimpleToken : Token
{
    public static SimpleToken From(string value) => value switch 
    {
        ";" => new Semicolon(),
        "=" => new Equality(),
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}

sealed class Equality : SimpleToken 
{
    public override string Stringify() => "=";
}
sealed class Semicolon: SimpleToken 
{
    public override string Stringify() => ";";
}

