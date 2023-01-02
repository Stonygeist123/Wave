using System.Collections.Immutable;
using Wave.Nodes;

namespace Wave
{
    internal class Parser
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _position = 0;
        private readonly DiagnosticBag _diagnostics = new();
        private readonly SourceText _source;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Parser(SourceText source)
        {
            Lexer lexer = new(source);
            List<Token> tokens = new();
            while (true)
            {
                Token t = lexer.GetToken();
                if (t.Kind != SyntaxKind.Bad && t.Kind != SyntaxKind.Space)
                    tokens.Add(t);

                if (t.Kind == SyntaxKind.Eof)
                    break;
            }

            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
            _source = source;
        }

        public CompilationUnit ParseCompilationUnit()
        {
            StmtNode expr = ParseStmt();
            Token eofToken = Match(SyntaxKind.Eof);
            return new(expr, eofToken);
        }

        private StmtNode ParseStmt()
        {
            if (Current.Kind == SyntaxKind.LBrace)
                return ParseBlockStmt();
            if (Current.Kind == SyntaxKind.Var)
                return ParseVarStmt();
            return ParseExprStmt();
        }

        private BlockStmt ParseBlockStmt()
        {
            ImmutableArray<StmtNode>.Builder stmts = ImmutableArray.CreateBuilder<StmtNode>();
            Token lBrace = Advance();
            while (Current.Kind != SyntaxKind.RBrace && Current.Kind != SyntaxKind.Eof)
                stmts.Add(ParseStmt());

            Token rBrace = Match(SyntaxKind.RBrace);
            return new(lBrace, stmts.ToImmutable(), rBrace);
        }

        private VarStmt ParseVarStmt()
        {
            Token keyword = Advance();
            Token? mutKw = Current.Kind == SyntaxKind.Mut ? Advance() : null;
            Token name = Match(SyntaxKind.Identifier);
            Token eqToken = Match(SyntaxKind.Eq);
            ExprNode value = ParseExpr();
            return new(keyword, mutKw, name, eqToken, value, Match(SyntaxKind.Semicolon));
        }

        private ExpressionStmt ParseExprStmt()
        {
            ExprNode expr = ParseExpr();
            return new(expr, Match(SyntaxKind.Semicolon));
        }

        private ExprNode ParseExpr() => ParseAssignmentExpr();
        private ExprNode ParseAssignmentExpr()
        {
            if (Current.Kind == SyntaxKind.Identifier && Peek(1).Kind == SyntaxKind.Eq)
            {
                Token id = Advance();
                Token eqToken = Advance();
                ExprNode right = ParseAssignmentExpr();
                return new AssignmentExpr(id, eqToken, right);
            }

            return ParseBinExpr();
        }

        private ExprNode ParseBinExpr(ushort parentPrecedence = 0)
        {
            ExprNode left;
            ushort unOpPrec = Current.Kind.GetUnOpPrecedence();
            if (unOpPrec != 0 && unOpPrec >= parentPrecedence)
            {
                Token op = Advance();
                left = new UnaryExpr(op, ParseBinExpr(unOpPrec));
            }
            else
                left = ParsePrimaryExpr();

            while (true)
            {
                ushort precendence = Current.Kind.GetBinOpPrecedence();
                if (precendence <= parentPrecedence)
                    break;

                Token op = Advance();
                ExprNode right = ParseBinExpr(precendence);
                left = new BinaryExpr(left, op, right);
            }

            return left;
        }


        private ExprNode ParsePrimaryExpr()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.LParen:
                    {
                        Token lParen = Advance();
                        ExprNode expr = ParseExpr();
                        Token rParen = Match(SyntaxKind.RParen);
                        return new GroupingExpr(lParen, expr, rParen);
                    }
                case SyntaxKind.Identifier:
                    {
                        Token id = Advance();
                        return new NameExpr(id);
                    }
                case SyntaxKind.True:
                case SyntaxKind.False:
                    return new LiteralExpr(Current, Advance().Kind == SyntaxKind.True);
                case SyntaxKind.Float:
                    return new LiteralExpr(Advance());
                default:
                    Token number = Match(SyntaxKind.Int);
                    return new LiteralExpr(number);
            }
        }

        private Token Match(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Advance();

            _diagnostics.Report(Current.Span, $"Unexpected token: \"{Current.Kind}\" - expected \"{kind}\".");
            return Current;
        }

        private Token Advance()
        {
            _position++;
            return Peek(-1);
        }

        private Token Peek(int offset) => _position + offset >= _tokens.Length ? _tokens.Last() : _tokens[_position + offset];
        private Token Current => Peek(0);
    }
}
