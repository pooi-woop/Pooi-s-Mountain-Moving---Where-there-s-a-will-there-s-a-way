using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PMM.MountainMoving
{
    /// <summary>
    /// "移除厚岩顶"区域指令。出现在 建筑师 → 命令 面板。
    /// 只能指派存在厚岩顶(overhead mountain)的格子。
    /// </summary>
    public class Designator_RemoveThickRoof : Designator_Cells
    {
        public Designator_RemoveThickRoof()
        {
            defaultLabel = "PMM_Designator_RemoveThickRoof_Label".Translate();
            defaultDesc = "PMM_Designator_RemoveThickRoof_Desc".Translate();
            icon = ContentFinder<Texture2D>.Get("PMM/UI/RemoveThickRoof", true);
            useMouseIcon = true;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            soundSucceeded = SoundDefOf.Designate_Mine;
        }

        // 2 = 可拖拽出矩形区域
        public override int DraggableDimensions => 2;

        public override bool DragDrawMeasurements => true;

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map))
                return false;

            RoofDef roof = Map.roofGrid.RoofAt(c);
            if (roof == null || !roof.isThickRoof)
                return false; // 只针对厚岩顶

            // 已指派过的格子不重复指派
            if (Map.designationManager.DesignationAt(c, PMM_DefOf.PMM_RemoveThickRoof) != null)
                return false;

            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            if (CanDesignateCell(c).Accepted)
            {
                Map.designationManager.AddDesignation(new Designation(c, PMM_DefOf.PMM_RemoveThickRoof));
            }
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            foreach (IntVec3 c in cells)
            {
                DesignateSingleCell(c);
            }
        }
    }
}
