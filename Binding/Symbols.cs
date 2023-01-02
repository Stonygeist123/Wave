namespace Wave.Binding
{
    public readonly struct VariableSymbol
    {
        public VariableSymbol(string name, Type type, bool isMut)
        {
            Name = name;
            Type = type;
            IsMut = isMut;
        }

        public string Name { get; }
        public Type Type { get; }
        public bool IsMut { get; }
    }
}
