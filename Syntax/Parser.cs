using System.Collections.Immutable;
using Wave.Nodes;
using Wave.Syntax.Nodes;

namespace Wave
{
    internal class Parser
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _position = 0;
        private readonly DiagnosticBag _diagnostics = new();
#pragma warning disable IDE0052 // Remove unread private members
        private readonly SourceText _source;
#pragma warning restore IDE0052 // Remove unread private members

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

        private StmtNode ParseStmt() => Current.Kind switch
        {
            SyntaxKind.LBrace => ParseBlockStmt(),
            SyntaxKind.Var => ParseVarStmt(),
            SyntaxKind.If => ParseIfStmt(),
            SyntaxKind.While => ParseWhileStmt(),
            SyntaxKind.For => ParseForStmt(),
            _ => ParseExprStmt()
        };

        private BlockStmt ParseBlockStmt()
        {
            ImmutableArray<StmtNode>.Builder stmts = ImmutableArray.CreateBuilder<StmtNode>();
            Token lBrace = Advance();

            while (Current.Kind != SyntaxKind.RBrace && Current.Kind != SyntaxKind.Eof)
            {
                Token startToken = Current;
                stmts.Add(ParseStmt());
                if (startToken == Current)
                    Advance();
            }

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

        private IfStmt ParseIfStmt()
        {
            Token kw = Advance();
            ExprNode condition = ParseExpr();
            StmtNode thenBranch = ParseStmt();
            ElseClause? elseClause = Current.Kind == SyntaxKind.Else ? new(Advance(), ParseStmt()) : null;
            if (Current.Kind == SyntaxKind.Else)
                _diagnostics.Report(Current.Span, "Multiple else-clauses are not allowed.");

            return new(kw, condition, thenBranch, elseClause);
        }

        private WhileStmt ParseWhileStmt()
        {
            Token kw = Advance();
            ExprNode condition = ParseExpr();
            StmtNode stmt = ParseStmt();
            return new(kw, condition, stmt);
        }

        private ForStmt ParseForStmt()
        {
            Token kw = Advance();
            Token id = Match(SyntaxKind.Identifier);
            Token eqToken = Match(SyntaxKind.Eq);
            ExprNode lowerBound = ParseExpr();
            Token arrow = Match(SyntaxKind.Arrow);
            ExprNode upperBound = ParseExpr();
            StmtNode stmt = ParseStmt();
            return new(kw, id, eqToken, lowerBound, arrow, upperBound, stmt);
        }

        private ExpressionStmt ParseExprStmt()
        {
            ExprNode expr = ParseExpr();
            if (Current.Kind == SyntaxKind.Semicolon)
                Advance();

            return new(expr);
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
            return Current.Kind switch
            {
                SyntaxKind.LParen => new GroupingExpr(Advance(), ParseExpr(), Match(SyntaxKind.RParen)),
                SyntaxKind.True or SyntaxKind.False => new LiteralExpr(Current, Advance().Kind == SyntaxKind.True),
                SyntaxKind.Int => new LiteralExpr(Match(SyntaxKind.Int)),
                SyntaxKind.Float => new LiteralExpr(Advance()),
                SyntaxKind.String => new LiteralExpr(Advance()),
                SyntaxKind.Identifier or _ => ParseIdentifier(),
            };
        }

        private ExprNode ParseIdentifier()
        {
            if (Current.Kind == SyntaxKind.Identifier && Peek(1).Kind == SyntaxKind.LParen)
            {

                Token callee = Match(SyntaxKind.Identifier);
                Token lParen = Match(SyntaxKind.LParen);
                ImmutableArray<Node>.Builder args = ImmutableArray.CreateBuilder<Node>();
                while (Current.Kind != SyntaxKind.RParen && Current.Kind != SyntaxKind.Eof)
                {
                    args.Add(ParseExpr());
                    if (Current.Kind != SyntaxKind.RParen)
                        args.Add(Match(SyntaxKind.Comma));
                }

                return new CallExpr(callee, lParen, new SeparatedList<ExprNode>(args.ToImmutable()), Match(SyntaxKind.RParen));
            }

            return new NameExpr(Match(SyntaxKind.Identifier));
        }

        private Token Match(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return Advance();

            _diagnostics.Report(Current.Span, $"Got \"{SyntaxFacts.GetLexeme(Current.Kind) ?? Current.Kind.ToString()}\" - expected \"{SyntaxFacts.GetLexeme(kind) ?? kind.ToString()}\".");
            return new(kind, Current.Position, null, null);
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
