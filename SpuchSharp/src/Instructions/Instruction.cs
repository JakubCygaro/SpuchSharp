using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;

namespace SpuchSharp.Instructions;

internal abstract class Instruction 
{
    public Location? Location { get; set; }
}

