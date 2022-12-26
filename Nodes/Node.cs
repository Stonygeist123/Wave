namespace Wave.Nodes
{
    internal abstract class Node
    {
        public abstract SyntaxKind Kind { get; }
        public abstract IEnumerable<Node> GetChildren();
    }
}
