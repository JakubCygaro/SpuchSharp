using System;
using System.Collections.Generic;
using System.Linq;
using SpuchSharp.Tokens;
using System.IO;
using System.Collections;
using SpuchSharp;
using SpuchSharp.Parsing;

namespace SpuchSharp.Lexing;

internal sealed class Lexer
{
    internal static TokenStream Tokenize(string[] lines, string sourcePath) 
    {
        return new Lexer().Tokenize(new CharStream(lines, sourcePath));
    }
    public static TokenStream Tokenize(string[] lines)
    {
        return new Lexer().Tokenize(new CharStream(lines));
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

    public TokenStream Tokenize(CharStream charStream)
    {
        List<Token> tokens = new();
        while (charStream.Next() is char character)
        {
            if (character == ' ')
                continue;
            if (character == '#')
            {
                if (charStream.SkipLine())
                    continue;
                else
                    break;
            }
            var start = new Location
            {
                Column = charStream.Column,
                Line = charStream.LineNumber,
                File = charStream.SourceFile,
            };
            var token = Lex(character, charStream);
            token.Location = start;
            tokens.Add(token);
        }
        return tokens.ToTokenStream();
    }
    internal Token Lex(char first, CharStream charStream)
    {
        switch (first)
        {
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
                var token =  ScanForIdentOrKeyWord(ref first, charStream);
                if(token is Ident ident)
                {
                    if(ident == "true")
                        return new BooleanValue { Value = true };
                    else if (ident == "false")
                        return new BooleanValue { Value = false };
                }
                return token;

            //case for text literal
            case '"':
                return ScanForText(ref first, charStream);

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
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new Equality();
                }
                else
                    return new Assign();

            case ':':
                if (charStream.PeekNext() == ':')
                {
                    charStream.MoveNext();
                    return new Colon2();
                }
                else
                    return new Colon();

            case '!':
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new InEquality();
                }
                else
                    return new Exclam();

            case '>':
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new GreaterOrEq();
                }
                else
                    return new Greater();

            case '<':
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new LessOrEq();
                }
                else
                    return new Less();

            case '+':
                if(charStream.PeekNext() == '+')
                {
                    charStream.MoveNext();
                    return new Add2();
                }
                else if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new AssignAdd();
                }
                return new Add();

            case '-':
                if (charStream.PeekNext() is char posDigit)
                {
                    if (char.IsDigit(posDigit))
                        return ScanForNumberLiteral(ref first, charStream);
                }
                else if (charStream.PeekNext() == '-')
                {
                    charStream.MoveNext();
                    return new Sub2();
                }
                else if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new AssignSub();
                }
                return new Sub();

            case '/':
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new AssignDiv();
                }
                return new Div();

            case '*':
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new AssignMul();
                }
                return new Mult();
            case '%':
                if (charStream.PeekNext() == '=')
                {
                    charStream.MoveNext();
                    return new AssignModulo();
                }
                return new Percent();

            case '&':
                if (charStream.PeekNext() == '&')
                {
                    charStream.MoveNext();
                    return new And();
                }
                else
                    return new Ampersand();

            case '|':
                if (charStream.PeekNext() == '|')
                {
                    charStream.MoveNext();
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
                    return ScanForNumberLiteral(ref first, charStream);
                }
                catch (LexerException le)
                {
                    throw le;
                }
                catch(Exception) 
                {
                    throw new LexerException("Failed to parse number literal at", 
                        charStream.LineNumber,
                        charStream.Column);
                }
            

            default:
                throw new LexerException($"Unallowed character `{first}`");
        }

    }
    Token ScanForIdentOrKeyWord(ref char first, CharStream charStream)
    {
        int startPos = charStream.Column;
        List<char> literal = new() { first };
        while(charStream.PeekNext() is char next)
        {
            if(char.IsAsciiLetter(next) ||
                char.IsAsciiDigit(next) ||
                next == '_')
            {
                literal.Add((char)charStream.Next()!);
            }
            else
                break;
        }
        var value = new string(literal.ToArray());

        if(KeyWord.From(value) is KeyWord keyWord)
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

            return type;
        }
        return new Ident
        {
            Value = value,
            Location = new() { Column = startPos, Line = charStream.LineNumber, File = charStream.SourceFile }
        };
    }
    Token ScanForText(ref char first, CharStream charStream)
    {
        int startPos = charStream.Column;
        bool open = true;
        List<char> contents = new();
        while(charStream.Next() is char c)
        {
            if (c == '"')
            {
                open = false;
                break;
            }
            else if (c == '\\')
            {
                var next = charStream.Next() ??
                    throw new ParserException("Premature end of input", new Location
                    {
                        Column = charStream.Column,
                        Line = charStream.LineNumber,
                        File = charStream.SourceFile
                    });
                contents.Add(ScanEscape(next));
            }
            else
            {
                contents.Add(c);
            }
        }
        if (open)
            throw new ParserException("Unclosed parentheses", new Location
            {
                Column = charStream.Column,
                Line = charStream.LineNumber,
                File = charStream.SourceFile
            });
        return new TextValue
        {
            Value = new string(contents.ToArray()),
            Location = new Location 
            { 
                Column = startPos,
                Line = charStream.LineNumber,
                File = charStream.SourceFile
            }
        };
    }
    char ScanEscape(char special)
    {
        switch (special) 
        {
            case '\'':
                return '\'';
            case '"':
                return '"';
            case '\\':
                return '\\';
            case '0':
                return '\0';
            case 'a':
                return '\a';
            case 'b':
                return '\b';
            case 'f':
                return '\f';
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case 'v':
                return '\v';
            default:
                throw new ParserException("Unrecognized escape sequence");

        }
    }
    Token ScanForNumberLiteral(ref char first, CharStream charStream)
    {
        //int column = charStream.Column;
        List<char> literal = new() { first };
        bool dot = false;
        bool @long = false;
        while (charStream.PeekNext() is char next)
        {
            if (char.IsDigit(next))
            {
                charStream.MoveNext();
                literal.Add(next);
            }
            else if (next == '.')
            {
                if (dot)
                    throw new ParserException("Invalid number literal format", new Location
                    {
                        Column = charStream.Column,
                        Line = charStream.LineNumber,
                        File = charStream.SourceFile
                    });
                
                charStream.MoveNext();
                literal.Add(next);
                dot = true;
            }
            else if (next == 'f')
            {
                if (!dot)
                    dot = true;
                charStream.MoveNext();
                break;
            }
            else if (next == 'L')
            {
                if (!@long)
                    @long = true;
                charStream.MoveNext();
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

        Value ret;
        if (dot)
        {
            ret = new FloatValue
            {
                Value = float.Parse(new string(literal.ToArray()),
                                System.Globalization.NumberFormatInfo.InvariantInfo)
            };
        }
        else if (@long)
        {
            ret = new LongValue
            {
                Value = long.Parse(new string(literal.ToArray()),
                                System.Globalization.NumberFormatInfo.InvariantInfo)
            };
        }
        else
        {
            ret = new IntValue
            {
                Value = int.Parse(new string(literal.ToArray()),
                                System.Globalization.NumberFormatInfo.InvariantInfo)
            };
        }

        return ret;
    }

}
