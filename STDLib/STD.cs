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

    [Function("pow")]
    public static int Pow(int x, int y) => (int)Math.Pow(x, y);

    [Function("readln")]
    public static string ReadLine() => Console.ReadLine() ?? "";

    [Function("stoi")]
    public static int Stoi(string input) => int.Parse(input);

    [Function("toString")]
    public static string ToString(object obj) => obj.ToString() ?? "";

}