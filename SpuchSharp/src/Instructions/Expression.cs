using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;

namespace SpuchSharp.Instructions;
internal abstract class Expression : Instruction { }

internal sealed class Assignment : Expression 
{
    public required Ident Left { get; set; }
    public required Value Right { get; set; }
}