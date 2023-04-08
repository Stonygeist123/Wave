using System.CodeDom.Compiler;
using Wave.Source.Binding.BoundNodes;
using Wave.Source.Syntax.Nodes;
using Wave.Symbols;

namespace Wave.Source.Binding
{
    public sealed class ControlFlowGraph
    {
        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks, List<BasicBlockBranch> edges)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = edges;
        }

        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        public sealed class BasicBlock
        {
            public BasicBlock() { }
            public BasicBlock(bool isStart)
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }

            public bool IsStart { get; }
            public bool IsEnd { get; }
            public List<BoundStmt> Stmts { get; } = new();
            public List<BasicBlockBranch> Incoming { get; } = new();
            public List<BasicBlockBranch> Outgoing { get; } = new();
            public override string ToString()
            {
                if (IsStart)
                    return "<Start>";


                if (IsEnd)
                    return "<End>";


                using StringWriter sw = new();
                using IndentedTextWriter itw = new(sw);
                foreach (BoundStmt stmt in Stmts)
                    stmt.WriteTo(itw);

                return itw.ToString()!;
            }
        }

        public sealed class BasicBlockBranch
        {
            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpr? condition)
            {
                From = from;
                To = to;
                Condition = condition;
            }

            public BasicBlock From { get; }
            public BasicBlock To { get; }
            public BoundExpr? Condition { get; }
            public override string ToString() => Condition is null ? string.Empty : Condition.ToString();
        }

        public sealed class BasicBlockBuilder
        {
            private readonly List<BoundStmt> _stmts = new();
            private readonly List<BasicBlock> _blocks = new();
            public List<BasicBlock> Build(BoundBlockStmt blocks)
            {
                foreach (BoundStmt stmt in blocks.Stmts)
                {
                    switch (stmt.Kind)
                    {
                        case BoundNodeKind.LabelStmt:
                            StartBlock();
                            _stmts.Add(stmt);
                            break;
                        case BoundNodeKind.CondGotoStmt:
                        case BoundNodeKind.GotoStmt:
                        case BoundNodeKind.RetStmt:
                            _stmts.Add(stmt);
                            StartBlock();
                            break;
                        case BoundNodeKind.VarStmt:
                        case BoundNodeKind.ExpressionStmt:
                            _stmts.Add(stmt);
                            break;
                        default:
                            throw new Exception($"Unexpected statement \"${stmt.Kind}\".");
                    }
                }

                EndBlock();

                return _blocks.ToList();
            }

            private void StartBlock() => EndBlock();
            private void EndBlock()
            {
                if (_stmts.Count > 0)
                {
                    BasicBlock block = new();
                    block.Stmts.AddRange(_stmts);
                    _blocks.Add(block);
                    _stmts.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private readonly Dictionary<BoundStmt, BasicBlock> _blockFromStmt = new();
            private readonly Dictionary<LabelSymbol, BasicBlock> _blockFromLabel = new();
            private readonly List<BasicBlockBranch> _branches = new();
            private readonly BasicBlock _start = new(true), _end = new(false);

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                if (blocks.Any())
                    Connect(_start, blocks.First());
                else
                    Connect(_start, _end);

                foreach ((BasicBlock block, BoundStmt stmt) in from BasicBlock block in blocks
                                                               from BoundStmt stmt in block.Stmts
                                                               select (block, stmt))
                {
                    _blockFromStmt.Add(stmt, block);
                    if (stmt is BoundLabelStmt l)
                        _blockFromLabel.Add(l.Label, block);
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    BasicBlock current = blocks[i];
                    BasicBlock next = i == blocks.Count - 1 ? _end : blocks[i + 1];

                    foreach (BoundStmt stmt in current.Stmts)
                    {
                        bool isLastStmt = stmt == current.Stmts.Last();
                        switch (stmt.Kind)
                        {
                            case BoundNodeKind.CondGotoStmt:
                                BoundCondGotoStmt cgs = (BoundCondGotoStmt)stmt;

                                BoundExpr negCond = Negate(cgs.Condition);
                                BoundExpr thenCond = cgs.JumpIfTrue ? cgs.Condition : negCond;
                                BoundExpr elseCond = cgs.JumpIfTrue ? negCond : cgs.Condition;

                                Connect(current, _blockFromLabel[cgs.Label], thenCond);
                                Connect(current, next, elseCond);
                                break;
                            case BoundNodeKind.GotoStmt:
                                Connect(current, _blockFromLabel[((BoundGotoStmt)stmt).Label]);
                                break;
                            case BoundNodeKind.RetStmt:
                                Connect(current, _end);
                                break;
                            case BoundNodeKind.LabelStmt:
                            case BoundNodeKind.VarStmt:
                            case BoundNodeKind.ExpressionStmt:
                                if (isLastStmt)
                                    Connect(current, next);
                                break;
                            default:
                                throw new Exception($"Unexpected statement \"${stmt.Kind}\".");
                        }
                    }
                }

            ScanAgain:
                foreach (BasicBlock block in blocks)
                {
                    if (!block.Incoming.Any())
                    {
                        RemoveBlock(ref blocks, block);
                        goto ScanAgain;
                    }
                }

                blocks.Insert(0, _start);
                blocks.Add(_end);
                return new(_start, _end, blocks, _branches);
            }

            private void RemoveBlock(ref List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (BasicBlockBranch branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    _branches.Remove(branch);
                }

                foreach (BasicBlockBranch branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    _branches.Remove(branch);
                }

                blocks.Remove(block);
            }

            private static BoundExpr Negate(BoundExpr condition)
            {
                if (condition is BoundLiteral l)
                    return new BoundLiteral(!(bool)l.Value);

                BoundUnOperator unOp = BoundUnOperator.Bind(SyntaxKind.Bang, TypeSymbol.Bool)!;
                return new BoundUnary(unOp, condition);
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpr? condition = null)
            {
                if (condition is BoundLiteral l)
                {
                    if ((bool)l.Value)
                        condition = null;
                    else
                        return;
                }

                BasicBlockBranch branch = new(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                _branches.Add(branch);
            }
        }

        public void WriteTo(TextWriter writer)
        {
            static string Quote(string text) => "\"" + text.TrimEnd().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\l") + "\"";

            writer.WriteLine("digraph G {");
            Dictionary<BasicBlock, string> blockIds = new();
            for (int i = 0; i < Blocks.Count; ++i)
                blockIds.Add(Blocks[i], $"N{i}");

            foreach (BasicBlock block in Blocks)
            {
                string id = blockIds[block];
                string label = Quote(block.ToString());
                writer.WriteLine($"    {id} [label = {label}, shape = box]");
            }

            foreach (BasicBlockBranch branch in Branches)
            {
                string fromId = blockIds[branch.From];
                string toId = blockIds[branch.To];
                string label = Quote(branch.Condition?.ToString() ?? "");
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph CreateGraph(BoundBlockStmt body)
        {
            BasicBlockBuilder blockBuilder = new();
            List<BasicBlock> blocks = blockBuilder.Build(body);
            GraphBuilder graphBuilder = new();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStmt body)
        {
            ControlFlowGraph graph = CreateGraph(body);
            foreach (BasicBlockBranch branch in graph.End.Incoming)
            {
                BoundStmt? lastStmt = branch.From.Stmts.LastOrDefault();
                if (lastStmt is null || lastStmt.Kind != BoundNodeKind.RetStmt)
                    return false;
            }

            return true;
        }
    }
}
