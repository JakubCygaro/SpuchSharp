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
        "(" or ")" => new Round(),
        "{" or "}" => new Curly(),
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

abstract class Paren : SimpleToken { }

sealed class Round: Paren 
{
    public override string Stringify()
    {
        return "()";
    }
}
sealed class Curly : Paren 
{
    public override string Stringify()
    {
        return "{}";
    }
}

