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
using SModule = SpuchSharp.Interpreting.Module;
namespace SpuchSharp.API;

internal static class Importer
{
    public static string GetExternalLibsGlobalPath()
    {
        var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
            throw new Exception("Unable to localize the assembly directory");
        location = Path.GetFullPath(location);
        var path = Path.Combine(location, "ExternalLibs");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
    static Dictionary<Ident, StructTy> EMPTY_STRUCT_SCOPE = new();
    static Dictionary<Ident, SVariable> EMPTY_VARIABLE_SCOPE = new();
    public static SModule ImportModule(string ddlPath, Ident moduleIdent)
    {
        return new SModule()
        {
            OwnedFunctions = ImportFunctions(ddlPath),
            FunctionScope = new(),
            VariableScope = EMPTY_VARIABLE_SCOPE,
            StructScope = EMPTY_STRUCT_SCOPE,
            Ident = moduleIdent,
            Modules = new(),
            ParentModule = null,
            IsExternal = true,
            OwnedVariables = EMPTY_VARIABLE_SCOPE,
            OwnedStructs = EMPTY_STRUCT_SCOPE,
            DirectoryPath = Path.GetDirectoryName(Path.GetFullPath(ddlPath)) ??
                                    throw new ImporterException($"Could not determine module directory path for external library `{ddlPath}`")
        };
    }
    public static FunctionScope ImportFunctions(string dllPath)
    {
        

        Assembly importedAssembly;
        try
        {
            dllPath = Path.GetFullPath(dllPath);
            importedAssembly = Assembly.LoadFrom(dllPath);
            var valid = GetValidFunctions(importedAssembly);

            return valid.Select(info => new ExternalFunction
            {
                Args = info.Args.ToArray(),
                Block = Array.Empty<Instructions.Instruction>(),
                Ident = info.Ident,
                MethodInfo = info.MethodInfo,
                ReturnTy = Ty.FromCSharpType(info.MethodInfo.ReturnType),
                IsPublic = true,
                ParentModule = null,
            })
                .ToDictionary(external => external.Ident, external => external as SFunction);
        }
        catch(Exception ex) 
        {
            throw new ImporterException(ex.Message, ex);
        }
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
        int i = 0;
        foreach(var parameter in functionInfo.MethodInfo.GetParameters())
        {
            
            var paramType = parameter.ParameterType;
            try
            {
                functionInfo.Args.Add(new FunArg
                {
                    Name = new Ident { Value = $"external_function_argument_{i++}" },
                    Ty = Ty.FromCSharpType(paramType),
                    Ref = false,
                    Const = false,
                });
            }
            catch
            {
                throw new ImporterException("Function declared in an external library uses" +
                    $" unsuported argument types. fun: {functionInfo.MethodInfo.Name}, type: {paramType}");
            }

        }
        return true;
    }
    private class FunctionInfo
    {
        public required Ident Ident { get; init; }
        public List<FunArg> Args { get; set; } = new();
        public required MethodInfo MethodInfo { get; init; }
    }
}
