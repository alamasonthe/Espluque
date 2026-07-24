namespace Pronom.Entities
{
    public class PronomSubSequence
    {
        public int? MinFragLength { get; set; }

        public int Position { get; set; }

        public int? SubSeqMaxOffset { get; set; }

        public int SubSeqMinOffset { get; set; }

        public string Sequence { get; set; } = string.Empty;

        public List<PronomFragment> Fragments { get; set; } = [];
    }
}
