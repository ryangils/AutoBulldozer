using System.Collections.Generic;
using Colossal;

namespace AutoBulldozer
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Auto Bulldozer" },

                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kMainGroup), "Options" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kTimingGroup), "Timing" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kStatsGroup), "Statistics" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableMod)), "Enable Auto Bulldozer" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableMod)), "Master switch. When off, nothing is demolished automatically." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DemolishAbandoned)), "Demolish abandoned buildings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DemolishAbandoned)), "Automatically demolish buildings that have been abandoned by their occupants." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DemolishCondemned)), "Demolish condemned buildings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DemolishCondemned)), "Automatically demolish buildings that have been condemned (e.g. below required level)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DemolishDestroyed)), "Clear destroyed growable buildings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DemolishDestroyed)), "Automatically clear rubble of zoned (residential/commercial/industrial/office) buildings destroyed by fire or disasters, so the game regrows them. Service buildings, signature buildings and anything you placed by hand are left alone, so you never lose a service and can rebuild them yourself." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AbandonedGraceDays)), "Grace period for abandoned buildings (days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AbandonedGraceDays)), "How many in-game days a building must stay abandoned before it is demolished. 0 demolishes on the next sweep. Gives buildings a chance to be re-occupied and gives you a chance to notice problem areas." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SweepsPerDay)), "Sweeps per in-game day" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SweepsPerDay)), "How often the mod scans the city for buildings to demolish. Higher values clear buildings sooner after they qualify; lower values batch the work up." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalDemolished)), "Buildings demolished this session" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalDemolished)), "Number of buildings this mod has demolished since the game was started." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalAbandoned)), "— abandoned" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalAbandoned)), "Abandoned buildings demolished this session." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCondemned)), "— condemned" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCondemned)), "Condemned buildings demolished this session." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalDestroyed)), "— destroyed" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalDestroyed)), "Destroyed buildings cleared this session." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetStatistics)), "Reset statistics" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetStatistics)), "Set all demolition counters back to zero." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetStatistics)), "Reset all demolition counters to zero?" },
            };
        }

        public void Unload()
        {
        }
    }
}
