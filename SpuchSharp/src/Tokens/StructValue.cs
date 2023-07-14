using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal class StructValue : Value
{
    public required Dictionary<Ident, Value> Fields { get; init; } = new();

    private StructTy _structTy;

    public override Ty Ty => _structTy;

    public override object ValueAsObject => Fields;

    public override Value Clone()
    {
        throw new NotImplementedException();
    }

    public override string Stringify()
    {
        throw new NotImplementedException();
    }

    internal StructValue(StructTy structTy)
    {
        _structTy = structTy;
    }
}
