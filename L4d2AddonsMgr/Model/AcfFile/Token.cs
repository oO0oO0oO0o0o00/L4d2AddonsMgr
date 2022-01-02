namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        struct Token {

            public TokenType Type { get; }
            public string Str { get; }

            public Token(TokenType type, string str) {
                Type = type;
                Str = str;
            }

            public override string ToString() {
                switch (Type) {
                    case TokenType.StringType:
                        return "\"" + Str + "\"";
                    case TokenType.NakedString:
                        return "'" + Str + "'";
                    case TokenType.Comment:
                        return "\"//" + Str + "\"";
                    case TokenType.LstBegin:
                        return "\"{\"";
                    case TokenType.LstEnd:
                        return "\"}\"";
                    case TokenType.Eof:
                        return "[EOF]";
                }
                return null;
            }

        }

    }
}
