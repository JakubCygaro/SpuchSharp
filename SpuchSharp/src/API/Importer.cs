using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using SpuchSharp.Interpreting;

using VariableScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;
using SpuchSharp.Tokens;
using SpuchSharp.Parsing;

namespace SpuchSharp.API;

internal static class Importer
{
    public static FunctionScope ImportFunctions(string dllPath)
    {

        Assembly importedAssembly;
        try
        {
            dllPath = Path.GetFullPath(dllPath);
            importedAssembly = Assembly.LoadFrom(dllPath);
        }
        catch(Exception ex) 
        {
            throw new ImporterException(ex.Message, ex);
        }
        var valid = GetValidFunctions(importedAssembly);
        return valid.Select(info => new ExternalFunction
        {
            Args = info.Args.ToArray(),
            Block = Array.Empty<Instructions.Instruction>(),
            Ident = info.Ident,
            MethodInfo = info.MethodInfo,
        })
            .ToDictionary(external => external.Ident, external => external as SFunction);

    }
    static List<FunctionInfo> GetValidFunctions(Assembly assembly)
    {
        //var methods = assembly.GetTypes()
        //    .Where(t => t.IsClass && t.IsPublic)
        //    .Select(c => c.GetMethods(BindingFlags.Static | BindingFlags.Public));
        List<FunctionInfo> validFunctions = new();
        foreach(var methodList in assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic)
                .Select(c => c.GetMethods(BindingFlags.Static | BindingFlags.Public)))
        {
            var valid = methodList
                .Select(m =>
                {
                    FunctionInfo? functionInfo = null;
                    if (m.GetCustomAttribute<FunctionAttribute>() is FunctionAttribute attr)
                    {
                        functionInfo = new FunctionInfo
                        {
                            Ident = attr._ident,
                            MethodInfo = m,
                        };
                    }
                    return functionInfo;
                })
                .Where(i => i is not null)
                .Cast<FunctionInfo>()
                .Where(Validate)
                .ToList();
            validFunctions.AddRange(valid);
        }
        return validFunctions;
    }
    static bool Validate(FunctionInfo functionInfo)
    {
        foreach(var parameter in functionInfo.MethodInfo.GetParameters())
        {
            
            var paramType = parameter.ParameterType;
            if (paramType == typeof(string))
            {
                functionInfo.Args.Add(new FunArg
                {
                    Name = new Ident { Value = "unnamed" },
                    Ty = Ty.Text,
                });
            }
            else if (paramType == typeof(int))
            {
                functionInfo.Args.Add(new FunArg
                {
                    Name = new Ident { Value = "unnamed" },
                    Ty = Ty.Int,
                });
            }
            else if (paramType == typeof(bool))
            {
                functionInfo.Args.Add(new FunArg
                {
                    Name = new Ident { Value = "unnamed" },
                    Ty = Ty.Boolean,
                });
            }
            else if (paramType == typeof(object))
            {
                functionInfo.Args.Add(new FunArg
                {
                    Name = new Ident { Value = "unnamed" },
                    Ty = Ty.Any,
                });
            }
            else
                throw new ImporterException("Function declared in the external library uses" +
                    $" unsuported argument types. fun: {functionInfo.MethodInfo.Name}, type: {paramType}");
        }
        if(functionInfo.MethodInfo.ReturnType != typeof(void))
            throw new ImporterException("Function declared in the external library uses" +
                    $" an unsuported return type: {functionInfo.MethodInfo.Name}");
        return true;
    }
    private class FunctionInfo
    {
        public required Ident Ident { get; init; }
        public List<FunArg> Args { get; set; } = new();
        public required MethodInfo MethodInfo { get; init; }
    }
}
