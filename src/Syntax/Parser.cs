using System.Collections.Immutable;
using Wave.Source.Syntax.Nodes;

namespace Wave.Source.Syntax
{
    public class Parser
    {
        private readonly ImmutableArray<Token> _tokens;
        private int _position = 0;
        private readonly DiagnosticBag _diagnostics = new();
        private readonly SyntaxTree _syntaxTree;

        public DiagnosticBag Diagnostics => _diagnostics;

        public Parser(SyntaxTree syntaxTree)
        {
            Lexer lexer = new(syntaxTree);
            ImmutableArray<Token>.Builder tokens = ImmutableArray.CreateBuilder<Token>();
            while (true)
            {
                Token token = lexer.GetToken();
                tokens.Add(token);
                if (token.Kind == SyntaxKind.Eof)
                    break;
            }

            _diagnostics.AddRange(lexer.Diagnostics);
            _tokens = tokens.Where(t => t.Kind != SyntaxKind.Space && t.Kind != SyntaxKind.Comment).ToImmutableArray();
            _syntaxTree = syntaxTree;
        }

        public CompilationUnit ParseCompilationUnit()
        {
            ImmutableArray<MemberNode> members = ParseMembers();
            Token eofToken = Match(SyntaxKind.Eof);
            return new(_syntaxTree, members, eofToken);
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

        private GlobalStmt ParseGlobalStmt() => new(_syntaxTree, ParseStmt());
        private FnDeclStmt ParseFnDeclaration()
        {
            Token kw = Match(SyntaxKind.Fn);
            Token name = Match(SyntaxKind.Identifier);
            ParameterList? parameters = Current.Kind == SyntaxKind.LParen ? ParseParameterList() : null;
            TypeClause? typeClause = ParseOptTypeClause(SyntaxKind.Arrow);
            StmtNode body = ParseStmt();
            if (body.Kind == SyntaxKind.ExpressionStmt && Peek(-1).Kind != SyntaxKind.Semicolon)
                Match(SyntaxKind.Semicolon, $"A semicolon is expected after a function declaration when the body is an expression.");

            return new(_syntaxTree, kw, name, parameters, typeClause, body);
        }

        private ParameterNode ParseParameter() => new(_syntaxTree, Match(SyntaxKind.Identifier), ParseTypeClause(SyntaxKind.Colon));
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

            return new(_syntaxTree, lParen, new SeparatedList<ParameterNode>(parameters.ToImmutable()), Match(SyntaxKind.RParen));
        }

        private StmtNode ParseStmt() => Current.Kind switch
        {
            SyntaxKind.LBrace => ParseBlockStmt(),
            SyntaxKind.Var => ParseVarStmt(),
            SyntaxKind.If => ParseIfStmt(),
            SyntaxKind.While => ParseWhileStmt(),
            SyntaxKind.Do => ParseDoWhileStmt(),
            SyntaxKind.For => ParseForStmt(),
            SyntaxKind.Break => ParseBreakStmt(),
            SyntaxKind.Continue => ParseContinueStmt(),
            SyntaxKind.Ret => ParseRetStmt(),
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
            return new(_syntaxTree, lBrace, stmts.ToImmutable(), rBrace);
        }

        private VarStmt ParseVarStmt()
        {
            Token keyword = Match(SyntaxKind.Var);
            Token? mutKw = Current.Kind == SyntaxKind.Mut ? Advance() : null;
            Token name = Match(SyntaxKind.Identifier);
            TypeClause? typeClause = ParseOptTypeClause(SyntaxKind.Colon);
            Token eqToken = Match(SyntaxKind.Eq);
            ExprNode value = ParseExpr();
            return new(_syntaxTree, keyword, mutKw, name, typeClause, eqToken, value, Match(SyntaxKind.Semicolon));
        }

        private TypeClause ParseTypeClause(SyntaxKind sign) => new(_syntaxTree, Match(sign), Match(SyntaxKind.Identifier));
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
            ElseClause? elseClause = Current.Kind == SyntaxKind.Else ? new(_syntaxTree, Advance(), ParseStmt()) : null;
            if (Current.Kind == SyntaxKind.Else)
                _diagnostics.Report(Current.Location, "Multiple else-clauses are not allowed.");

