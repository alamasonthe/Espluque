namespace PE.Entities
{
    public class PeOffsets
    {
        public long NtHeader { get; set; }
        public long FileHeader { get; set; }
        public long OptionalHeader { get; set; }
        public long DataDirectory { get; set; }
        public long SectionHeaders { get; set; }
        public long ResourceSection { get; set; }
        public uint ResourceSectionRva { get; set; }
    }
}
