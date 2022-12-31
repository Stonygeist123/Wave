namespace Wave
{
    public static class SyntaxFacts
    {
        public static ushort GetBinOpPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.Power => 9,
            SyntaxKind.Star or SyntaxKind.Slash or SyntaxKind.Mod => 8,
            SyntaxKind.Plus or SyntaxKind.Minus => 7,
            SyntaxKind.EqEq or SyntaxKind.NotEq => 6,
            SyntaxKind.And => 5,
            SyntaxKind.Xor => 4,
            SyntaxKind.Or => 3,
            SyntaxKind.LogicAnd => 2,
            SyntaxKind.LogicOr => 1,
            _ => 0,
        };

        public static ushort GetUnOpPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.Plus or SyntaxKind.Minus or SyntaxKind.Bang => 10,
            _ => 0,
        };

        public static SyntaxKind GetKeyWordKind(string text) => text switch
        {
            "true" => SyntaxKind.True,
            "false" => SyntaxKind.False,
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
            SyntaxKind.Bang => "!",
            SyntaxKind.LogicAnd => "&&",
            SyntaxKind.LogicOr => "||",
            SyntaxKind.EqEq => "==",
            SyntaxKind.NotEq => "!=",
            SyntaxKind.Eq => "=",
            SyntaxKind.True => "true",
            SyntaxKind.False => "false",
            _ => null,
        };
    }
}
