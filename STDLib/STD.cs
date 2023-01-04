using SpuchSharp.API;

namespace STDLib;

public static class STD
{
    [Function("print")]
    public static void Print(object text) => Console.WriteLine(text);
}