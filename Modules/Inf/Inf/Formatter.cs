using Espluque.ModuleCommons;

namespace Inf
{
    internal class Formatter : FormatterBase
    {
        private readonly List<string> _ignoredKeys =
        [];

        public override KeyValuePair<string, string>? Format(KeyValuePair<string, string> item)
        {
            if (_ignoredKeys.Contains(item.Key))
            {
                return null;
            }

            string value = item.Value;
            switch (item.Key)
            {
                case "Class":
                    value = FormatClass(value);
                    break;

            }

            KeyValuePair<string, string> newKeyValuePair = new(item.Key, value);
            return newKeyValuePair;
        }

        private string FormatClass(string infClass)
        {
            if (string.IsNullOrWhiteSpace(infClass))
            {
                return infClass;
            }

            return infClass switch
            {
                "1394" => "1394 (IEEE 1394 Host Bus Controllers)",
                "1394Debug" => "1394Debug (Host-side IEEE 1394 Kernel Debugger Support)",
                "61883" => "61883 (IEEE 1394 IEC-61883 Devices)",
                "Adapter" => "Adapter (Adapter)",
                "APMSupport" => "APMSupport (APM)",
                "AudioEndpoint" => "AudioEndpoint (Audio Endpoint)",
                "AudioProcessingObject" => "AudioProcessingObject (Audio Processing Objects)",
                "AVC" => "AVC (IEEE 1394 AVC Devices)",
                "Battery" => "Battery (Battery Devices)",
                "Biometric" => "Biometric (Biometric Devices)",
                "Bluetooth" => "Bluetooth (Bluetooth Devices)",
                "Camera" => "Camera (Camera Devices)",
                "CDROM" => "CDROM (CD-ROM Drives)",
                "Computer" => "Computer (Computer)",
                "Decoder" => "Decoder (Decoders)",
                "DiskDrive" => "DiskDrive (Disk Drives)",
                "Display" => "Display (Display Adapters)",
                "Dot4" => "Dot4 (IEEE 1284.4 Devices)",
                "Dot4Print" => "Dot4Print (IEEE 1284.4 Print Functions)",
                "Enum1394" => "Enum1394 (IEEE 1394 IP Network Enumerator)",
                "Extension" => "Extension (Extension INF)",
                "FDC" => "FDC (Floppy Disk Controllers)",
                "FloppyDisk" => "FloppyDisk (Floppy Disk Drives)",
                "HDC" => "HDC (Hard Disk Controllers)",
                "HIDClass" => "HIDClass (Human Interface Devices)",
                "Image" => "Image (Imaging Devices)",
                "Infrared" => "Infrared (IrDA Devices)",
                "Keyboard" => "Keyboard (Keyboards)",
                "LegacyDriver" => "LegacyDriver (Non-Plug and Play Drivers)",
                "MediumChanger" => "MediumChanger (Media Changers)",
                "Modem" => "Modem (Modems)",
                "Monitor" => "Monitor (Monitors)",
                "Mouse" => "Mouse (Mice and Pointing Devices)",
                "MTD" => "MTD (Memory Technology Devices)",
                "Multifunction" => "Multifunction (Multifunction Devices)",
                "Media" => "Media (Multimedia Devices)",
                "MultiportSerial" => "MultiportSerial (Multiport Serial Adapters)",
                "Net" => "Net (Network Adapters)",
                "NetClient" => "NetClient (Network Clients)",
                "NetService" => "NetService (Network Services)",
                "NetTrans" => "NetTrans (Network Transports)",
                "NoDriver" => "NoDriver (No Driver)",
                "NvmeDisk" => "NvmeDisk (Storage Disks)",
                "PCMCIA" => "PCMCIA (PCMCIA Adapters)",
                "PNPPrinters" => "PNPPrinters (Bus-specific Printers)",
                "Ports" => "Ports (COM and LPT Ports)",
                "Printer" => "Printer (Printers)",
                "PrinterUpgrade" => "PrinterUpgrade (Printer Upgrade)",
                "PrintQueue" => "PrintQueue (Print Queue)",
                "Processor" => "Processor (Processors)",
                "SBP2" => "SBP2 (IEEE 1394 SBP2 Devices)",
                "SCSIAdapter" => "SCSIAdapter (SCSI, RAID, and NVMe Controllers)",
                "SecurityAccelerator" => "SecurityAccelerator (PCI SSL Accelerators)",
                "Securitydevices" => "Securitydevices (Security Devices)",
                "Sensor" => "Sensor (Sensors)",
                "SmartCardReader" => "SmartCardReader (Smart Card Readers)",
                "SoftwareComponent" => "SoftwareComponent (Software Components)",
                "SoftwareDevice" => "SoftwareDevice (Software Device)",
                "Sound" => "Sound (Sound)",
                "System" => "System (System Devices)",
                "TapeDrive" => "TapeDrive (Tape Drives)",
                "UCM" => "UCM (USB Connector Managers)",
                "USB" => "USB (USB Host Controllers and Hubs)",
                "USBDevice" => "USBDevice (USB Devices)",
                "Unknown" => "Unknown (Other Devices)",
                "Volume" => "Volume (Storage Volumes)",
                "VolumeSnapshot" => "VolumeSnapshot (Storage Volume Snapshots)",
                "WCEUSBS" => "WCEUSBS (Windows CE USB ActiveSync Devices)",
                "WPD" => "WPD (Windows Portable Devices)",
                _ => infClass
            };
        }
    }
}
