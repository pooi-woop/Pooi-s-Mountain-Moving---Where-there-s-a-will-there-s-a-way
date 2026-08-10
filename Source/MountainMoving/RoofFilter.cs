using RimWorld;
using Verse;

namespace PMM.MountainMoving
{
    /// <summary>
    /// 统一判断某种屋顶是否允许被"移除岩顶"指令拆除（Bug 2 修复）。
    ///
    /// 原先 4 处都硬编码 roof.isThickRoof，导致薄岩顶等其它岩顶一律拆不掉。
    /// 现在按 Mod 设置里的三个开关分类判断：
    ///   厚岩顶(overhead mountain)  isThickRoof=true              → MountainMovingMod.RemoveThickRoof
    ///   薄岩顶                     isNatural=true 且非厚岩顶       → MountainMovingMod.RemoveThinRoof
    ///   建造屋顶                   isNatural=false                → MountainMovingMod.RemoveConstructedRoof
    /// 每次调用都实时读取设置，改动设置后立即生效。
    /// </summary>
    public static class RoofFilter
    {
        public static bool IsRemovableRoof(RoofDef roof)
        {
            if (roof == null)
                return false;
            if (roof.isThickRoof)
                return MountainMovingMod.RemoveThickRoof;
            if (roof.isNatural)
                return MountainMovingMod.RemoveThinRoof;
            return MountainMovingMod.RemoveConstructedRoof;
        }
    }
}
