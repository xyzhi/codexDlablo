using System;
using System.Collections.Generic;
using System.Linq;
namespace Wuxing.Config
{
    [Serializable]
    public class EventOptionDatabase
    {
        public List<EventOptionConfig> eventOptions = new List<EventOptionConfig>();
        public IReadOnlyList<EventOptionConfig> EventOptions => eventOptions;
        public List<EventOptionConfig> GetByProfile(string profile)
        {
            return eventOptions
                .Where(config => config != null && string.Equals(config.Profile, profile, StringComparison.OrdinalIgnoreCase))
                .OrderBy(config => config.OptionIndex)
                .ToList();
        }
    }
}