using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace PMM.MountainMoving
{
    /// <summary>
    /// 移除厚岩顶的实际工作逻辑。
    ///
    /// 工作量极高（见 Mod 设置 workAmount，默认 120000 ≈ 2 个游戏日 @ 速度 1.0）。
    /// 每个 tick 按"建造速度"累积进度并写入 MapComponent，因此吃饭/睡觉/读档后
    /// 会接着上次进度继续。进度条实时显示。
    /// </summary>
    public class JobDriver_RemoveThickRoof : JobDriver
    {
        private const TargetIndex CellInd = TargetIndex.A;

        // 装饰性特效（凿岩尘土）。用运行时查找，取不到就静默跳过，绝不影响编译/运行。
        private EffecterDef workEffect;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            workEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("Mine");

            // 失败条件：屋顶已经没了，或玩家取消了指派。
            // （完成的那一瞬间我们先移除屋顶再立刻结束工作，不会误触发此条件）
            AddFailCondition(delegate
            {
                IntVec3 cell = TargetLocA;
                RoofDef roof = Map.roofGrid.RoofAt(cell);
                if (!RoofFilter.IsRemovableRoof(roof))
                    return true;
                return Map.designationManager.DesignationAt(cell, PMM_DefOf.PMM_RemoveThickRoof) == null;
            });

            // 1) 走到目标格旁边
            yield return Toils_Goto.GotoCell(CellInd, PathEndMode.Touch);

            // 2) 长时间凿岩
            Toil work = new Toil();
            work.defaultCompleteMode = ToilCompleteMode.Never; // 由我们手动结束
            work.WithProgressBar(CellInd, Progress01, false, -0.5f);
            if (workEffect != null)
                work.WithEffect(workEffect, CellInd);
            work.tickAction = TickWork;
            yield return work;
        }

        // 进度条读取（0~1）
        private float Progress01()
        {
            float required = MountainMovingMod.WorkAmount;
            if (required <= 0f)
                return 1f;
            return MapComponent_RoofRemoval.For(Map).GetProgress(TargetLocA) / required;
        }

        // 每个 tick 累积工作量
        private void TickWork()
        {
            Pawn actor = pawn;
            IntVec3 cell = TargetLocA;
            Map map = actor.Map;
            MapComponent_RoofRemoval comp = MapComponent_RoofRemoval.For(map);

            float done = comp.GetProgress(cell);
            done += actor.GetStatValue(StatDefOf.ConstructionSpeed);
            comp.SetProgress(cell, done);

            // 边凿边练建造技能
            if (actor.skills != null)
                actor.skills.Learn(SkillDefOf.Construction, 0.1f, false);

            if (done < MountainMovingMod.WorkAmount)
                return;

            // ---- 完成：移除岩顶 ----
            RoofDef roof = map.roofGrid.RoofAt(cell);
            if (RoofFilter.IsRemovableRoof(roof))
                map.roofGrid.SetRoof(cell, null); // SetRoof 会负责光照/区域/屋顶贴图刷新

            comp.ClearProgress(cell);

            Designation des = map.designationManager.DesignationAt(cell, PMM_DefOf.PMM_RemoveThickRoof);
            if (des != null)
                map.designationManager.RemoveDesignation(des);

            if (MountainMovingMod.NotifyOnComplete)
            {
                Messages.Message(
                    "PMM_Message_RoofRemoved".Translate(),
                    new TargetInfo(cell, map, false),
                    MessageTypeDefOf.PositiveEvent);
            }

            actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
        }
    }
}
