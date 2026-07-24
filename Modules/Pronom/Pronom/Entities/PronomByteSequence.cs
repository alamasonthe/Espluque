namespace Pronom.Entities
{
    public class PronomByteSequence
    {
        public string? Reference { get; set; }

        public string? Endianness { get; set; }

        public List<PronomSubSequence> SubSequences { get; set; } = [];
    }
}
