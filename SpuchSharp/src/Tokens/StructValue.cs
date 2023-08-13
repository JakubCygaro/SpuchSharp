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
        return new StructValue(_structTy)
        {
            Fields = Fields.ToDictionary(fnt => fnt.Key, fnt => fnt.Value.Clone()),
        };
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
