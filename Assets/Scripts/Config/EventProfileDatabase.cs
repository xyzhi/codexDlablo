using System;
using System.Collections.Generic;
using System.Linq;

namespace Wuxing.Config
{
    [Serializable]
    public class EventProfileDatabase
    {
        public List<EventProfileConfig> eventProfiles = new List<EventProfileConfig>();

        public IReadOnlyList<EventProfileConfig> EventProfiles => eventProfiles;

        public EventProfileConfig GetByProfile(string profile)
        {
            return eventProfiles.FirstOrDefault(config =>
                config != null
                && string.Equals(config.Profile, profile, StringComparison.OrdinalIgnoreCase));
        }
    }
}