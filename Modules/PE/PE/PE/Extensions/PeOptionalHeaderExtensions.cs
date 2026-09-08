using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeOptionalHeaderExtensions
    {
        public static List<KeyValuePair<string, string>> ToGrabberList(this PeOptionalHeader header)
        {
            List<KeyValuePair<string, string>> result = [];

            void AddField(string name, PeField? field)
            {
                if (field is not null)
                    result.Add(new KeyValuePair<string, string>(name, field.ToDisplayString()));
            }

            void AddDirectory(string name, ImageDataDirectory? directory)
            {
                if (directory?.VirtualAddress is null || directory.Size is null)
                    return;

                result.Add(new KeyValuePair<string, string>(
                    name,
                    $"VirtualAddress={directory.VirtualAddress.ToDisplayString()}, Size={directory.Size.ToDisplayString()}"));
            }

            AddField(nameof(header.Magic), header.Magic);
            AddField(nameof(header.MajorLinkerVersion), header.MajorLinkerVersion);
            AddField(nameof(header.MinorLinkerVersion), header.MinorLinkerVersion);
            AddField(nameof(header.SizeOfCode), header.SizeOfCode);
            AddField(nameof(header.SizeOfInitializedData), header.SizeOfInitializedData);
            AddField(nameof(header.SizeOfUninitializedData), header.SizeOfUninitializedData);
            AddField(nameof(header.AddressOfEntryPoint), header.AddressOfEntryPoint);
            AddField(nameof(header.BaseOfCode), header.BaseOfCode);
            AddField(nameof(header.BaseOfData), header.BaseOfData);
            AddField(nameof(header.ImageBase), header.ImageBase);
            AddField(nameof(header.SectionAlignment), header.SectionAlignment);
            AddField(nameof(header.FileAlignment), header.FileAlignment);
            AddField(nameof(header.MajorOperatingSystemVersion), header.MajorOperatingSystemVersion);
            AddField(nameof(header.MinorOperatingSystemVersion), header.MinorOperatingSystemVersion);
            AddField(nameof(header.MajorImageVersion), header.MajorImageVersion);
            AddField(nameof(header.MinorImageVersion), header.MinorImageVersion);
            AddField(nameof(header.MajorSubsystemVersion), header.MajorSubsystemVersion);
            AddField(nameof(header.MinorSubsystemVersion), header.MinorSubsystemVersion);
            AddField(nameof(header.Win32VersionValue), header.Win32VersionValue);
            AddField(nameof(header.SizeOfImage), header.SizeOfImage);
            AddField(nameof(header.SizeOfHeaders), header.SizeOfHeaders);
            AddField(nameof(header.CheckSum), header.CheckSum);
            AddField(nameof(header.Subsystem), header.Subsystem);
            AddField(nameof(header.DllCharacteristics), header.DllCharacteristics);
            AddField(nameof(header.SizeOfStackReserve), header.SizeOfStackReserve);
            AddField(nameof(header.SizeOfStackCommit), header.SizeOfStackCommit);
            AddField(nameof(header.SizeOfHeapReserve), header.SizeOfHeapReserve);
            AddField(nameof(header.SizeOfHeapCommit), header.SizeOfHeapCommit);
            AddField(nameof(header.LoaderFlags), header.LoaderFlags);
            AddField(nameof(header.NumberOfRvaAndSizes), header.NumberOfRvaAndSizes);

            AddDirectory(nameof(header.ExportTable), header.ExportTable);
            AddDirectory(nameof(header.ImportTable), header.ImportTable);
            AddDirectory(nameof(header.ResourceTable), header.ResourceTable);
            AddDirectory(nameof(header.ExceptionTable), header.ExceptionTable);
            AddDirectory(nameof(header.CertificateTable), header.CertificateTable);
            AddDirectory(nameof(header.BaseRelocationTable), header.BaseRelocationTable);
            AddDirectory(nameof(header.Debug), header.Debug);
            AddDirectory(nameof(header.Architecture), header.Architecture);
            AddDirectory(nameof(header.GlobalPtr), header.GlobalPtr);
            AddDirectory(nameof(header.TLSTable), header.TLSTable);
            AddDirectory(nameof(header.LoadConfigTable), header.LoadConfigTable);
            AddDirectory(nameof(header.BoundImport), header.BoundImport);
            AddDirectory(nameof(header.IAT), header.IAT);
            AddDirectory(nameof(header.DelayImportDescriptor), header.DelayImportDescriptor);
            AddDirectory(nameof(header.CLRRuntimeHeader), header.CLRRuntimeHeader);
            AddDirectory(nameof(header.Reserved), header.Reserved);

            return result;
        }

        public static JsonObject ToJson(this PeOptionalHeader header)
        {
            JsonObject dataDirectories = [];

            foreach (KeyValuePair<string, ImageDataDirectory> item in header._dataDirectories)
                dataDirectories[item.Key] = item.Value.ToJson();

            return new JsonObject
            {
                ["IsLoaded"] = header._isLoaded,
                ["StructureStartOffset"] = header.StructureStartOffset,
                ["Fields"] = JsonSerializer.SerializeToNode(header._fields),
                ["DataDirectories"] = dataDirectories
            };
        }
    }
}