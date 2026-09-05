namespace PE.Entities
{
    internal class PeHeader
    {
        public PeField PeSignature { get; set; }
        public PeCoffFileHeader CoffFileHeader { get; set; }
        public PeOptionalHeader OptionalHeader { get; set; }
    }
}
