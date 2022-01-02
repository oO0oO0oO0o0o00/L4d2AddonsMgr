using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        /*
         * Static?
         * http://adam-porat.blogspot.com/2013/04/java-static-class-vs-c-static-class.html
         */
        static class Lexer {

            // Const vs static readonly.
            // https://stackoverflow.com/questions/441420/why-does-c-sharp-limit-the-set-of-types-that-can-be-declared-as-const
            private static readonly Token EofToken = new Token(TokenType.Eof, null);
            private static readonly Token FailureToken = new Token(TokenType.Failure, null);
            private static readonly Token LstBeginToken = new Token(TokenType.LstBegin, null);
            private static readonly Token LstEndToken = new Token(TokenType.LstEnd, null);

            private static void PardonOrDeath(ParserException e, bool forgive) {
                if (forgive)
                    Debug.WriteLine("Warning: Finishing Acf file parsing because an error was encountered.");
                e.LogErrorString();
                if (!forgive) throw e;
            }

            public static Token NextToken(StringCodeReader charStream, bool forgive) {
                // Assuming no emoji.
                // May need this later?
                // https://stackoverflow.com/questions/32895131/how-to-compare-and-convert-emoji-characters-in-c-sharp
                char ch;
                do {
                    var chVal = charStream.Read();
                    if (chVal < 0) return EofToken;
                    ch = (char)chVal;
                } while (char.IsWhiteSpace(ch));
                switch (ch) {
                case '{': return LstBeginToken;
                case '}': return LstEndToken;
                case '"': return ContinueReadingStringToken(charStream, forgive);
                case '/': return ContinueReadingComment(charStream, forgive);
                default:
                    if (char.IsLetterOrDigit(ch)) return ContinueReadingNakedStringToken(charStream, ch, forgive);
                    var e = new UnexpectedCharacterException(
                        charStream.LineNo, charStream.Col, "beginning of any token", ch);
                    PardonOrDeath(e, forgive);
                    return FailureToken;
                }
            }

            private static Token ContinueReadingStringToken(StringCodeReader charStream, bool forgive) {
                var escaping = false;
                var openedQuote = false;
                var sb = new StringBuilder();
                while (true) {
                    var chVal = charStream.Read();
                    if (chVal < 0) {
                        var e = new UnexpectedCharacterException(
                            charStream.LineNo, charStream.Col, "continuation of string literal", (char)0);
                        PardonOrDeath(e, forgive);
                        return FailureToken;
                    }
                    var ch = (char)chVal;
                    if (escaping) {
                        ReadEscapedChar(ch, sb);
                        escaping = false;
                    } else if (ch == '"') {
                        if (openedQuote) {
                            sb.Append('"');
                            openedQuote = false;
                        } else {
                            var chPeek = (char)charStream.Peek();
                            if (char.IsWhiteSpace(chPeek))
                                return new Token(TokenType.StringType, sb.ToString());
                            else {
                                openedQuote = true;
                                sb.Append('"');
                            }
                        }
                    } else if (ch == '\\') escaping = true;
                    else sb.Append(ch);
                }
            }

            private static Token ContinueReadingNakedStringToken(StringCodeReader charStream, char firstChar, bool forgive) {
                var sb = new StringBuilder();
                sb.Append(firstChar);
                char ch;
                while (true) {
                    var chVal = charStream.Read();
                    if (chVal < 0) {
                        var e = new UnexpectedCharacterException(
                            charStream.LineNo, charStream.Col, "continuation of naked string (or decimal)", (char)0);
                        PardonOrDeath(e, forgive);
                        return FailureToken;
                    }
                    ch = (char)chVal;
                    if (char.IsWhiteSpace((ch))) break;
                    sb.Append(ch);
                }
                return new Token(TokenType.NakedString, sb.ToString());
            }

            private static Token ContinueReadingComment(StringCodeReader charStream, bool forgive) {
                char ch;
                if ((ch = (char)charStream.Read()) != '/') {
                    var e = new UnexpectedCharacterException(charStream.LineNo, charStream.Col - 1, "beginning of any token", ch);
                    PardonOrDeath(e, forgive);
                    return FailureToken;
                }
                var sb = new StringBuilder();
                while (true) {
                    var chVal = charStream.Read();
                    if (chVal < 0) {
                        var e = new UnexpectedCharacterException(
                            charStream.LineNo, charStream.Col, "continuation of comment", (char)0);
                        PardonOrDeath(e, forgive);
                        return FailureToken;
                    }
                    ch = (char)chVal;
                    if (IsLineSeparator(ch)) break;
                    sb.Append(ch);
                }
                return new Token(TokenType.Comment, sb.ToString());
            }

            private static bool IsLineSeparator(char ch) {
                if (ch == '\r' || ch == '\n') return true;
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.LineSeparator || cat == UnicodeCategory.ParagraphSeparator) return true;
                return false;
            }

            private static void ReadEscapedChar(char ch, StringBuilder sb) {
                switch (ch) {
                case '\\':
                    sb.Append('\\');
                    break;
                case 'n':
                    sb.Append('\n');
                    break;
                case 'r':
                    break;
                case 't':
                    sb.Append('\t');
                    break;
                case '"':
                    sb.Append('"');
                    break;
                default:
                    sb.Append('\\');
                    sb.Append(ch);
                    break;
                }
            }
        }
    }
}
