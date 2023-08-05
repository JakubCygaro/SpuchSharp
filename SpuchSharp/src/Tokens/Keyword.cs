using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class KeyWord : Token 
{
    public static KeyWord? From(string literal) => literal switch
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
        "ref" => new Ref(),
        "mod" => new Mod(),
        "use" => new Use(),
        "pub" => new Public(),
        "const" => new Const(),
        "struct" => new Struct(),
        _ => null
    };
    public static KeyWord? From(ReadOnlySpan<char> literal) => literal switch
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
        "ref" => new Ref(),
        "mod" => new Mod(),
        "use" => new Use(),
        "pub" => new Public(),
        "const" => new Const(),
        "struct" =>  new Struct(),
        _ => null
    };
}
internal sealed class Var : KeyWord, IStaticStringify
{
    public static string StaticStringify => "var";
    public override string Stringify() => StaticStringify;
}
internal sealed class Fun : KeyWord, IStaticStringify
{
    public static string StaticStringify => "fun";

    public override string Stringify() => StaticStringify;
}

internal sealed class Delete : KeyWord, IStaticStringify
{
    public static string StaticStringify => "delete"; 
    public override string Stringify() => StaticStringify;
}

internal sealed class Import : KeyWord, IStaticStringify
{
    public static string StaticStringify => "import"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Return : KeyWord, IStaticStringify
{
    public static string StaticStringify => "return"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class If : KeyWord, IStaticStringify
{
    public static string StaticStringify => "if"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Else : KeyWord, IStaticStringify
{
    public static string StaticStringify => "else"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Loop : KeyWord, IStaticStringify
{
    public static string StaticStringify => "loop"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class Skip : KeyWord, IStaticStringify
{
    public static string StaticStringify => "skip"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class Break : KeyWord, IStaticStringify
{
    public static string StaticStringify => "break"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class For : KeyWord, IStaticStringify
{
    public static string StaticStringify => "for"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class From : KeyWord, IStaticStringify
{
    public static string StaticStringify => "from"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class To : KeyWord, IStaticStringify
{
    public static string StaticStringify => "to"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class While : KeyWord, IStaticStringify
{
    public static string StaticStringify => "while"; 
    public override string Stringify() => StaticStringify;

}
internal sealed class Ref : KeyWord, IStaticStringify
{
    public static string StaticStringify => "ref"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Use : KeyWord, IStaticStringify
{
    public static string StaticStringify => "use"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Mod : KeyWord, IStaticStringify
{
    public static string StaticStringify => "mod"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Public : KeyWord, IStaticStringify
{
    public static string StaticStringify => "pub";

    public override string Stringify() => StaticStringify;
}
internal sealed class Const : KeyWord, IStaticStringify
{
    public static string StaticStringify => "const";

    public override string Stringify() => StaticStringify;
}

internal sealed class Switch : KeyWord, IStaticStringify
{
    public static string StaticStringify => "switch"; 
    public override string Stringify() => StaticStringify;
}
internal sealed class Struct : KeyWord, IStaticStringify
{
    public static string StaticStringify => "struct";
    public override string Stringify() => StaticStringify;
}
