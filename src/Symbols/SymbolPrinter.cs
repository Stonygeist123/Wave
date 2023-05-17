using Wave.IO;
using Wave.Source.Binding.BoundNodes;
using Wave.Source.Syntax.Nodes;

namespace Wave.Symbols
{
    public static class SymbolPrinter
    {
        public static void WriteTo(this Symbol symbol, TextWriter writer)
        {
            switch (symbol)
            {
                case ParameterSymbol p:
                    writer.WriteIdentifier(p.Name);
                    writer.WritePunctuation(SyntaxKind.Colon);
                    writer.WriteSpace();
                    p.Type.WriteTo(writer);
                    break;
                case FieldSymbol f:
                    writer.WriteKeyword(f.Accessibility == Accessibility.Pub ? SyntaxKind.Pub : SyntaxKind.Priv);
                    writer.WriteSpace();
                    if (f.IsMut)
                    {
                        writer.WriteKeyword(SyntaxKind.Mut);
                        writer.WriteSpace();
                    }

                    if (f.IsStatic)
                        writer.WritePunctuation(SyntaxKind.Dot);

                    writer.WriteIdentifier(f.Name);
                    writer.WritePunctuation(SyntaxKind.Colon);
                    writer.WriteSpace();
                    f.Type.WriteTo(writer);
                    break;
                case VariableSymbol v:
                    writer.WriteKeyword(SyntaxKind.Var);
                    writer.WriteSpace();
                    if (v.IsMut)
                    {
                        writer.WriteKeyword(SyntaxKind.Mut);
                        writer.WriteSpace();
                    }

                    writer.WriteIdentifier(v.Name);
                    writer.WritePunctuation(SyntaxKind.Colon);
                    writer.WriteSpace();
                    v.Type.WriteTo(writer);
                    break;
                case TypeSymbol t:
                    writer.WriteIdentifier(t.Name);
                    if (t.IsArray)
                    {
                        writer.WritePunctuation(SyntaxKind.LBracket);
                        writer.WritePunctuation(SyntaxKind.RBracket);
                    }
                    break;
                case FunctionSymbol f:
                    writer.WriteKeyword(SyntaxKind.Fn);
                    writer.WriteSpace();
                    writer.WriteIdentifier(symbol.Name);
                    writer.WritePunctuation(SyntaxKind.LParen);
                    for (int i = 0; i < f.Parameters.Length; ++i)
                    {
                        if (i > 0)
                        {
                            writer.WritePunctuation(SyntaxKind.Comma);
                            writer.WriteSpace();
                        }

                        f.Parameters[i].WriteTo(writer);
                    }

                    writer.WritePunctuation(SyntaxKind.RParen);
                    writer.WriteSpace();
                    writer.WritePunctuation(SyntaxKind.Arrow);
                    writer.WriteSpace();
                    f.Type.WriteTo(writer);
                    break;
                case ADTSymbol adt:
                    writer.WriteKeyword(SyntaxKind.Type);
                    writer.WriteSpace();
                    writer.WriteIdentifier(symbol.Name);
                    writer.WriteSpace();
                    writer.WritePunctuation(SyntaxKind.LBrace);
                    foreach (KeyValuePair<string, BoundExpr> m in adt.Members)
                    {
                        if (m.Key != adt.Members.FirstOrDefault().Key)
                            writer.WritePunctuation(SyntaxKind.Comma);

                        writer.WriteSpace();
                        writer.WriteIdentifier(m.Key);
                        writer.WriteSpace();
                        writer.WritePunctuation(SyntaxKind.Eq);
                        writer.WriteSpace();
                        m.Value.WriteTo(writer);
                    }

                    writer.WriteSpace();
                    writer.WritePunctuation(SyntaxKind.RBrace);
                    break;
                case ClassSymbol c:
                    writer.WriteKeyword(SyntaxKind.Class);
                    writer.WriteSpace();
                    writer.WriteIdentifier(symbol.Name);
                    writer.WriteSpace();
                    writer.WritePunctuation(SyntaxKind.LBrace);
                    foreach ((MethodSymbol m, _) in c.Fns)
                    {
                        writer.WriteLine();
                        writer.WriteSpace();
                        writer.WriteSpace();
                        writer.WriteSpace();
                        writer.WriteSpace();
                        m.WriteTo(writer);
                    }

                    foreach ((FieldSymbol f, _) in c.Fields)
                    {
                        writer.WriteLine();
                        writer.WriteSpace();
                        writer.WriteSpace();
                        writer.WriteSpace();
                        writer.WriteSpace();
                        f.WriteTo(writer);
                    }

                    if (c.Fields.Any() || c.Fns.Any())
                        writer.WriteLine();
                    writer.WritePunctuation(SyntaxKind.RBrace);
                    break;
                case LabelSymbol:
                    break;
                default:
                    break;
            }
        }
    }
}
