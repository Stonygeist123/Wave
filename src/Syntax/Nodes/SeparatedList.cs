using System.Collections;
using System.Collections.Immutable;

namespace Wave.Source.Syntax.Nodes
{
    public class SeparatedList<T> : IEnumerable<T>
        where T : Node
    {
        private readonly ImmutableArray<Node> _nodes;
        public SeparatedList(ImmutableArray<Node> nodes) => _nodes = nodes;
        public int Count => (_nodes.Length + 1) / 2;
        public T this[int index] => (T)_nodes[index * 2];
        public ImmutableArray<Node> GetWithSeps() => _nodes;
        public Token? GetSeparator(int index)
        {
            if (index == Count - 1)
                return null;
            return (Token)_nodes[index * 2 + 1];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }
    }
}