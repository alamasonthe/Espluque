using System.Runtime.InteropServices;
using System.Text;

namespace Inf
{
    internal static class InfCommon
    {
        internal const uint INF_STYLE_NONE = 0x00000000;
        internal const uint INF_STYLE_OLDNT = 0x00000001;
        internal const uint INF_STYLE_WIN4 = 0x00000002;
        internal const uint INF_STYLE_SUPPORTED = INF_STYLE_WIN4 | INF_STYLE_OLDNT;

        internal const string INF_STYLE_NAME_NONE = "INF_STYLE_NONE";
        internal const string INF_STYLE_NAME_OLDNT = "INF_STYLE_OLDNT";
        internal const string INF_STYLE_NAME_WIN4 = "INF_STYLE_WIN4";

        internal const uint INFINFO_INF_SPEC_IS_HINF = 0x00000001;
        internal const uint INFINFO_INF_NAME_IS_ABSOLUTE = 0x00000002;

        internal static readonly IntPtr InvalidHandleValue = new(-1);

        [StructLayout(LayoutKind.Sequential)]
        internal struct InfContext
        {
            public IntPtr Inf;
            public IntPtr CurrentInf;
            public uint Section;
            public uint Line;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupOpenInfFileW")]
        internal static extern IntPtr SetupOpenInfFile(
            string fileName,
            string? infClass,
            uint infStyle,
            out uint errorLine);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern void SetupCloseInfFile(IntPtr infHandle);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupFindFirstLineW")]
        internal static extern bool SetupFindFirstLine(
            IntPtr infHandle,
            string section,
            string key,
            out InfContext context);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern uint SetupGetFieldCount(ref InfContext context);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupGetStringFieldW")]
        internal static extern bool SetupGetStringField(
            ref InfContext context,
            uint fieldIndex,
            StringBuilder? returnBuffer,
            uint returnBufferSize,
            out uint requiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupGetInfInformationW")]
        internal static extern bool SetupGetInfInformation(
            string infSpec,
            uint searchControl,
            byte[]? returnBuffer,
            uint returnBufferSize,
            out uint requiredSize);
    }
}