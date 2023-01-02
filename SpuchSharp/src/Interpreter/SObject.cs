using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Instructions;
using SpuchSharp.Tokens;

namespace SpuchSharp.Interpreting;

internal abstract class SObject
{
    public abstract new string ToString();
}

internal class SVariable : SObject
{
    //public required string Name { get; init; }
    public required Tokens.Value Value { get; set; }
    public override string ToString() => $"{Value.Ty.Stringify()} = {Value.Val}";
    //public override int GetHashCode()
    //{
    //    return Name.GetHashCode();
    //}
}

//internal class SFunction : SObject
//{

//}

 