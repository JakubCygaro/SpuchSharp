using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpuchSharp;

public class ProjectSettings
{
    public required string ProjectName { get; set; }
    public required string EntryPoint { get; set; }
    public required Dictionary<string, string> ExternalLibs { get; set; }
    //public required string[] Args { get; set; }

    public static ProjectSettings Default = new SpuchSharp.ProjectSettings
    {
        EntryPoint = "main.spsh",
        ProjectName = Directory.GetCurrentDirectory(),
        ExternalLibs = new(),
        //Args = new string[]{ Directory.GetCurrentDirectory() }
    };
    public static ProjectSettings Debug = new SpuchSharp.ProjectSettings
    {
        EntryPoint = "debug",
        ProjectName = "<DEBUG PROJECT>",
        ExternalLibs = new(),
        //Args = new string[] { "<DEBUG PROJECT>" }
    };


}
