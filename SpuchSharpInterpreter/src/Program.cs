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
        return Parser.Default.ParseArguments<RunOptions, SetupOptions>(args)
            .MapResult(
                (RunOptions runopts) => RunProject(runopts),
                (SetupOptions setupupt) => SetupProject(setupupt),
                errs => HandleErrors(errs));
        //return ParseOptions(args) switch
        //{
        //    RunOptions r => RunProject(r),
        //    SetupOptions s => SetupProject(s),
        //    ParseFailure f => HandleFailure(f),
        //    _ => -1,
        //};
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
        if (runOptions.EntryFile is not null)
        {
            settings.EntryPoint = runOptions.EntryFile;
        }
        Interpreter interpreter = new(settings, runOptions.Args?.ToArray());
        try
        {
            return interpreter.Run();
        }
        catch (Exception ex)
        {
#if DEBUG 
            Console.Error.WriteLine($"Error: {ex}");
#else
            Console.Error.WriteLine($"Error: {ex.Message}");
#endif
        }
        return 1;
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
        foreach (var err in errors)
        {
            Console.Error.WriteLine(err.ToString());
        }
        return 1;
    }

}
[Verb("run", HelpText = "Run a Spuch# script")]
class RunOptions
{
    [Value(0, HelpText = "Run from provided entry point .spsh file, `main` by default")]
    public required string EntryFile { get; set; }

    [Value(1, HelpText = "Arguments that will be passed to the script")]
    public IEnumerable<string> Args { get; set; } = Enumerable.Empty<string>();
}

[Verb("setup", HelpText = "Setup a project")]
class SetupOptions
{
    [Value(0, HelpText = "Project Name")]
    public required string ProjectName { get; set; }
}