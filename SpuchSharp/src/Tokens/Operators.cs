using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

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
        "+" => new Add(),
        "-" => new Sub(),
        "*" => new Mult(),
        "/" => new Div(),
        "&" => new Ampersand(),
        "&&" => new And(),
        "|" => new Pipe(),
        "||" => new Or(),
        _ => throw new Lexing.LexerException($"Failed to tokenize into Operator `{value}`"),
    };

}


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
sealed class And : LogicOperator
{
    public override string Stringify() => "&&";
}
sealed class Or : LogicOperator
{
    public override string Stringify() => "||";
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

internal abstract class BitOperator : Operator { }

sealed class Ampersand : BitOperator
{
    public override string Stringify() => "&";
}
sealed class Pipe : BitOperator
{
    public override string Stringify() => "|";
}