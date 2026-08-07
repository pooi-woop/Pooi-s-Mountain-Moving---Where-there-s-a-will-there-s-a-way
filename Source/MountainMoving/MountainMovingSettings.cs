using UnityEngine;
using Verse;

namespace PMM.MountainMoving
{
    /// <summary>
    /// Mod 设置：每格厚岩顶所需工作量、完成时是否提示。
    /// </summary>
    public class MountainMovingSettings : ModSettings
    {
        // 默认每格 120000 tick ≈ 2 个游戏日（1 游戏日 = 60000 tick），建造速度 1.0 时。
        public const float DefaultWorkAmount = 120000f;

        public float workAmount = DefaultWorkAmount;
        public bool notifyOnComplete = false;

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);

            l.Label("PMM_Setting_WorkAmount".Translate(
                workAmount.ToString("F0"),
                (workAmount / 60000f).ToString("F1")));
            workAmount = l.Slider(workAmount, 6000f, 600000f);
            // 吸附到 0.1 天（6000 tick）一档，读起来整齐
            workAmount = (float)System.Math.Round(workAmount / 6000f) * 6000f;

            l.Gap();
            l.CheckboxLabeled("PMM_Setting_Notify".Translate(), ref notifyOnComplete);

            l.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workAmount, "workAmount", DefaultWorkAmount);
            Scribe_Values.Look(ref notifyOnComplete, "notifyOnComplete", false);
        }
    }
}
