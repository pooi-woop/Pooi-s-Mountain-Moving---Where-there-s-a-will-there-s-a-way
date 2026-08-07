using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace PMM.MountainMoving
{
    /// <summary>
    /// 扫描所有被指派"移除厚岩顶"的格子，给从事建造工作的殖民者派活。
    /// 结构与原版 WorkGiver_Miner / WorkGiver_RemoveRoof 一致。
    /// </summary>
    public class WorkGiver_RemoveThickRoof : WorkGiver_Scanner
    {
        // 站到目标格旁边即可施工（厚岩顶下方不一定能直接站人）
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            List<Designation> desList = pawn.Map.designationManager.AllDesignations;
            for (int i = 0; i < desList.Count; i++)
            {
                if (desList[i].def == PMM_DefOf.PMM_RemoveThickRoof)
                    yield return desList[i].target.Cell;
            }
        }

        public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            if (pawn.Map.designationManager.DesignationAt(c, PMM_DefOf.PMM_RemoveThickRoof) == null)
                return false;

            RoofDef roof = pawn.Map.roofGrid.RoofAt(c);
            if (roof == null || !roof.isThickRoof)
                return false; // 已被移除（可能被其他人/其他方式干掉）

            if (!pawn.CanReserve(c, 1, -1, null, forced))
                return false;

            // 必须能走到旁边（山体深处够不着的格子不会派活）
            if (!pawn.CanReach(c, PathEndMode.Touch, Danger.Deadly))
                return false;

            return true;
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
        {
            return JobMaker.MakeJob(PMM_DefOf.PMM_RemoveThickRoofJob, c);
        }
    }
}
