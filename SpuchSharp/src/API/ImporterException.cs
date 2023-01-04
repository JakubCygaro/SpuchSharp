using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.API;


[Serializable]
public class ImporterException : Exception
{
	public ImporterException() { }
	public ImporterException(string message) : base(message) { }
	public ImporterException(string message, Exception inner) : base(message, inner) { }
	protected ImporterException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

