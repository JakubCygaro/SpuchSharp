using System;
using System.Collections.Generic;
using System.Linq;
using SpuchSharp.Tokens;
using System.IO;
using System.Collections;
using SpuchSharp;
using SpuchSharp.Parsing;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace SpuchSharp.Lexing;

internal sealed class Lexer
{
    internal static TokenStream Tokenize(string source, string sourcePath) 
    {
        return new Lexer(new CharStream(source, sourcePath)).Tokenize();
    }
    public static TokenStream Tokenize(string source)
    {
        return new Lexer(new CharStream(source)).Tokenize();
    }

    enum TypeFlag
    {
        NONE = 0,
        SHORT,
        INT,
        LONG,
        FLOAT,
        DOUBLE,
        TEXT,
    }
    private TypeFlag LastTypeFlag { get; set; } = TypeFlag.NONE;


    CharStream _charStream;

    public Lexer(CharStream charStream)
    {
        _charStream = charStream;
    }

    public TokenStream Tokenize()
    {
        List<Token> tokens = new();
        Token token;
        while ((token = Lex()) is not EOFToken)
        {
            Location loc = new Location()
            {
                Column = (ulong)_startColumn,
                Line = (ulong)_startLine,
                File = _charStream.SourceFile,
            };
            token.Location = loc;
            tokens.Add(token);
        }
        return tokens.ToTokenStream();
    }
    int _startColumn;
    int _startLine;
    internal Token Lex()
    {
        while (_charStream.Next() is char character)
        {
            _startColumn = _charStream.Column;
            _startLine = _charStream.Line;

            switch (character)
            {
                case ' ':
                case '\t':
                case '\r':
                    continue;

                case '\n':
                    continue;

                case '#':
                    while (_charStream.Next() is char c)
                    {
                        if (c == '\n')
                            break;
                    }
                    continue;

                //case for ident
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    var token = ScanForIdentOrKeyWord();
                    return token;

                //case for text literal
                case '"':
                    return ScanForText();

                case ';':
                    LastTypeFlag = TypeFlag.NONE;
                    return new Semicolon();

                case '{':
                    return new Curly.Open();
                case '}':
                    return new Curly.Closed();

                case '(':
                    return new Round.Open();
                case ')':
                    return new Round.Closed();

                case '[':
                    return new Square.Open();
                case ']':
                    return new Square.Closed();

                case '.':
                    return new Dot();

                case ',':
                    return new Comma();

                case '=':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new Equality();
                    }
                    else
                        return new Assign();

                case ':':
                    if (_charStream.PeekNext() == ':')
                    {
                        _charStream.MoveNext();
                        return new Colon2();
                    }
                    else
                        return new Colon();

                case '!':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new InEquality();
                    }
                    else
                        return new Exclam();

                case '>':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new GreaterOrEq();
                    }
                    else
                        return new Greater();

