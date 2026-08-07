using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PMM.MountainMoving
{
    /// <summary>
    /// 按格子保存"移除厚岩顶"的进度（键 = 格子索引）。
    ///
    /// 为什么需要它：本 mod 的工作量极高（默认每格约 2 个游戏日），殖民者
    /// 中途一定会去吃饭/睡觉。进度存到这里，回来后接着干，不会前功尽弃。
    ///
    /// 该组件采用"惰性注入"（第一次使用时才加入 map.components），
    /// 因此无需 Harmony 补丁即可正常工作；存档/读档时随地图一起保存。
    /// </summary>
    public class MapComponent_RoofRemoval : MapComponent
    {
        private Dictionary<int, float> progress = new Dictionary<int, float>();

        // 复用的临时缓冲，避免每 500 tick 分配
        private readonly List<int> staleCells = new List<int>();
        private readonly List<Designation> staleDesignations = new List<Designation>();

        public MapComponent_RoofRemoval(Map map) : base(map)
        {
        }

        /// <summary>获取（或惰性创建）该地图上的本组件。</summary>
        public static MapComponent_RoofRemoval For(Map map)
        {
            List<MapComponent> comps = map.components;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is MapComponent_RoofRemoval found)
                    return found;
            }
            MapComponent_RoofRemoval comp = new MapComponent_RoofRemoval(map);
            map.components.Add(comp);
            return comp;
        }

        public float GetProgress(IntVec3 cell)
        {
            float value;
            return progress.TryGetValue(map.cellIndices.CellToIndex(cell), out value) ? value : 0f;
        }

        public void SetProgress(IntVec3 cell, float value)
        {
            progress[map.cellIndices.CellToIndex(cell)] = value;
        }

        public void ClearProgress(IntVec3 cell)
        {
            progress.Remove(map.cellIndices.CellToIndex(cell));
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // 每 500 tick 做一次清理，开销可忽略
            if (Find.TickManager.TicksGame % 500 != 0)
                return;

            // 1) 清理失效的进度记录（指派被取消，或屋顶已不存在）
            if (progress.Count > 0)
            {
                staleCells.Clear();
                foreach (KeyValuePair<int, float> kv in progress)
                {
                    IntVec3 cell = map.cellIndices.IndexToCell(kv.Key);
                    RoofDef roof = map.roofGrid.RoofAt(cell);
                    if (roof == null || !roof.isThickRoof ||
                        map.designationManager.DesignationAt(cell, PMM_DefOf.PMM_RemoveThickRoof) == null)
                    {
                        staleCells.Add(kv.Key);
                    }
                }
                for (int i = 0; i < staleCells.Count; i++)
                    progress.Remove(staleCells[i]);
            }

            // 2) 清理"屋顶已消失但指派标记还留着"的残留标记
            staleDesignations.Clear();
            List<Designation> allDes = map.designationManager.AllDesignations;
            for (int i = 0; i < allDes.Count; i++)
            {
                if (allDes[i].def == PMM_DefOf.PMM_RemoveThickRoof)
                {
                    RoofDef roof = map.roofGrid.RoofAt(allDes[i].target.Cell);
                    if (roof == null || !roof.isThickRoof)
                        staleDesignations.Add(allDes[i]);
                }
            }
            for (int i = 0; i < staleDesignations.Count; i++)
                map.designationManager.RemoveDesignation(staleDesignations[i]);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref progress, "PMM_roofRemovalProgress", LookMode.Value, LookMode.Value);
            if (progress == null)
                progress = new Dictionary<int, float>();
        }
    }
}
