using SpuchSharp.Instructions;
using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Interpreting;


[Serializable]
public class InterpreterException : Exception
{
	public InterpreterException() { }
	public InterpreterException(string message) : base(message) { }
	public InterpreterException(string message, Tokens.Token token) 
		: base($"{message} {token.Location}") 
	{ }
	internal InterpreterException(string message, Instructions.Instruction instruction) 
		: base($"{message} {instruction.Location}") 
	{ }
	internal static InterpreterException UnsuportedInstruction(Instruction ins)
	{
		var message = $"Instruction is not yet supported by the interpreter: {ins}";
		return new InterpreterException(message, ins);
	}
	internal static InterpreterException TypeMismatch<TExpected>(Ty ty)
		where TExpected : Ty
	{
		var message = $"Type mismatch, expected {nameof(TExpected)}, got {ty.Ident}";
		return new InterpreterException(message, ty);
	}
	internal static InterpreterException VariableNotFound(Ident ident)
	{
		var message = $"Variable {ident.Value} not found in present scope.";
		return new InterpreterException(message, ident);
	}
	public InterpreterException(string message, Exception inner) : base(message, inner) { }
	protected InterpreterException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
