using Lucene.Net.Analysis;

namespace LuceneSearch
{
    internal class NGramAnalyzer : Analyzer
    {
        private readonly int _minGram;
        private readonly int _maxGram;

        public NGramAnalyzer(int minGram, int maxGram)
        {
            _minGram = minGram;
            _maxGram = maxGram;
        }

        protected override TokenStreamComponents CreateComponents( string fieldName, TextReader reader)
        {
            var tokenizer = new Lucene.Net.Analysis.NGram.NGramTokenizer(
                Lucene.Net.Util.LuceneVersion.LUCENE_48,
                reader,
                _minGram,
                _maxGram);

            var lowerCaseFilter = new Lucene.Net.Analysis.Core.LowerCaseFilter(
                Lucene.Net.Util.LuceneVersion.LUCENE_48,
                tokenizer);

            return new TokenStreamComponents(tokenizer, lowerCaseFilter);
        }
    }
}
