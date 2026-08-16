using Lucene.Net.Documents;
using Espluque.Contracts.Thesaurus;

namespace LuceneSearch
{
    public class SearchService : IDisposable, ISearchService
    {
        Lucene.Net.Store.RAMDirectory? _directory;

        private const int _minGram = 3;
        private const int _maxGram = 3;

        public async Task Index(IThesaurusService thesaurusService)
        {
            _directory?.Dispose();
            _directory = new Lucene.Net.Store.RAMDirectory();

            Lucene.Net.Index.IndexWriter writer;

            var analyzer = new NGramAnalyzer(_minGram, _maxGram);
            var configuration = new Lucene.Net.Index.IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = Lucene.Net.Index.OpenMode.CREATE
            };

            writer = new Lucene.Net.Index.IndexWriter(_directory, configuration);

            var concepts = await thesaurusService.GetConcepts();
            foreach (var concept in concepts)
            {
                foreach (var term in concept.Terms)
                {
                    if (string.IsNullOrWhiteSpace(term.Term))
                        continue;

                    var luceneDocument = new Document
                        {
                            new StoredField("ConceptId", concept.Id.Value),
                            new TextField( "Term", term.Term, Field.Store.YES)
                        };

                    writer.AddDocument(luceneDocument);
                }
            }

            writer.Commit();
            writer.Dispose();
            analyzer.Dispose();
        }

        public List<KeyValuePair<int, string>> Search(string searchedTerm, int maxResults)
        {
            if (_directory is null
                || string.IsNullOrWhiteSpace(searchedTerm)
                || (!Lucene.Net.Index.DirectoryReader.IndexExists(_directory)))
                return [];
            if (maxResults <= 0) maxResults = 10;

            searchedTerm = searchedTerm.Trim();

            List<string> nGrams = [];
            List<KeyValuePair<int, string>> searchResults = [];

            NGramAnalyzer? analyzer = null;
            Lucene.Net.Analysis.TokenStream? tokenStream = null;
            Lucene.Net.Index.DirectoryReader? reader = null;

            try
            {
                analyzer = new NGramAnalyzer(_minGram, _maxGram);
                tokenStream = analyzer.GetTokenStream("Term", searchedTerm);

                Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute termAttribute =
                    tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();

                tokenStream.Reset();
                Lucene.Net.Search.BooleanQuery nGramQuery = new Lucene.Net.Search.BooleanQuery();

                while (tokenStream.IncrementToken())
                {
                    string nGram = termAttribute.ToString();

                    if (!nGrams.Contains(nGram))
                    {
                        nGrams.Add(nGram);

                        Lucene.Net.Index.Term indexedNGram = new Lucene.Net.Index.Term("Term", nGram);
                        Lucene.Net.Search.TermQuery termQuery = new Lucene.Net.Search.TermQuery(indexedNGram);

                        nGramQuery.Add(termQuery, Lucene.Net.Search.Occur.SHOULD);
                    }
                }

                tokenStream.End();

                reader = Lucene.Net.Index.DirectoryReader.Open(_directory);
                if (reader.MaxDoc == 0) return searchResults;

                Lucene.Net.Search.IndexSearcher searcher = new Lucene.Net.Search.IndexSearcher(reader);
                Lucene.Net.Search.TopDocs results = searcher.Search(nGramQuery, reader.MaxDoc);

                foreach (Lucene.Net.Search.ScoreDoc scoreDoc in results.ScoreDocs)
                {
                    Lucene.Net.Documents.Document document = searcher.Doc(scoreDoc.Doc);
                    int? conceptId = document.GetField("ConceptId")?.GetInt32Value();
                    string? matchedTerm = document.Get("Term");

                    if (conceptId.HasValue && !string.IsNullOrWhiteSpace(matchedTerm) && !searchResults.Any(result => result.Key == conceptId.Value))
                        searchResults.Add(new KeyValuePair<int, string>(conceptId.Value, matchedTerm));

                    if (searchResults.Count >= maxResults)
                        break;
                }

            }
            finally
            {
                reader?.Dispose();
                tokenStream?.Dispose();
                analyzer?.Dispose();
            }
            return searchResults;
        }

        public void Dispose()
        {
            _directory?.Dispose();
            _directory = null;
        }
    }
}
