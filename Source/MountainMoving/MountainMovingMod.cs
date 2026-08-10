using UnityEngine;
using Verse;

namespace PMM.MountainMoving
{
    /// <summary>
    /// Mod 入口：加载设置，并把设置窗口接管给我们的 MountainMovingSettings。
    /// </summary>
    public class MountainMovingMod : Mod
    {
        public static MountainMovingSettings settings;

        public MountainMovingMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<MountainMovingSettings>();
        }

        // 防御性取值：即使设置尚未加载也有默认值兜底
        public static float WorkAmount =>
            settings != null ? settings.workAmount : MountainMovingSettings.DefaultWorkAmount;

        public static bool NotifyOnComplete =>
            settings != null && settings.notifyOnComplete;

        // 岩顶过滤开关（默认厚/薄岩顶开、建造屋顶关；设置未加载时按默认值兜底）。
        public static bool RemoveThickRoof =>
            settings == null || settings.removeThickRoof;

        public static bool RemoveThinRoof =>
            settings == null || settings.removeThinRoof;

        public static bool RemoveConstructedRoof =>
            settings != null && settings.removeConstructedRoof;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Pooi's Mountain Moving";
        }
    }
}
