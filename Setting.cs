using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;

namespace AutoBulldozer
{
    [FileLocation("ModsSettings/AutoBulldozer/AutoBulldozer")]
    [SettingsUIGroupOrder(kMainGroup, kTimingGroup, kStatsGroup)]
    [SettingsUIShowGroupName(kMainGroup, kTimingGroup, kStatsGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kMainGroup = "Options";
        public const string kTimingGroup = "Timing";
        public const string kStatsGroup = "Statistics";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kMainGroup)]
        public bool EnableMod { get; set; }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsModDisabled))]
        [SettingsUISection(kSection, kMainGroup)]
        public bool DemolishAbandoned { get; set; }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAbandonedGraceDisabled))]
        [SettingsUISection(kSection, kMainGroup)]
        public bool RenovateAbandoned { get; set; }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsModDisabled))]
        [SettingsUISection(kSection, kMainGroup)]
        public bool DemolishCondemned { get; set; }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsModDisabled))]
        [SettingsUISection(kSection, kMainGroup)]
        public bool DemolishDestroyed { get; set; }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAbandonedGraceDisabled))]
        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kTimingGroup)]
        public int AbandonedGraceDays { get; set; }

        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsModDisabled))]
        [SettingsUISlider(min = 1, max = AutoBulldozerSystem.kMaxSweepsPerDay, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kTimingGroup)]
        public int SweepsPerDay { get; set; }

        [SettingsUISection(kSection, kStatsGroup)]
        public string TotalDemolished => AutoBulldozerSystem.TotalDemolished.ToString();

        [SettingsUISection(kSection, kStatsGroup)]
        public string TotalAbandoned => AutoBulldozerSystem.TotalAbandoned.ToString();

        [SettingsUISection(kSection, kStatsGroup)]
        public string TotalCondemned => AutoBulldozerSystem.TotalCondemned.ToString();

        [SettingsUISection(kSection, kStatsGroup)]
        public string TotalDestroyed => AutoBulldozerSystem.TotalDestroyed.ToString();

        [SettingsUISection(kSection, kStatsGroup)]
        public string TotalRenovated => AutoBulldozerSystem.TotalRenovated.ToString();

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kStatsGroup)]
        public bool ResetStatistics
        {
            set { AutoBulldozerSystem.ResetStatistics(); }
        }

        public bool IsModDisabled() => !EnableMod;

        public bool IsAbandonedGraceDisabled() => !EnableMod || !DemolishAbandoned;

        public sealed override void SetDefaults()
        {
            EnableMod = true;
            DemolishAbandoned = true;
            RenovateAbandoned = false;
            DemolishCondemned = false;
            DemolishDestroyed = false;
            AbandonedGraceDays = 0;
            SweepsPerDay = 16;
        }
    }
}
