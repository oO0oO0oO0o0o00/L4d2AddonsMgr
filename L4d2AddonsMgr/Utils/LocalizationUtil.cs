using System.Text.RegularExpressions;

namespace L4d2AddonsMgr.Utils {

    public class LocalizationUtil {

        private static readonly Regex cjkRegex;

        // https://stackoverflow.com/questions/16415074/detecting-cjk-characters-in-a-string-c
        static LocalizationUtil() {
            cjkRegex = new Regex(
                @"\p{IsHangulJamo}|" +
                @"\p{IsCJKRadicalsSupplement}|" +
                @"\p{IsCJKSymbolsandPunctuation}|" +
                @"\p{IsEnclosedCJKLettersandMonths}|" +
                @"\p{IsCJKCompatibility}|" +
                @"\p{IsCJKUnifiedIdeographsExtensionA}|" +
                @"\p{IsCJKUnifiedIdeographs}|" +
                @"\p{IsHangulSyllables}|" +
                @"\p{IsCJKCompatibilityForms}");
        }

        public static bool HasCjkCharacter(string text) => cjkRegex.IsMatch(text);
    }
}
