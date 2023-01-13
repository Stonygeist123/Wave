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
            ImmutableArray<Token> tokens = SyntaxTree.ParseTokens(source, out ImmutableArray<Diagnostic> diagnostics);
            _tokens = tokens.Where(t => t.Kind != SyntaxKind.Space).ToImmutableArray();
            _diagnostics.AddRange(diagnostics);
            _source = source;
        }

        public CompilationUnit ParseCompilationUnit()
        {
            ImmutableArray<MemberNode> members = ParseMembers();
            Token eofToken = Match(SyntaxKind.Eof);
            return new(members, eofToken);
        }

        private ImmutableArray<MemberNode> ParseMembers()
        {
            ImmutableArray<MemberNode>.Builder members = ImmutableArray.CreateBuilder<MemberNode>();
            while (Current.Kind != SyntaxKind.Eof)
            {
                Token start = Current;
                members.Add(ParseMember());

                if (Current == start)
                    Advance();
            }

            return members.ToImmutable();
        }

        private MemberNode ParseMember()
        {
            if (Current.Kind == SyntaxKind.Fn)
                return ParseFnDeclaration();
            return ParseGlobalStmt();
        }

        private GlobalStmt ParseGlobalStmt() => new(ParseStmt());
        private FnDeclStmt ParseFnDeclaration()
        {
            Token kw = Match(SyntaxKind.Fn);
            Token name = Match(SyntaxKind.Identifier);
            ParameterList? parameters = Current.Kind == SyntaxKind.LParen ? ParseParameterList() : null;
            TypeClause? typeClause = ParseOptTypeClause(SyntaxKind.Arrow);
            StmtNode body = ParseStmt();

            return new(kw, name, parameters, typeClause, body);
        }

        private ParameterNode ParseParameter() => new(Match(SyntaxKind.Identifier), ParseTypeClause(SyntaxKind.Colon));
        private ParameterList ParseParameterList()
        {
            Token lParen = Match(SyntaxKind.LParen);
            ImmutableArray<Node>.Builder parameters = ImmutableArray.CreateBuilder<Node>();
            bool parseNextParam = true;
            while (parseNextParam && Current.Kind != SyntaxKind.RParen && Current.Kind != SyntaxKind.Eof)
            {
                parameters.Add(ParseParameter());
                if (Current.Kind == SyntaxKind.Comma)
                    parameters.Add(Match(SyntaxKind.Comma));
                else
                    parseNextParam = false;
            }

            return new(lParen, new SeparatedList<ParameterNode>(parameters.ToImmutable()), Match(SyntaxKind.RParen));
        }

        private StmtNode ParseStmt() => Current.Kind switch
        {
            SyntaxKind.LBrace => ParseBlockStmt(),
            SyntaxKind.Var => ParseVarStmt(),
            SyntaxKind.If => ParseIfStmt(),
            SyntaxKind.While => ParseWhileStmt(),
            SyntaxKind.Do => ParseDoWhileStmt(),
            SyntaxKind.For => ParseForStmt(),
            _ => ParseExprStmt()
        };

        private BlockStmt ParseBlockStmt()
        {
            ImmutableArray<StmtNode>.Builder stmts = ImmutableArray.CreateBuilder<StmtNode>();
            Token lBrace = Match(SyntaxKind.LBrace);

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
            Token keyword = Match(SyntaxKind.Var);
            Token? mutKw = Current.Kind == SyntaxKind.Mut ? Advance() : null;
            Token name = Match(SyntaxKind.Identifier);
            TypeClause? typeClause = ParseOptTypeClause(SyntaxKind.Colon);
            Token eqToken = Match(SyntaxKind.Eq);
            ExprNode value = ParseExpr();
            return new(keyword, mutKw, name, typeClause, eqToken, value, Match(SyntaxKind.Semicolon));
        }

        private TypeClause ParseTypeClause(SyntaxKind sign) => new(Match(sign), Match(SyntaxKind.Identifier));
        private TypeClause? ParseOptTypeClause(SyntaxKind sign)
        {
            if (Current.Kind != sign)
                return null;

            return ParseTypeClause(sign);
        }

        private IfStmt ParseIfStmt()
        {
            Token kw = Match(SyntaxKind.If);
            ExprNode condition = ParseExpr();
            StmtNode thenBranch = ParseStmt();
            ElseClause? elseClause = Current.Kind == SyntaxKind.Else ? new(Advance(), ParseStmt()) : null;
            if (Current.Kind == SyntaxKind.Else)
                _diagnostics.Report(Current.Span, "Multiple else-clauses are not allowed.");

            return new(kw, condition, thenBranch, elseClause);
        }

        private WhileStmt ParseWhileStmt()
        {
            Token kw = Match(SyntaxKind.While);
            ExprNode condition = ParseExpr();
            StmtNode stmt = ParseStmt();
            return new(kw, condition, stmt);
        }

        private DoWhileStmt ParseDoWhileStmt()
        {
            Token kw = Match(SyntaxKind.Do);
            StmtNode stmt = ParseStmt();
            Token whileKw = Match(SyntaxKind.While);
            ExprNode condition = ParseExpr();
            return new(kw, stmt, whileKw, condition);
        }

        private ForStmt ParseForStmt()
        {
            Token kw = Match(SyntaxKind.For);
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
            return new(expr, Current.Kind == SyntaxKind.Semicolon ? Advance() : null);
        }

        private ExprNode ParseExpr() => ParseAssignmentExpr();
        private ExprNode ParseAssignmentExpr()
        {
            if (Current.Kind == SyntaxKind.Identifier && Peek(1).Kind == SyntaxKind.Eq)
            {
                Token id = Match(SyntaxKind.Identifier);
                Token eqToken = Match(SyntaxKind.Eq);
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
                bool parseNextArg = true;
                while (parseNextArg && Current.Kind != SyntaxKind.RParen && Current.Kind != SyntaxKind.Eof)
                {
                    args.Add(ParseExpr());
                    if (Current.Kind == SyntaxKind.Comma)
                        args.Add(Match(SyntaxKind.Comma));
                    else
                        parseNextArg = false;
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
