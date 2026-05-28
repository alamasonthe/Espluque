namespace PronomSqlite.Entities
{
    public class PronomInternalSignature
    {
        public int Id { get; set; }

        public List<PronomByteSequence> ByteSequences { get; set; } = [];
    }
}
