using SpuchSharp.Parsing;
using System.Collections.Generic;
using Xunit.Sdk;
using System;

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
        Assert.Throws<ParserException>(() => RunSource(s));
    }
}