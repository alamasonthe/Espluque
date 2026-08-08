using Espluque.Contracts.Interfaces;
using System.Globalization;
using System.Windows.Data;

namespace Espluquer.UserControls.Shell
{
    public class SearchTermsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not IEnumerable<IThesaurusTerm> terms)
                return string.Empty;

            string? matchedTerm = values[1] as string;
            string? preferredTerm = terms.FirstOrDefault(term => term.IsPreferred)?.Term;

            List<string> displayTerms = [];

            if (!string.IsNullOrWhiteSpace(matchedTerm) && !string.Equals(matchedTerm, preferredTerm, StringComparison.OrdinalIgnoreCase))
                displayTerms.Add(matchedTerm.Trim());

            foreach (IThesaurusTerm term in terms)
            {
                if (string.IsNullOrWhiteSpace(term.Term)
                    || string.Equals(term.Term, preferredTerm, StringComparison.OrdinalIgnoreCase)
                    || displayTerms.Any(value => string.Equals(value, term.Term, StringComparison.OrdinalIgnoreCase)))
                    continue;

                displayTerms.Add(term.Term.Trim());
            }

            int maxDisplayedTerms = 3;

            if (parameter is string parameterValue && int.TryParse(parameterValue, out int parsedValue) && parsedValue > 0)
                maxDisplayedTerms = parsedValue;

            List<string> displayedTerms = displayTerms.Take(maxDisplayedTerms).ToList();
            int hiddenCount = displayTerms.Count - displayedTerms.Count;

            string result = string.Join(" · ", displayedTerms);

            if (hiddenCount > 0)
                result += $" · +{hiddenCount}";

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
        }
    }
}
