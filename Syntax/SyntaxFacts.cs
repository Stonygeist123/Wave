namespace Wave
{
    public static class SyntaxFacts
    {
        public static ushort GetBinOpPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.Power => 9,
            SyntaxKind.Star or SyntaxKind.Slash or SyntaxKind.Mod => 8,
            SyntaxKind.Plus or SyntaxKind.Minus => 7,
            SyntaxKind.EqEq or SyntaxKind.NotEq or SyntaxKind.Greater or SyntaxKind.Less or SyntaxKind.GreaterEq or SyntaxKind.LessEq => 6,
            SyntaxKind.And => 5,
            SyntaxKind.Xor => 4,
            SyntaxKind.Or => 3,
            SyntaxKind.LogicAnd => 2,
            SyntaxKind.LogicOr => 1,
            _ => 0,
        };

        public static ushort GetUnOpPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.Plus or SyntaxKind.Minus or SyntaxKind.Bang or SyntaxKind.Inv => 10,
            _ => 0,
        };

        public static SyntaxKind GetKeyWordKind(string text) => text switch
        {
            "true" => SyntaxKind.True,
            "false" => SyntaxKind.False,
            "var" => SyntaxKind.Var,
            "mut" => SyntaxKind.Mut,
            "if" => SyntaxKind.If,
            "else" => SyntaxKind.Else,
            "while" => SyntaxKind.While,
            "for" => SyntaxKind.For,
            _ => SyntaxKind.Identifier,
        };

        public static string? GetLexeme(SyntaxKind kind) => kind switch
        {
            SyntaxKind.Space => " ",
            SyntaxKind.Plus => "+",
            SyntaxKind.Minus => "-",
            SyntaxKind.Star => "*",
            SyntaxKind.Slash => "/",
            SyntaxKind.Mod => "%",
            SyntaxKind.LParen => "(",
            SyntaxKind.RParen => ")",
            SyntaxKind.LBrace => "{",
            SyntaxKind.RBrace => "}",
            SyntaxKind.Bang => "!",
            SyntaxKind.And => "&",
            SyntaxKind.Or => "|",
            SyntaxKind.Xor => "^",
            SyntaxKind.Inv => "~",
            SyntaxKind.LogicAnd => "&&",
            SyntaxKind.LogicOr => "||",
            SyntaxKind.EqEq => "==",
            SyntaxKind.NotEq => "!=",
            SyntaxKind.Greater => ">",
            SyntaxKind.Less => "<",
            SyntaxKind.GreaterEq => ">=",
            SyntaxKind.LessEq => "<=",
            SyntaxKind.Eq => "=",
            SyntaxKind.Semicolon => ";",
            SyntaxKind.Arrow => "->",
            SyntaxKind.True => "true",
            SyntaxKind.False => "false",
            SyntaxKind.Var => "var",
            SyntaxKind.Mut => "mut",
            SyntaxKind.While => "while",
            SyntaxKind.For => "for",
            _ => null,
        };
    }
}
