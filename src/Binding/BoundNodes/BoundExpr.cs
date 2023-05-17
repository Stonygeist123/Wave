using System.Collections.Immutable;
using Wave.Symbols;

namespace Wave.Source.Binding.BoundNodes
{
    public enum BoundUnOpKind
    {
        Plus,
        Minus,
        Bang,
        Inv
    }

    public enum BoundBinOpKind
    {
        Plus,
        Minus,
        Star,
        Slash,
        Power,
        Mod,
        And,
        Or,
        Xor,
        LogicAnd,
        LogicOr,
        EqEq,
        NotEq,
        Greater,
        Less,
        GreaterEq,
        LessEq
    }

    public abstract class BoundExpr : BoundNode
    {
        public abstract TypeSymbol Type { get; }
    }

    public sealed class BoundLiteral : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpr;
        public object Value { get; }
        public BoundLiteral(object value)
        {
            Value = value;
            if (value is int)
                Type = TypeSymbol.Int;
            else if (value is double)
                Type = TypeSymbol.Float;
            else if (value is bool)
                Type = TypeSymbol.Bool;
            else if (value is string)
                Type = TypeSymbol.String;
            else
                throw new Exception($"Unexpected literal \"{value}\" of type \"{value.GetType()}\".");
        }
    }

    public sealed class BoundUnary : BoundExpr
    {
        public override TypeSymbol Type => Op.ResultType;
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpr;
        public BoundUnOperator Op { get; }
        public BoundExpr Operand { get; }
        public BoundUnary(BoundUnOperator op, BoundExpr operand)
        {
            Op = op;
            Operand = operand;
        }
    }

    public sealed class BoundBinary : BoundExpr
    {
        public override TypeSymbol Type => Op.ResultType;
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpr;
        public BoundExpr Left { get; }
        public BoundBinOperator Op { get; }
        public BoundExpr Right { get; private set; }

        public BoundBinary(BoundExpr left, BoundBinOperator op, BoundExpr right)
        {
            Left = left;
            Op = op;
            Right = right;
        }
    }

    public sealed class BoundName : BoundExpr
    {
        public override TypeSymbol Type => Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.NameExpr;
        public VariableSymbol Variable { get; }
        public BoundName(VariableSymbol variable) => Variable = variable;
    }

    public sealed class BoundAssignment : BoundExpr
    {
        public override TypeSymbol Type => Value.Type;
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpr;
        public VariableSymbol Variable { get; }
        public BoundExpr Value { get; }
        public bool IsArray { get; }
        public BoundAssignment(VariableSymbol variable, BoundExpr value, bool isArray = false)
        {
            Variable = variable;
            Value = value;
            IsArray = isArray;
        }
    }

    public sealed class BoundArrayAssignment : BoundExpr
    {
        public override TypeSymbol Type => Value.Type;
        public override BoundNodeKind Kind => BoundNodeKind.ArrayAssignmentExpr;
        public VariableSymbol Variable { get; }
        public BoundExpr Index { get; }
        public BoundExpr Value { get; }
        public BoundArrayAssignment(VariableSymbol variable, BoundExpr index, BoundExpr value)
        {
            Variable = variable;
            Index = index;
            Value = value;
        }
    }

    public sealed class BoundCall : BoundExpr
    {
        public override TypeSymbol Type => Function.Type;
        public override BoundNodeKind Kind => BoundNodeKind.CallExpr;
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpr> Args { get; }
        public NamespaceSymbol? NamespaceSymbol { get; }
        public BoundCall(FunctionSymbol function, ImmutableArray<BoundExpr> args, NamespaceSymbol? namespaceSymbol)
        {
            Function = function;
            Args = args;
            NamespaceSymbol = namespaceSymbol;
        }
    }

    public sealed class BoundArray : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ArrayExpr;
        public ImmutableArray<BoundExpr> Elements { get; }
        public BoundArray(ImmutableArray<BoundExpr> elements, TypeSymbol type)
        {
            Elements = elements;
            Type = type;
        }
    }

    public sealed class BoundIndexing : BoundExpr
    {
        public override TypeSymbol Type => new(Expr.Type.Name, false);
        public override BoundNodeKind Kind => BoundNodeKind.IndexingExpr;
        public BoundExpr Expr { get; }
        public BoundExpr Index { get; }
        public BoundIndexing(BoundExpr expr, BoundExpr index)
        {
            Expr = expr;
            Index = index;
        }
    }

    public sealed class BoundEnumIndexing : BoundExpr
    {
        public override TypeSymbol Type => TypeSymbol.String;
        public override BoundNodeKind Kind => BoundNodeKind.EnumIndexingExpr;
        public ADTSymbol ADT { get; }
        public BoundExpr Index { get; }
        public BoundEnumIndexing(ADTSymbol adt, BoundExpr index)
        {
            ADT = adt;
            Index = index;
        }
    }

    public sealed class BoundConversion : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ConversionExpr;
        public BoundExpr Expr { get; }
        public BoundConversion(TypeSymbol type, BoundExpr expr)
        {
            Type = new(type.Name);
            Expr = expr;
        }
    }

    public sealed class BoundInstance : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.InstanceExpr;
        public string Name { get; }
        public ImmutableArray<BoundExpr> Args { get; }
        public NamespaceSymbol? NamespaceSymbol { get; }
        public BoundInstance(string name, ImmutableArray<BoundExpr> args, NamespaceSymbol? namespaceSymbol)
        {
            Type = new(name, false, true, false, namespaceSymbol);
            Name = name;
            Args = args;
            NamespaceSymbol = namespaceSymbol;
        }
    }

    public sealed class BoundEnumGet : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.EnumGetExpr;
        public ADTSymbol EnumSymbol { get; }
        public string Member { get; }

        public BoundEnumGet(ADTSymbol enumSymbol, string member, NamespaceSymbol? namespaceSymbol)
        {
            Type = new(enumSymbol.Name, false, false, true, namespaceSymbol);
            EnumSymbol = enumSymbol;
            Member = member;
        }
    }

    public sealed class BoundGet : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.GetExpr;
        public VariableSymbol? Id { get; }
        public FieldSymbol Field { get; }
        public NamespaceSymbol? NamespaceSymbol { get; }
        public bool HasInstance { get; }
        public BoundGet(VariableSymbol? id, FieldSymbol field, NamespaceSymbol? namespaceSymbol, bool hasInstance)
        {
            Type = field.Type;
            Id = id;
            Field = field;
            NamespaceSymbol = namespaceSymbol;
            HasInstance = hasInstance;
        }
    }

    public sealed class BoundMethod : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.MethodExpr;
        public VariableSymbol? Id { get; }
        public MethodSymbol Function { get; }
        public ImmutableArray<BoundExpr> Args { get; }
        public BoundMethod(VariableSymbol? id, MethodSymbol function, ImmutableArray<BoundExpr> args)
        {
            Type = function.Type;
            Id = id;
            Function = function;
            Args = args;
        }
    }

    public sealed class BoundSet : BoundExpr
    {
        public override TypeSymbol Type { get; }
        public override BoundNodeKind Kind => BoundNodeKind.SetExpr;
        public VariableSymbol? Id { get; }
        public FieldSymbol Field { get; }
        public BoundExpr Value { get; }
        public bool HasInstance { get; }
        public BoundSet(VariableSymbol? id, FieldSymbol field, BoundExpr value, bool hasInstance)
        {
            Type = field.Type;
            Id = id;
            Field = field;
            Value = value;
            HasInstance = hasInstance;
        }
    }

    public sealed class BoundError : BoundExpr
    {
        public override TypeSymbol Type => TypeSymbol.Unknown;
        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpr;
    }
}
