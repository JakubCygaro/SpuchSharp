using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Lexing;


[Serializable]
public class LexerException : Exception
{
	private readonly int _line = 0;
	private readonly int _column = 0;

	public LexerException(string message, int line, int column) : 
		base($"{message}. ({line}:{column})")
	{
		_line = line;
		_column = column;
	}
	public LexerException() { }
	public LexerException(string message) : base(message) { }
	public LexerException(string message, Exception inner) : base(message, inner) { }
	protected LexerException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
