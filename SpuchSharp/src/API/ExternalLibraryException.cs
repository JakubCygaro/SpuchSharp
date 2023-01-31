using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.API;


[Serializable]
public class ExternalLibraryException : Exception
{
	public ExternalLibraryException() { }
	public ExternalLibraryException(string message) : base(message) { }
	public ExternalLibraryException(string message, Exception inner) : base(message, inner) { }
	protected ExternalLibraryException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
