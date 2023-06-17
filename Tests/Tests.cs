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
}