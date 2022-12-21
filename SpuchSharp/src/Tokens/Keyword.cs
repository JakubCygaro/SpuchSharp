using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class KeyWord : Token 
{
    public static KeyWord From(string value) => value switch
    {
        "var" => new Var(),
        "fun" => new Fun(),
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}
internal sealed class Var : KeyWord 
{
    public override string Stringify() => "var";
}
internal sealed class Fun : KeyWord
{
    public override string Stringify() => "fun";
}

