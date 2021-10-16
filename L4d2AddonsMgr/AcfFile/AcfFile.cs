using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

/*
 * Would this help?
 * https://github.com/leovp/steamfiles
 * Definately no why are you using regex for CFG parsing.
 * 
 * Create a parser? It should have been done within that very lesson. Not trying out till now.
 * http://lisperator.net/pltut/parser/
 * 
 * In other files `AcfFile.Node` shall be used instead of just `Node` nor `L4d2AddonsMgr.AcfFile.Node`.
 * How to? Not by namespaces they suck.
 * https://stackoverflow.com/questions/17449366/is-it-possible-to-do-relative-c-sharp-namespace-reference
 * https://stackoverflow.com/questions/12392876/class-vs-public-class
 * 
 * To use partial class `internal` was enforced.
 * What about test? Easy.
 * https://stackoverflow.com/questions/204739/what-is-the-c-sharp-equivalent-of-friend
 * 
 * Poor autocomplete vs. Need the cracked tomato.
 * https://blog.csdn.net/weixin_40539125/article/details/85596313
 * It's designed for enterprises so using it for free as an individual won't harm the tomato.
 */
[assembly: InternalsVisibleTo("L4d2AddonsMgrTest")]
namespace L4d2AddonsMgr.AcfFileSpace {

    /*
     * Internal instead of public?
     * https://stackoverflow.com/questions/165719/practical-uses-for-the-internal-keyword-in-c-sharp
     */
    internal partial class AcfFile {

        public static AcfFile ParseString(string input, bool forgive) => new AcfFile(input, forgive);

        public static AcfFile CreateNew() => new AcfFile();

        public static Node GetNode(Node node, string key) {
            if (!(node is CompoundNode cnode)) return null;
            foreach (var child in cnode.Value) {
                // String.Equals or ==?
                // https://stackoverflow.com/questions/814878/c-sharp-difference-between-and-equals
                if (key == child.Key) return child;
            }
            return null;
        }

        public static Node GetNodeIgnoreCase(Node node, string key) {
            if (!(node is CompoundNode cnode)) return null;
            foreach (var child in cnode.Value) {
                if (key.ToLowerInvariant() == child.Key?.ToLowerInvariant()) return child;
            }
            return null;
        }

        public CompoundNode Root { get; private set; }
        public bool HasError { get; private set; }

        private AcfFile() {
            HasError = false;
            Root = new CompoundNode(null, null);
        }

        private AcfFile(string input, bool forgive) {

            HasError = false;
            Root = new CompoundNode(null, null);

            const string ExpectedTokensNameK = "key string or \"}\"";

            var status = ParserStatus.ExpectingKeyOrRPar;
            var reader = new StringCodeReader(input);
            //if (reader.Peek() == 65279) reader.Read();
            var stack = new Stack<CompoundNode>(64);
            var isKeyNaked = false;
            string lastKey = null;
            var parent = Root;
            Token token;
            Node node;
            LeafNode lnode, comnode;
            CompoundNode cnode;
            ParserException e;
            do {
                token = Lexer.NextToken(reader, forgive);
                if (token.Type == TokenType.Failure) {
                    HasError = true;
                    return;
                }
                switch (status) {


                case ParserStatus.ExpectingKeyOrRPar:
                    switch (token.Type) {
                    case TokenType.StringType:
                    case TokenType.NakedString:
                        lastKey = token.Str;
                        isKeyNaked = token.Type == TokenType.NakedString;
                        status = ParserStatus.ExpectingLParOrVal;
                        break;
                    case TokenType.Comment:
                        comnode = new LeafNode(null, token.Str, parent) {
                            IsComment = true
                        };
                        parent.Add(comnode);
                        continue;
                    case TokenType.Eof:
                        if (stack.Count == 0) return;
                        e = new UnexpectedTokenException(
                            reader.LineNo, reader.Col, ExpectedTokensNameK, token.ToString());
                        PardonOrDeath(e, forgive);
                        return;
                    case TokenType.LstEnd:
                        if (stack.Count == 0) {
                            e = new UnexpectedTokenException(
                                reader.LineNo, reader.Col, ExpectedTokensNameK, token.ToString());
                            PardonOrDeath(e, forgive);
                            return;
                        }
                        parent = stack.Pop();
                        break;
                    default:
                        e = new UnexpectedTokenException(
                            reader.LineNo, reader.Col, ExpectedTokensNameK, token.ToString());
                        PardonOrDeath(e, forgive);
                        return;
                    }
                    break;


                case ParserStatus.ExpectingLParOrVal:
                    switch (token.Type) {
                    case TokenType.StringType:
                    case TokenType.NakedString:
                        lnode = new LeafNode(lastKey, token.Str, parent);
                        node = lnode;
                        lnode.IsKeyNaked = isKeyNaked;
                        lnode.IsValueNaked = token.Type == TokenType.NakedString;
                        parent.Add(node);
                        break;
                    case TokenType.Comment:
                        comnode = new LeafNode(null, token.Str, parent) {
                            IsComment = true
                        };
                        parent.Add(comnode);
                        continue;
                    case TokenType.LstBegin:
                        cnode = new CompoundNode(lastKey, parent) {
                            IsKeyNaked = isKeyNaked
                        };
                        node = cnode;
                        parent.Add(node);
                        stack.Push(parent);
                        parent = cnode;
                        break;
                    default:
                        e = new UnexpectedTokenException(
                            reader.LineNo, reader.Col,
                            "\"{\" or value string", token.ToString());
                        PardonOrDeath(e, forgive);
                        return;
                    }
                    status = ParserStatus.ExpectingKeyOrRPar;
                    break;
                }
            } while (true);
        }

