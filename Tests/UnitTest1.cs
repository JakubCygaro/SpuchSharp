using SpuchSharp.Parsing;
using System.Collections.Generic;


namespace Tests;

public class UnitTest1
{
    static Queue<Token> _queue = __SetupQueue();
    static Queue<Token> __SetupQueue()
    {
        var queue = new Queue<Token>();
        queue.Enqueue(new Token()
        {
            TokenType = Token.Type.Name,
            Value = "spucha"
        });
        queue.Enqueue(new Token()
        {
            TokenType = Token.Type.Assign,
        });
        queue.Enqueue(new Token()
        {
            TokenType = Token.Type.Value,
            Value = 10
        });
        return queue;
    }

    [Theory]
    [InlineData("spucha = 10")]
    public void TestParsing(string text)
    {
        var lexer = new Lexer();

        var tok = lexer.ProduceTokenStream(text);

        Assert.Equal(_queue, tok);
    }
}