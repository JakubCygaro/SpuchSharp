using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

public interface IStaticStringify
{
    public abstract static string StaticStringify { get; }
}
