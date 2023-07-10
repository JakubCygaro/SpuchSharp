#if DEBUG
#define INSTRUCTION
#define EXPRESSION
#define SCOPE
#define LEXER_DEBUG
#endif

using SpuchSharp.Interpreting;
using CommandLine;
using Newtonsoft.Json;
using System.Reflection;

//[assembly:AssemblyVersion("0.0.1.0")]

namespace SpuchSharp;
internal class Program
{
    static string MAIN_FILE_CONTENT = "import \"STDLib\";\nfun main() {\n\tprintln(\"Hello world!\");\n}";
    static string PROJECT_JSON = "project.json";

    static int Main(string[] args)
    {
        //return Parser.Default.ParseArguments<RunOptions, SetupOptions>(args)
        //    .MapResult(
        //        (RunOptions runopts) => RunProject(runopts),
        //        (SetupOptions setupupt) => SetupProject(setupupt),
        //        errs => HandleErrors(errs));
        return ParseOptions(args) switch
        {
            RunOptions r => RunProject(r),
            SetupOptions s => SetupProject(s),
            ParseFailure f => HandleFailure(f),
            _ => -1,
        };
    }
    static int RunProject(RunOptions runOptions)
    {
        ProjectSettings settings;
        if (File.Exists(PROJECT_JSON))
        {
            var json = File.ReadAllText(PROJECT_JSON);
            try
            {
                settings = JsonConvert.DeserializeObject<ProjectSettings>(json) ??
                    throw new Exception("Could not read project file.");
            }
            catch (Exception ex) 
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
        else
            settings = ProjectSettings.Default;
        if(runOptions.EntryFile is not null)
        {
            settings.EntryPoint = runOptions.EntryFile;
        }
        Interpreter interpreter = new(settings, runOptions.Args?.ToArray());
        try
        {
            interpreter.Run();
        }
        catch (Exception ex)
        {
#if DEBUG 
            Console.Error.WriteLine(ex);
#else
            Console.Error.WriteLine(ex.Message);
#endif
        }
        return 0;
    }
    static int SetupProject(SetupOptions setupOptions)
    {
        if (Directory.Exists(setupOptions.ProjectName))
        {
            Console.Error.WriteLine($"A directory with name `{setupOptions.ProjectName}` already exists");
            return 1;
        }
        Directory.CreateDirectory(setupOptions.ProjectName);
        Directory.SetCurrentDirectory(setupOptions.ProjectName);

        var settings = new SpuchSharp.ProjectSettings
        {
            EntryPoint = "main.spsh",
            ProjectName = setupOptions.ProjectName,
            ExternalLibs = new(),
        };
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(PROJECT_JSON, json);
        File.WriteAllText("main.spsh", MAIN_FILE_CONTENT);
        return 0;
    }
    static int HandleErrors(IEnumerable<Error> errors)
    {
        foreach(var err in errors)
        {
            Console.Error.WriteLine(err.ToString());
        }
        return 1;
    }

    static ParseResult ParseOptions(string[] args)
    {
        if (args.Length == 0)
            return new ParseFailure
            {
                Reason = "Invalid argument count"
            };
        if (args[0] == "run")
            return ParseRun(args);
        else if (args[0] == "setup")
            return ParseSetup(args);
        else
            return new ParseFailure
            {
                Reason = "Unknown command type"
            };
    }
    static ParseResult ParseRun(string[] args)
    {
        if (args.Length < 2)
            return new ParseFailure
            {
                Reason = "Entry point file path required as the second argument"
            };
        var entryFile = args[1];
        var remainder = args[2..];

        return new RunOptions
        {
            Args = remainder ?? new string[0],
            EntryFile = entryFile,
        };

    }
    static ParseResult ParseSetup(string[] args)
    {
        if (args.Length != 2)
            return new ParseFailure
            {
                Reason = "Project name required as the second argument"
            };
        var projectName = args[1];
        return new SetupOptions
        {
            ProjectName = projectName
        };
    }

    static int HandleFailure(ParseFailure parseFailure)
    {
        Console.Error.WriteLine(parseFailure.Reason);
        return -1;
    }
}

abstract class ParseResult { }
sealed class RunOptions : ParseResult 
{
    public required string EntryFile { get; init; }
    public required string[] Args { get; init; }
}

sealed class SetupOptions : ParseResult
{
    public required string ProjectName { get; init; }
}

sealed class ParseFailure : ParseResult
{
    public required string Reason { get; init; }
}






//[Verb("run", HelpText = "Run a Spuch# script")]
//class RunOptions
//{
//    [Option('e', "entry", Required = false, HelpText = "Run from provided entry point .spsh file, `main` by default")]
//    public string? EntryFile { get; set; }
    
//    [Option('a', "args")]
//    public IEnumerable<string>? Args { get; set; }
//}

//[Verb("setup", HelpText = "Setup a project")]
//class SetupOptions
//{
//    [Option('n', "name", Required = true, HelpText = "Project Name")]
//    public required string ProjectName { get; set; }
//}