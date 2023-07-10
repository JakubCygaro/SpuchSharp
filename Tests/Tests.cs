using SpuchSharp.Parsing;
using System.Collections.Generic;
using Xunit.Sdk;
using System;
using SpuchSharp;
using System.Reflection;
using System.IO;

namespace Tests;

public class Tests
{
    void RunSource(string src)
    {
        new Interpreter(src).Run();
    }
    [Fact]
    public void HelloWorld()
    {
        var s = """
            import "STDLib";
            fun main(){
                println("Hello world!");
            }
            """;
        RunSource(s);
    }
    [Fact]
    public void ParserExceptionNoSemicolonAfterImport()
    {
        var s = """
            import "STDLib"
            fun main(){
                println("Hello world!");
            }
            """;
        Assert.ThrowsAny<ParserException>(() => RunSource(s));
    }
    [Fact]
    public void CallingOtherFunctions()
    {
        var s =
        """
        import "STDLib";
        fun main(){
            var x = 10;
            foo1();
            foo2(x);
            foo3(x);
        }
        fun foo1(){}
        fun foo2(int x) int {
            return x;
        }
        fun foo3(ref int x){
            x++;
        }
        """;
        RunSource(s);
    }
    [Fact]
    public void VariablesAndDeclarations()
    {
        var s =
        """
        import "STDLib";
        fun main(){
            bool b = false;
            bool b2 = true;
            short s = 1;
            int a = 1;
            long l1 = 100;
            long l2 = 100L;
            float f1 = 1.0;
            float f2 = 1f;
            double d1 = 1.0;
            double d2 = 1f;

            text t1 = "text";
            text t2 = "text\ntext";

            int[] i_arr1 = {
                1,
                2,
                3,
                4,
            };
            int[] i_arr2 = [10];
            int[][] i_arr3 = {
                {1, 2},
                {3, 4}
            };
        }
        """;
        RunSource(s);
    }
    [Theory]
    [InlineData("Modules")]
    [InlineData("Expressions")]
    [InlineData("Expressions", "operators")]
    [InlineData("Arrays")]
    [InlineData("Loops")]

    public void TestCorrectSources(string dir, string main = "main")
    {
        var proj = new ProjectSettings()
        {
            EntryPoint = $"A:\\C#\\SpuchSharp\\Tests\\Source\\{dir}\\{main + ".spsh"}",
            ExternalLibs = new(),
            ProjectName = dir,
        };
        new Interpreter(proj).Run();
    }
    /// <summary>
    /// Tests code that is supposed to throw an exception
    /// </summary>
    [Theory]
    [InlineData("References", "constreffun")]
    [InlineData("References", "constvar")]
    [InlineData("References", "constass")]
    [InlineData("References", "constarr")]
    [InlineData("References", "constmultfun")]
    [InlineData("Arrays", "indexing_one")]
    [InlineData("Arrays", "indexing_two")]
    [InlineData("Arrays", "indexing_three")]
    [InlineData("Arrays", "empty_array")]

    public void TestThrowingSources(string dir, string main = "main")
    {
        var proj = new ProjectSettings()
        {
            EntryPoint = $"A:\\C#\\SpuchSharp\\Tests\\Source\\{dir}\\{main + ".spsh"}",
            ExternalLibs = new(),
            ProjectName = dir,
        };
        Assert.ThrowsAny<InterpreterException>(() => new Interpreter(proj).Run());
    }

    [Fact]
    public void TestArguments()
    {
        var proj = new ProjectSettings()
        {
            EntryPoint = $"A:\\C#\\SpuchSharp\\Tests\\Source\\Args\\main.spsh",
            ExternalLibs = new(),
            ProjectName = "Args",
        };
        new Interpreter(proj, new string[] { "a", "b", "c" }).Run();
    }

    [Theory]
    [InlineData("fail")]
    public void TestArgumentsFailing(string source, string[]? args = null)
    {
        args ??= Array.Empty<string>();
        var proj = new ProjectSettings()
        {
            EntryPoint = $"A:\\C#\\SpuchSharp\\Tests\\Source\\Args\\{source + ".spsh"}",
            ExternalLibs = new(),
            ProjectName = "Args",
        };
        Assert.ThrowsAny<InterpreterException>(() => new Interpreter(proj, args).Run());
    }

}