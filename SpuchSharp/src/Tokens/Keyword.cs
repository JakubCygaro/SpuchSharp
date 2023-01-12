using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class KeyWord : Token 
{
    public static KeyWord From(string literal) => literal switch
    {
        "var" => new Var(),
        "fun" => new Fun(),
        "delete" => new Delete(),
        "import" => new Import(),
        "return" => new Return(),
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

internal sealed class Delete : KeyWord
{
    public override string Stringify() => "delete";
}

internal sealed class Import : KeyWord
{
    public override string Stringify() => "import";
}
internal sealed class Return : KeyWord
{
    public override string Stringify() => "return";
}

