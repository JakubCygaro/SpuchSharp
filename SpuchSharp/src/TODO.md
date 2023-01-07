# TODO

## Top priority
Return types for functions
return statement
if and else statements
## Second priority
better assignment resolution
`x = a = b + 2;`

## Type rework proposal

interface ITypeCompare {
	public Ty Ty { get; }
	public static abstract operator == (ITypeCompare otherTy);
}

Value : Token, ITypeCompare {
	public abstract required Ty Ty { get; }
}

SText Value.From(string text) => new SText { String = text };
SInt Value.From(int i) => new SInt { Int =  i };

SText : Value {
	Ty: Ty.Text,
	String: (string)
	public abstract override operator == (ITypeCompare other){
		return Ty.Equals(other.Ty);
	}
}
SInt : Value {
	Ty: Ty.Int,
	Int: (int)
}
SBool : Value {
	Ty: Ty.Boolean,
	Bool: (bool)
}
SVoid : Value {
	Ty: Ty.Void
}