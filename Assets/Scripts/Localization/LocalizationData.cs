using System;

namespace Wuxing.Localization
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string zhHans;
        public string en;
    }

    [Serializable]
    public class LocalizationTable
    {
        public LocalizationEntry[] entries;
    }
}

