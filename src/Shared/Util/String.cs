using System.Text;
using System.Text.RegularExpressions;

namespace Util
{
    public class String
    {
        public static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string[] words = Regex.Split(value.Trim(), @"\s+");

            StringBuilder stringBuilder = new();

            foreach (string word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                stringBuilder.Append(char.ToUpperInvariant(word[0]));

                if (word.Length > 1)
                {
                    stringBuilder.Append(word[1..].ToLowerInvariant());
                }
            }

            return stringBuilder.ToString();
        }

    }
}
