using RimWorld;
using Verse;

namespace PMM.MountainMoving
{
    /// <summary>
    /// 集中管理本 mod 的 Def 引用，避免手写字符串 defName。
    /// 字段名必须与 XML 中的 defName 完全一致。
    /// </summary>
    [DefOf]
    public static class PMM_DefOf
    {
        public static DesignationDef PMM_RemoveThickRoof;
        public static JobDef PMM_RemoveThickRoofJob;

        static PMM_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PMM_DefOf));
        }
    }
}
