using SpuchSharp.API;

namespace STDLib;

public static class STD
{
    [Function("println")]
    public static void Println(object text) => Console.WriteLine(text);

    [Function("print")]
    public static void Print(object text) => Console.Write(text);

    [Function("pause")]
    public static void Pause(int millisecond) => Task.Delay(millisecond).Wait();
}