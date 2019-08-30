using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.International.Converters.PinYinConverter;

namespace L4d2AddonsMgr.SearchSpace {

    public class SearchName {

        private static Word ReadWord(char[] arr, ref int offset) {
            var ch = arr[offset];
            while (char.IsWhiteSpace(ch)) {
                offset++;
                if (offset >= arr.Length) return null;
                ch = arr[offset];
            }
            if (char.IsLetter(ch)) {
                switch (char.GetUnicodeCategory(ch)) {
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.UppercaseLetter:
                    return ReadLatinWord(arr, ref offset);
                default:
                    offset++;
                    return ReadCjk(ch);
                }
            } else {
                return ReadPuncts(arr, ref offset);
            }
        }

        private static Word ReadLatinWord(char[] arr, ref int offset) {
            var sb = new StringBuilder();
            sb.Append(arr[offset]);
            for (var i = offset + 1; i < arr.Length; i++) {
                switch (char.GetUnicodeCategory(arr[i])) {
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    sb.Append(arr[i]);
                    break;
                default:
                    offset = i;
                    return Word.NewLatin(sb.ToString());
                }
            }
            offset = arr.Length;
            return Word.NewLatin(sb.ToString());
        }

        private static Word ReadCjk(char ch) {
            string[] latins;
            // Chinese char => Pinyin:
            // https://www.cnblogs.com/lonelyxmas/p/9697737.html
            if (ChineseChar.IsValidChar(ch)) {
                var zh = new ChineseChar(ch);
                latins = new string[zh.PinyinCount];
                for (var i = 0; i < zh.PinyinCount; i++) {
                    latins[i] = zh.Pinyins[i].ToLowerInvariant();
                }
            } else latins = new string[] { "unknown" };
            return Word.NewCjk(new string(ch, 1), latins);
        }

        private static Word ReadPuncts(char[] arr, ref int offset) {
            var sb = new StringBuilder();
            sb.Append(arr[offset]);
            for (var i = offset + 1; i < arr.Length; i++) {
                if (!char.IsLetter(arr[i]))
                    sb.Append(arr[i]);
                else {
                    offset = i;
                    return Word.NewPuncts(sb.ToString());
                }
            }
            offset = arr.Length;
            return Word.NewPuncts(sb.ToString());
        }

        private readonly List<Word> name;

        // Meow cat => [Meow, meow], [cat] Impl
        // MeowCat => [Meow, meow], [cat] Ignored
        // UIElement a => [UI, ui], [Element, element], [a] Ignored
        // meow喵cat => [meow], [喵, miao], [cat] Impl
        public SearchName(string name) {
            var arr = name.ToCharArray();
            this.name = new List<Word>();
            for (var i = 0; i < arr.Length;) {
                var word = ReadWord(arr, ref i);
                if (word == null) return;
                this.name.Add(word);
            }
        }

        public bool Match(string query) {
            return MatchRecursive(query, 0, 0);
        }

        private bool MatchRecursive(string query, int qPos, int myPos) {
            for (; myPos < name.Count; myPos++) {
                var word = name[myPos];
                if (MatchWord(query, qPos, myPos, false, word.word)) return true;
                if (MatchWord(query, qPos, myPos, true, word.lower ?? word.word)) return true;
                if (word.latins != null)
                    foreach (var latin in word.latins)
                        if (MatchWord(query, qPos, myPos, true, latin)) return true;
            }
            return false;
        }

        private bool MatchWord(string query, int qPos, int myPos, bool ignoreCase, string word) {
            for (var wPos = 0; qPos < query.Length; qPos++, wPos++) {
                if (wPos >= word.Length)
                    return MatchRecursive(query, qPos, myPos + 1);
                if (
                    // With ignoreCase on, word shall be lower by default.
                    (ignoreCase && word[wPos] != char.ToLower(query[qPos])) ||
                    (!ignoreCase && word[wPos] != query[qPos])) {
                    if (wPos == 0) return false;
                    return MatchRecursive(query, qPos, myPos + 1);
                }
            }
            return true;
        }

        public override string ToString() {
            if (name.Count <= 0) return "";
            var sb = new StringBuilder();
            foreach (var word in name) {
                sb.Append(word);
                sb.Append(' ');
            }
            sb.Length--;
            return sb.ToString();
        }

        private enum WordType {
            Latin, Cjk, Puncts
        }

        private class Word {

            public WordType type;
            public string word;
            public string lower;
            public string[] latins;

            public static Word NewLatin(string word) {
                var ret = new Word(WordType.Latin, word) {
                    lower = word.ToLowerInvariant()
                };
                if (ret.word == ret.lower) ret.lower = null;
                return ret;
            }

            public static Word NewCjk(string word, string[] latins)
                => new Word(WordType.Cjk, word) {
                    latins = latins
                };

            public static Word NewPuncts(string word)
                => new Word(WordType.Puncts, word);

            private Word(WordType type, string word) {
                this.type = type;
                this.word = word;
            }

            public override string ToString() {
                var sb = new StringBuilder();
                sb.Append('[');
                sb.Append(word);
                if (lower != null && lower != word)
                    sb.Append(',').Append(lower);
                if (latins != null)
                    foreach (var word in latins)
                        sb.Append(',').Append(word);
                sb.Append(']');
                return sb.ToString();
            }
        }
    }
}