            return new(_syntaxTree, kw, condition, thenBranch, elseClause);
        }

        private WhileStmt ParseWhileStmt()
        {
            Token kw = Match(SyntaxKind.While);
            ExprNode condition = ParseExpr();
            StmtNode stmt = ParseStmt();
            return new(_syntaxTree, kw, condition, stmt);
        }

        private DoWhileStmt ParseDoWhileStmt()
        {
            Token kw = Match(SyntaxKind.Do);
            StmtNode stmt = ParseStmt();
            Token whileKw = Match(SyntaxKind.While);
            ExprNode condition = ParseExpr();
            return new(_syntaxTree, kw, stmt, whileKw, condition);
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
            return new(_syntaxTree, kw, id, eqToken, lowerBound, arrow, upperBound, stmt);
        }

        private BreakStmt ParseBreakStmt()
        {
            Token kw = Match(SyntaxKind.Break);
            Token semi = Match(SyntaxKind.Semicolon);
            return new(_syntaxTree, kw, semi);
        }

        private ContinueStmt ParseContinueStmt()
        {
            Token kw = Match(SyntaxKind.Continue);
            Token semi = Match(SyntaxKind.Semicolon);
            return new(_syntaxTree, kw, semi);
        }

        private RetStmt ParseRetStmt()
        {
            Token kw = Match(SyntaxKind.Ret);
            ExprNode? value = Current.Kind == SyntaxKind.Semicolon ? null : ParseExpr();
            Token semi = Match(SyntaxKind.Semicolon);
            return new(_syntaxTree, kw, value, semi);
        }

        private ExpressionStmt ParseExprStmt()
        {
            ExprNode expr = ParseExpr();
            return new(_syntaxTree, expr, Current.Kind == SyntaxKind.Semicolon ? Advance() : null);
        }

        private ExprNode ParseExpr() => ParseAssignmentExpr();
        private ExprNode ParseAssignmentExpr()
        {
            if (Current.Kind == SyntaxKind.Identifier && Peek(1).Kind == SyntaxKind.Eq)
            {
                Token id = Match(SyntaxKind.Identifier);
                Token eqToken = Match(SyntaxKind.Eq);
                ExprNode right = ParseAssignmentExpr();
                return new AssignmentExpr(_syntaxTree, id, eqToken, right);
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
                left = new UnaryExpr(_syntaxTree, op, ParseBinExpr(unOpPrec));
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
                left = new BinaryExpr(_syntaxTree, left, op, right);
            }

            return left;
        }


        private ExprNode ParsePrimaryExpr()
        {
            return Current.Kind switch
            {
                SyntaxKind.LParen => new GroupingExpr(_syntaxTree, Advance(), ParseExpr(), Match(SyntaxKind.RParen)),
                SyntaxKind.True or SyntaxKind.False => new LiteralExpr(_syntaxTree, Current, Advance().Kind == SyntaxKind.True),
                SyntaxKind.Int => new LiteralExpr(_syntaxTree, Match(SyntaxKind.Int)),
                SyntaxKind.Float => new LiteralExpr(_syntaxTree, Advance()),
                SyntaxKind.String => new LiteralExpr(_syntaxTree, Advance()),
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

                return new CallExpr(_syntaxTree, callee, lParen, new SeparatedList<ExprNode>(args.ToImmutable()), Match(SyntaxKind.RParen));
            }

            return new NameExpr(_syntaxTree, Match(SyntaxKind.Identifier));
        }

        private Token Match(SyntaxKind kind, string? msg = null)
        {
            if (Current.Kind == kind)
                return Advance();

            _diagnostics.Report(Current.Location, msg ?? $"Got \"{Current.Lexeme}\" - expected \"{kind.GetLexeme() ?? kind.ToString()}\".");
            return new(_syntaxTree, kind, Current.Position, null, null);
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
