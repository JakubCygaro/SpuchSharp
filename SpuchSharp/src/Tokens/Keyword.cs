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
        "if" => new If(),
        "else" => new Else(),
        "break" => new Break(),
        "skip" => new Skip(),
        "loop" => new Loop(),
        "for" => new For(),
        "from" => new From(),
        "to" => new To(),
        "while" => new While(),
        _ => throw new Lexing.LexerException($"Failed to tokenize {literal} as a keyword token."),
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
internal sealed class If : KeyWord
{
    public override string Stringify() => "if";
}
internal sealed class Else : KeyWord
{
    public override string Stringify() => "else";
}
internal sealed class Loop : KeyWord
{
    public override string Stringify() => "loop";

}
internal sealed class Skip : KeyWord
{
    public override string Stringify() => "skip";

}
internal sealed class Break : KeyWord
{
    public override string Stringify() => "break";

}
internal sealed class For : KeyWord
{
    public override string Stringify() => "for";

}
internal sealed class From : KeyWord
{
    public override string Stringify() => "from";

}
internal sealed class To : KeyWord
{
    public override string Stringify() => "to";

}
internal sealed class While : KeyWord
{
    public override string Stringify() => "while";

}

