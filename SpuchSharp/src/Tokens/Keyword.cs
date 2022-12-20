using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class KeyWord : Token { }
internal sealed class Var : KeyWord 
{
    public override string Stringify() => "var";
}
internal sealed class Fun : KeyWord
{
    public override string Stringify() => "fun";
}

