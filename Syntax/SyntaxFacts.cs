namespace Wave
{
    internal static class SyntaxFacts
    {
        public static ushort GetBinOpPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.Star or SyntaxKind.Slash or SyntaxKind.Mod => 5,
            SyntaxKind.Plus or SyntaxKind.Minus => 4,
            SyntaxKind.EqEq or SyntaxKind.NotEq => 3,
            SyntaxKind.LogicAnd => 2,
            SyntaxKind.LogicOr => 1,
            _ => 0,
        };

        public static ushort GetUnOpPrecedence(this SyntaxKind kind) => kind switch
        {
            SyntaxKind.Plus or SyntaxKind.Minus or SyntaxKind.Bang => 6,
            _ => 0,
        };

        internal static SyntaxKind GetKeyWordKind(string text) => text switch
        {
            "true" => SyntaxKind.True,
            "false" => SyntaxKind.False,
            _ => SyntaxKind.Identifier,
        };
    }
}
