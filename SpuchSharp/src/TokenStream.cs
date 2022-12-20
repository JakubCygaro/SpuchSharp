using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Parsing;

//internal sealed class TokenStream
//{
//    private List<Token> _tokens;
//    private int _lenght;
//    private int _position;

//    public TokenStream(List<Token> tokens)
//    {
//        _tokens = tokens;
//        _lenght = _tokens.Count;
//        _position = 0;
//    }
//    public TokenStream(Token[] tokens) : this (tokens.ToList()) { }

//    public Token? Next()
//    {
//        if (_position >= _lenght) return null;
//        var token = _tokens[_position];
//        _position++;
//        return token;
//    }
//}
