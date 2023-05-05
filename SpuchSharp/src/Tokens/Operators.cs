using SpuchSharp.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;


abstract class Operator : SimpleToken
{
    public virtual short Precedence { get => 0; }
    new public static Operator From(string value) => value switch
    {
        "==" => new Equality(),
        "!=" => new InEquality(),
        ">" => new Greater(),
        "<" => new Less(),
        ">=" => new GreaterOrEq(),
        "<=" => new LessOrEq(),
        "+" => new Add(),
        "++" => new Add2(),
        "-" => new Sub(),
        "--" => new Sub2(),
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
    public override short Precedence => 4;
    public override string Stringify() => "==";
}
sealed class InEquality : LogicOperator
{
    public override short Precedence => 4;
    public override string Stringify() => "!=";
}
sealed class Greater : LogicOperator
{
    public override short Precedence => 5;
    public override string Stringify() => ">";
}
sealed class Less : LogicOperator
{
    public override short Precedence => 5;
    public override string Stringify() => "<";
}
sealed class GreaterOrEq : LogicOperator
{
    public override short Precedence => 5;
    public override string Stringify() => ">=";
}
sealed class LessOrEq : LogicOperator
{
    public override short Precedence => 5;
    public override string Stringify() => "<=";
}
sealed class And : LogicOperator
{
    public override short Precedence => 3;
    public override string Stringify() => "&&";
}
sealed class Or : LogicOperator
{
    public override short Precedence => 3;
    public override string Stringify() => "||";
}
sealed class Exclam : LogicOperator
{
    public override short Precedence => 30;
    public override string Stringify() => "!";
}

// value operators

internal abstract class ArithmeticOperator : Operator { }

sealed class Add : ArithmeticOperator
{
    public override short Precedence => 10;
    public override string Stringify() => "+";
}
sealed class Sub : ArithmeticOperator
{
    public override short Precedence => 10;
    public override string Stringify() => "-";
}
sealed class Mult : ArithmeticOperator
{
    public override short Precedence => 20;
    public override string Stringify() => "*";
}
sealed class Div : ArithmeticOperator
{
    public override short Precedence => 20;
    public override string Stringify() => "/";
}
sealed class Percent : ArithmeticOperator
{
    public override short Precedence => 20;
    public override string Stringify() => "%";
}
sealed class Add2 : ArithmeticOperator
{
    public override short Precedence => 20;
    public override string Stringify() => "++";
}
sealed class Sub2 : ArithmeticOperator
{
    public override short Precedence => 20;
    public override string Stringify() => "--";
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
internal sealed class CastOperator : Operator
{
    public required Ty TargetType { get; init; }
    public override short Precedence => 20;
    public override string Stringify()
    {
        return $"({TargetType.Stringify()})";
    }
}