                case '<':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new LessOrEq();
                    }
                    else
                        return new Less();

                case '+':
                    if (_charStream.PeekNext() == '+')
                    {
                        _charStream.MoveNext();
                        return new Add2();
                    }
                    else if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new AssignAdd();
                    }
                    return new Add();

                case '-':
                    if (_charStream.PeekNext() == '-')
                    {
                        _charStream.MoveNext();
                        return new Sub2();
                    }
                    else if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new AssignSub();
                    }
                    else if (_charStream.PeekNext() is char posDigit)
                    {
                        if (char.IsDigit(posDigit))
                            return ScanForNumberLiteral();
                    }
                    return new Sub();

                case '/':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new AssignDiv();
                    }
                    return new Div();

                case '*':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new AssignMul();
                    }
                    return new Mult();
                case '%':
                    if (_charStream.PeekNext() == '=')
                    {
                        _charStream.MoveNext();
                        return new AssignModulo();
                    }
                    return new Percent();

                case '&':
                    if (_charStream.PeekNext() == '&')
                    {
                        _charStream.MoveNext();
                        return new And();
                    }
                    else
                        return new Ampersand();

                case '|':
                    if (_charStream.PeekNext() == '|')
                    {
                        _charStream.MoveNext();
                        return new Or();
                    }
                    else
                        return new Pipe();

                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '0':
                    try
                    {
                        return ScanForNumberLiteral();
                    }
                    catch (LexerException le)
                    {
                        throw le;
                    }
                    catch (Exception)
                    {
                        throw new LexerException("Failed to parse number literal at",
                            (ulong)_charStream.Line,
                            (ulong)_charStream.Column);
                    }


                default:
                    var here = new Location()
                    {
                        Column = (ulong)_startColumn,
                        Line = (ulong)_startLine,
                        File = _charStream.SourceFile,
                    };
                    throw new LexerException($"Unallowed character `{character} at {here}`");
            }
        }

        return EOFToken.Instance;
    }
    Token ScanForIdentOrKeyWord()
    {
        int startPos = _charStream.Tell();
        int end = startPos;
        while(_charStream.PeekNext() is char next)
        {
            if(char.IsAsciiLetter(next) ||
                char.IsAsciiDigit(next) ||
                next == '_')
            {
                _charStream.MoveNext();
                end++;
            }
            else
                break;
        }
        int length = end - startPos;

        _charStream.SeekFromStart(startPos);
        var value = _charStream.ReadToSpan(length);

        if (KeyWord.From(value) is KeyWord keyWord)
        {
            if (keyWord is Var)
                LastTypeFlag = TypeFlag.NONE;
            return keyWord;
        }
        else if (Ty.From(value) is Ty type)
        {
            LastTypeFlag = type switch
            {
                ShortTy => TypeFlag.SHORT,
                IntTy => TypeFlag.INT,
                LongTy => TypeFlag.LONG,
                FloatTy => TypeFlag.FLOAT,
                DoubleTy => TypeFlag.DOUBLE,
                TextTy => TypeFlag.TEXT,
                _ => TypeFlag.NONE,
            };
            //type.Location = loc;
            return type;
        }
        else if (MemoryExtensions.Equals(value, "false", StringComparison.Ordinal))
        {
            return new BooleanValue { Value = false };
        }
        else if (MemoryExtensions.Equals(value, "true", StringComparison.Ordinal))
        {
            return new BooleanValue { Value = true };
        }
        else
            return new Ident
            {
                Value = value.ToString(),
            };
    }
    Token ScanForText()
    {
        int startPos = _charStream.Tell() + 1;
        int end = startPos;
        bool open = true;

        while(_charStream.Next() is char c)
        {
            if (c == '"')
            {
                open = false;
                break;
            }
            if (c == '\\')
            {
                if (_charStream.Next() is null)
                    throw new LexerException("Premature end of input");
                end += 2;
            }
            else
            {
                end++;
            }
        }
        if (open)
            throw new ParserException("Unclosed parentheses", new Location
            {
                Column = (ulong)_charStream.Column,
                Line = (ulong)_charStream.Line,
                File = _charStream.SourceFile
            });
        var length = end - startPos;
        string value;
        if (length == 0)
            value = string.Empty;
        else
        {
            _charStream.SeekFromStart(startPos);
            value = _charStream.ReadToString(length-1);
            value = ScanEscape(value);
        }
        _charStream.MoveNext();

        return new TextValue(value)
        {
            Location = new Location 
            {
                Column = (ulong)_startColumn,
                Line = (ulong)_startLine,
                File = _charStream.SourceFile
            }
        };
    }
    string ScanEscape(string s)
    {
        var span = s.AsSpan();
        if(span.Length < 2040)
        {
            Span<char> ret = stackalloc char[span.Length];
            for(int i = 0; i < span.Length; i++)
            {
                if (span[i] == '\\')
                    ret[i] = CleanEscapes(span[++i]);
                else 
                    ret[i] = span[i];
            }
            return ret.ToString();
        }
        else
        {
            var ret = new char[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '\\')
                    ret[i] = CleanEscapes(span[++i]);
                else
                    ret[i] = span[i];
            }
            var str = new string(ret);
            return str;
        }
    }
    char CleanEscapes(char c)
    {
        switch (c)
        {
            case 'n':
                return '\n';
            case 't':
                return '\t';
            case 'r':
                return '\r';
            case '0':
                return '\0';
            case 'v':
                return '\v';
            case '\\':
                return '\\';
            case '"':
                return '"';
            case '\'':
                return '\'';
            case 'b':
                return '\b';
            case 'f':
                return '\f';
            case 'a':
                return '\a';
            default:
                throw new LexerException("Unnknown escape sequence");
        }
    }
    Token ScanForNumberLiteral()
    {
        //int column = charStream.Column;
        var start = _charStream.Tell();
        var end = start;
        bool dot = false;
        bool @long = false;
        bool @float = false;
        while (_charStream.PeekNext() is char next)
        {
            if (char.IsDigit(next))
            {
                _charStream.MoveNext();
                end++;
            }
            else if (next == '.')
            {
                if (dot)
                    throw new ParserException("Invalid number literal format", new Location
                    {
                        Column = (ulong)_charStream.Column,
                        Line = (ulong)_charStream.Line,
                        File = _charStream.SourceFile
                    });
                
                _charStream.MoveNext();
                end++;
                dot = true;
            }
            else if (next == 'f')
            {
                if (!dot)
                    dot = true;
                @float = true;
                _charStream.MoveNext();
                //end++;
                break;
            }
            else if (next == 'L')
            {
                if (!@long)
                    @long = true;
                _charStream.MoveNext();
                //end++;
                break;
            }
            else
                break;
        }
        //Value ret;
        //if(!dot)
        //    switch (LastTypeFlag)
        //    {
        //        case TypeFlag.INT:
        //            ret = new IntValue
        //            {
        //                Value = int.Parse(new string(literal.ToArray()),
        //                        System.Globalization.NumberFormatInfo.InvariantInfo)
        //            };
        //            break;

        //        case TypeFlag.SHORT:
        //            ret = new ShortValue
        //            {
        //                Value = short.Parse(new string(literal.ToArray()),
        //                        System.Globalization.NumberFormatInfo.InvariantInfo)
        //            };
        //            break;

        //        case TypeFlag.LONG:
        //            ret = new LongValue
        //            {
        //                Value = long.Parse(new string(literal.ToArray()),
        //                        System.Globalization.NumberFormatInfo.InvariantInfo)
        //            };
        //            break;

        //        //default to int
        //        case TypeFlag.NONE:
        //            ret = new IntValue
        //            {
        //                Value = int.Parse(new string(literal.ToArray()),
        //                        System.Globalization.NumberFormatInfo.InvariantInfo)
        //            };
        //            break;

        //        default:
        //            throw new LexerException(
        //                $"Invalid number literal format `{new string(literal.ToArray())}`",
        //                charStream.LineNumber,
        //                charStream.Column);
        //    }
        //else
        //    switch (LastTypeFlag)
        //    {
        //        case TypeFlag.FLOAT:
        //            ret = new FloatValue
        //            {
        //                Value = float.Parse(new string(literal.ToArray()),
        //                        System.Globalization.NumberFormatInfo.InvariantInfo)
        //            };
        //            break;
        //        case TypeFlag.DOUBLE:
        //            ret = new DoubleValue
        //            {
        //                Value = double.Parse(new string(literal.ToArray()),
        //                        System.Globalization.NumberFormatInfo.InvariantInfo)
        //            };
        //            break;

        //        default:
        //            throw new LexerException(
        //                $"Invalid number literal format `{new string(literal.ToArray())}`", 
        //                charStream.LineNumber,
        //                charStream.Column);
        //    }
        //LastTypeFlag = TypeFlag.NONE;
        var length = end - start;
        _charStream.SeekFromStart(start);
        var literal = _charStream.ReadToSpan(length);
        if (@long || @float)
            _charStream.MoveNext();
        Value ret;
        if (dot)
        {
            ret = new FloatValue
            {
                Value = float.Parse(literal,
                                System.Globalization.NumberFormatInfo.InvariantInfo)
            };
        }
        else if (@long)
        {
            ret = new LongValue
            {
                Value = long.Parse(literal,
                                System.Globalization.NumberFormatInfo.InvariantInfo)
            };
        }
        else
        {
            ret = new IntValue
            {
                Value = int.Parse(literal,
                                System.Globalization.NumberFormatInfo.InvariantInfo)
            };
        }

        return ret;
    }

}
