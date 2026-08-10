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

        // Bug 3 修复：Designator 基类的 DraggableDimensions 默认返回 None（不框选），
        // Designator_Cells 并不会自动支持 2D 拖拽。必须像原版 Designator_Mine /
        // Designator_RemoveRoof 一样显式 override 成 Rectangle，才能矩形拖拽框选多格。
        public override DraggableDimensions DraggableDimensions => DraggableDimensions.Rectangle;

        public override bool DragDrawMeasurements => true;

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map))
                return false;

            RoofDef roof = Map.roofGrid.RoofAt(c);
            if (!RoofFilter.IsRemovableRoof(roof))
                return false; // 只针对设置里允许移除的岩顶

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
