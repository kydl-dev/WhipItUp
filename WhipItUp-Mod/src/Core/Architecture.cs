namespace WhipItUp.Core
{
    /// <summary>
    /// Which architecture this DLL is built with. The lab wrapper refuses a DLL without the tag (lessons\scripts\lab-guarded.ps1 -Marker),
    /// so a TestLab run can never test an old build by mistake. Logged once at startup.
    /// </summary>
    internal static class Architecture
    {
        /// KydlDev spec layout (MIGRATION_DECISIONS, 2026-11-03).
        internal const int Version = 1;
        /// The marker text searched for in the DLL (UTF-16): "WhipItUp-KYDL-SPEC-V1".
        internal static readonly string Tag = "WhipItUp-KYDL-SPEC-V1";
    }
}
