namespace PE.Entities
{
    internal class PeFile
    {
        public PeDosMzHeader DosMzHeader { get; set; }
        public PeDosStub DosStub { get; set; }
        public PeHeader Header { get; set; }
        public List<PeSectionHead> SectionTable { get; set; }
        public List<PeSection> Sections { get; set; }
    }
}
