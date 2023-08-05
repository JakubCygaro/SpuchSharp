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
        "+=" => new AssignAdd(),
        "-=" => new AssignSub(),
        "*=" => new AssignMul(),
        "/=" => new AssignDiv(),
        "%=" => new AssignModulo(),
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

abstract class AssignToken : SimpleToken { }
sealed class Assign : AssignToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;
    public static string StaticStringify => "=";
}
sealed class Semicolon: SimpleToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;
    public static string StaticStringify => ";";
}
sealed class Colon : SimpleToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;
    public static string StaticStringify => ":";

}
sealed class Colon2 : SimpleToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;
    public static string StaticStringify => "::";

}
sealed class Dot : Operator, IStaticStringify
{
    public override string Stringify() => StaticStringify;
    public static string StaticStringify => ".";

}
sealed class Comma : SimpleToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;

    public static string StaticStringify => ",";

}
sealed class AssignAdd : AssignToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;
    public static string StaticStringify => "+=";

}
sealed class AssignSub : AssignToken, IStaticStringify
{
    public override string Stringify() => StaticStringify;

    public static string StaticStringify => "-=";

}
sealed class AssignMul : AssignToken, IStaticStringify
{
    public override string Stringify() => StaticStringify; 
    public static string StaticStringify => "*=";
}
sealed class AssignDiv : AssignToken, IStaticStringify
{
    public override string Stringify() => StaticStringify; 
    public static string StaticStringify => "/=";
}
sealed class AssignModulo : AssignToken, IStaticStringify
{
    public override string Stringify() => StaticStringify; 
    public static string StaticStringify => "%=";
}

abstract class Paren : SimpleToken
{
}

abstract class Round: Paren
{
    internal sealed class Open : Round, IStaticStringify
    {
        public override string Stringify() => StaticStringify;
        public static string StaticStringify => "(";
    }
    internal sealed class Closed : Round, IStaticStringify
    {
        public override string Stringify() => StaticStringify;
        public static string StaticStringify => ")";
    }
}
abstract class Curly : Paren
{
    internal sealed class Open : Curly, IStaticStringify
    {
        public override string Stringify() => StaticStringify;
        public static string StaticStringify => "{";
    }
    internal sealed class Closed : Curly, IStaticStringify
    {
        public override string Stringify() => StaticStringify;
        public static string StaticStringify => "}";
    }
}
abstract class Brackets : SimpleToken { }
internal abstract class Square : Brackets
{
    internal sealed class Open : Square, IStaticStringify
    {
        public override string Stringify() => StaticStringify; 
        public static string StaticStringify => "[";
    }
    internal sealed class Closed : Square, IStaticStringify
    {
        public override string Stringify() => StaticStringify; 
        public static string StaticStringify => "]";
    }
}



// Logical operators


