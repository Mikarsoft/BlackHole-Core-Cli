

namespace BlackHoleCli
{
    public class ReferencedProjectInfo
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
    }

    public class BHCommandProperties
    {
        public string? ProjectPath { get; set; } = string.Empty;
        public string CliCommand { get; set; } = string.Empty;
        public string SettingMode { get; set; } = string.Empty;
        public string ExtraMode { get; set; } = string.Empty;
    }
}
