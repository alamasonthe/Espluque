namespace PE.Entities
{
    internal class PeOptionalHeader
    {
        public PeField Magic { get; set; }
        public PeField MajorLinkerVersion { get; set; }
        public PeField MinorLinkerVersion { get; set; }
        public PeField SizeOfCode { get; set; }
        public PeField SizeOfInitializedData { get; set; }
        public PeField SizeOfUninitializedData { get; set; }
        public PeField AddressOfEntryPoint { get; set; }
        public PeField BaseOfCode { get; set; }
        public PeField BaseOfData { get; set; }
        public PeField ImageBase { get; set; }
        public PeField SectionAlignment { get; set; }
        public PeField FileAlignment { get; set; }
        public PeField MajorOperatingSystemVersion { get; set; }
        public PeField MinorOperatingSystemVersion { get; set; }
        public PeField MajorImageVersion { get; set; }
        public PeField MinorImageVersion { get; set; }
        public PeField MajorSubsystemVersion { get; set; }
        public PeField MinorSubsystemVersion { get; set; }
        public PeField Win32VersionValue { get; set; }
        public PeField SizeOfImage { get; set; }
        public PeField SizeOfHeaders { get; set; }
        public PeField CheckSum { get; set; }
        public PeField Subsystem { get; set; }
        public PeField DllCharacteristics { get; set; }
        public PeField SizeOfStackReserve { get; set; }
        public PeField SizeOfStackCommit { get; set; }
        public PeField SizeOfHeapReserve { get; set; }
        public PeField SizeOfHeapCommit { get; set; }
        public PeField LoaderFlags { get; set; }
        public PeField NumberOfRvaAndSizes { get; set; }
        public List<PeDataDirectory> DataDirectories { get; set; }
    }
}