        private void PardonOrDeath(ParserException e, bool forgive) {
            if (forgive) {
                Debug.WriteLine("Warning: Finishing Acf file parsing because an error was encountered.");
                HasError = true;
            }
            e.LogErrorString();
            if (!forgive) throw e;
        }

        /*
         * using arbitray many args.
         * https://stackoverflow.com/questions/16201210/does-net-have-an-equivalent-of-kwargs-in-python
         */
        public Node GetNodeByPath(params string[] path) {
            Node node = Root;
            foreach (var seg in path) {
                if (node == null) return null;
                node = GetNodeIgnoreCase(node, seg);
            }
            return node;
        }

        public string GetValueOfPath(params string[] path)
            => (GetNodeByPath(path) is LeafNode lnode) ? lnode.Value : null;

#if DEBUG
        public static string TestLexer(string input) {
            var stream = new StringCodeReader(input);
            Token token;
            var sb = new StringBuilder();
            do {
                token = Lexer.NextToken(stream, false);
                sb.Append(token.ToString());
            } while (token.Type != TokenType.Eof);
            return sb.ToString();
        }

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var node in Root.Value) {
                ToStringRecursive(sb, node, 0);
            }
            return sb.ToString();
        }

        private void ToStringRecursive(StringBuilder sb, Node node, int depth) {
            WriteIndent(sb, depth);
            if (node is LeafNode leaf) {
                if (leaf.IsComment) WriteComment(sb, leaf);
                else {
                    WriteKey(sb, node);
                    sb.Append("\t\t");
                    if (!leaf.IsValueNaked) sb.Append('"');
                    sb.Append(leaf.Value);
                    if (!leaf.IsValueNaked) sb.Append('"');
                }
            } else {
                WriteKey(sb, node);
                sb.AppendLine();
                WriteIndent(sb, depth);
                sb.AppendLine("{");
                var newDepth = depth + 1;
                foreach (var child in (node as CompoundNode).Value)
                    ToStringRecursive(sb, child, newDepth);
                WriteIndent(sb, depth);
                sb.Append('}');
            }
            sb.AppendLine();
        }

        private void WriteComment(StringBuilder sb, LeafNode node) {
            sb.Append("//");
            sb.Append(node.Value);
        }

        private void WriteKey(StringBuilder sb, Node node) {
            if (!node.IsKeyNaked) sb.Append('"');
            sb.Append(node.Key);
            if (!node.IsKeyNaked) sb.Append('"');
        }

        private void WriteIndent(StringBuilder sb, int depth) {
            for (var i = 0; i < depth; i++) sb.Append("\t");
        }
#endif

        private enum ParserStatus {
            ExpectingLParOrVal, ExpectingKeyOrRPar
        }
    }

}
