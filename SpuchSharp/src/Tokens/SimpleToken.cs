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
        "=" => new Assigment(),
        "(" or ")" => new Round(),
        "{" or "}" => new Curly(),
        "." => new Dot(),
        ":" => new Colon(),
        "," => new Comma(),
        _ => Operator.From(value),
    };
}

sealed class Assigment : SimpleToken 
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
sealed class Dot : SimpleToken 
{
    public override string Stringify() => ".";
}
sealed class Comma : SimpleToken
{
    public override string Stringify() => ",";
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

abstract class Operator : SimpleToken
{
    new public static Operator From(string value) => value switch 
    {
        "==" => new Equality(),
        "!=" => new InEquality(),
        ">" => new Greater(),
        "<" => new Less(),
        ">=" => new GreaterOrEq(),
        "<=" => new LessOrEq(),
        _ => throw new Lexing.LexerException($"Failed to tokenize into Operator `{value}`"),
    };

}
// Logical operators
internal abstract class LogicOperator : Operator { }
sealed class Equality : LogicOperator
{
    public override string Stringify() => "==";
}
sealed class InEquality : LogicOperator
{
    public override string Stringify() => "!=";
}
sealed class Greater : LogicOperator
{
    public override string Stringify() => ">";
}
sealed class Less : LogicOperator
{
    public override string Stringify() => "<";
}
sealed class GreaterOrEq : LogicOperator
{
    public override string Stringify() => ">=";
}
sealed class LessOrEq : LogicOperator
{
    public override string Stringify() => "<=";
}

// value operators

internal abstract class ValOperator : Operator { }

sealed class Add : ValOperator
{
    public override string Stringify() => "+";
}
sealed class Sub : ValOperator
{
    public override string Stringify() => "-";
}
sealed class Mult : ValOperator
{
    public override string Stringify() => "*";
}
sealed class Div : ValOperator
{
    public override string Stringify() => "/";
}
