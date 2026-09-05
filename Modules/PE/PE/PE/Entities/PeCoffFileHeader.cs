namespace PE.Entities
{
    internal class PeCoffFileHeader
    {
        public PeField Machine { get; set; }
        public PeField NumberOfSections { get; set; }
        public PeField TimeDateStamp { get; set; }
        public PeField PointerToSymbolTable { get; set; }
        public PeField NumberOfSymbols { get; set; }
        public PeField SizeOfOptionalHeader { get; set; }
        public PeField Characteristics { get; set; }
    }
}
