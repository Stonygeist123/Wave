using Wave.IO;
using Wave.Source.Syntax.Nodes;

namespace Wave.Symbols
{
    internal static class SymbolPrinter
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
                    writer.WriteLine();
                    break;
                case LabelSymbol:
                    break;
                default:
                    break;
            }
        }
    }
}
