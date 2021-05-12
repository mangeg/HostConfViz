namespace HostConfViz
{
    internal class HostInfoOptions
    {
        public bool DisplayEnvironment { get; set; } = true;
        public bool DisplayConfig { get; set; } = true;
        public bool IgnoreGlobalEnvironment { get; set; }
        public bool RedactSecrets { get; set; } = true;
    }
}
