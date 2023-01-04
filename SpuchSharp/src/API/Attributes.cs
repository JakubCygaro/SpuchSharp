using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;

namespace SpuchSharp.API;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class FunctionAttribute : Attribute
{
    internal readonly Ident _ident;
    public FunctionAttribute(string ident)
    {
        try
        {
            _ident = Ident.From(ident);
        } 
        catch (Exception)
        {
            throw new Exception("Function name is invalid");
        }
    }
}
