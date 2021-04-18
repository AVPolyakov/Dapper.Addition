using System.Threading;

namespace PlainQuery
{
    public static class MappingCheckSettings
    {
        private static readonly AsyncLocal<bool> _mappingCheckEnabled = new();

        public static bool MappingCheckEnabled
        {
            get => _mappingCheckEnabled.Value;
            set => _mappingCheckEnabled.Value = value;
        }
    }
}