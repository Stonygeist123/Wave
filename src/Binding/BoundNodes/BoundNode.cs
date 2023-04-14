using System.Reflection;

namespace Wave.Source.Binding.BoundNodes
{
    public enum BoundNodeKind
    {
        // Expr
        LiteralExpr,
        UnaryExpr,
        BinaryExpr,
        NameExpr,
        AssignmentExpr,
        CallExpr,
        ArrayExpr,
        IndexingExpr,
        ConversionExpr,
        ErrorExpr,

        // Stmt
        ExpressionStmt,
        BlockStmt,
        VarStmt,
        IfStmt,
        LoopStmt,
        WhileStmt,
        DoWhileStmt,
        ForStmt,
        ForEachStmt,
        LabelStmt,
        GotoStmt,
        CondGotoStmt,
        RetStmt,
        ErrorStmt
    }

    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
        public IEnumerable<BoundNode?> GetChildren()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType))
                    yield return (BoundNode?)property.GetValue(this);
                else if (typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<BoundNode>? children = (IEnumerable<BoundNode>?)property.GetValue(this);
                    if (children is not null)
                        foreach (BoundNode? child in children)
                            yield return child;
                }
            }
        }

        public IEnumerable<(string Name, object Value)> GetProps()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == nameof(Kind) || property.Name == nameof(BoundBinary.Op))
                    continue;

                if (typeof(BoundNode).IsAssignableFrom(property.PropertyType)
                    || typeof(IEnumerable<BoundNode>).IsAssignableFrom(property.PropertyType))
                    continue;

                object? value = property.GetValue(this);
                if (value is not null)
                    yield return (property.Name, value);
            }
        }

        public override string ToString()
        {
            using StringWriter writer = new();
            this.WriteTo(writer);
            return writer.ToString();
        }
    }
}
