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
        "=" => new Assign(),
        "(" => new Round.Open(),
        ")" => new Round.Closed(),
        "{" => new Curly.Open(),
        "}" => new Curly.Closed(),
        "[" => new Square.Open(),
        "]" => new Square.Closed(),
        "." => new Dot(),
        ":" => new Colon(),
        "::" => new Colon2(),
        "," => new Comma(),
        _ => Operator.From(value),
    };
}

sealed class Assign : SimpleToken 
{
    public override string Stringify() => "=";
}
sealed class Semicolon: SimpleToken 
{
    public override string Stringify() => ";";
}
sealed class Colon : SimpleToken
{
    public override string Stringify() => ":";
}
sealed class Colon2 : SimpleToken
{
    public override string Stringify() => "::";
}
sealed class Dot : SimpleToken 
{
    public override string Stringify() => ".";
}
sealed class Comma : SimpleToken
{
    public override string Stringify() => ",";
}



abstract class Paren : SimpleToken
{
}

abstract class Round: Paren
{
    internal sealed class Open : Round
    {
        public override string Stringify()
        {
            return "(";
        }
    }
    internal sealed class Closed : Round
    {
        public override string Stringify()
        {
            return ")";
        }
    }
}
abstract class Curly : Paren
{
    internal sealed class Open : Curly
    {
        public override string Stringify()
        {
            return "{";
        }
    }
    internal sealed class Closed : Curly
    {
        public override string Stringify()
        {
            return "}";
        }
    }
}
abstract class Brackets : SimpleToken { }
internal abstract class Square : Brackets
{
    internal sealed class Open : Square
    {
        public override string Stringify()
        {
            return "[";
        }
    }
    internal sealed class Closed : Square
    {
        public override string Stringify()
        {
            return "]";
        }
    }
}



// Logical operators


