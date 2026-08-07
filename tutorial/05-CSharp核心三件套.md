# 第 5 章：C# 核心三件套（指令 → 派活 → 干活）

> 本章目标：彻底理解"玩家框选 → 小人自动来凿 → 一格凿完"这条链路，这是本教程的核心。
> 打开对照：`Designator_RemoveThickRoof.cs`、`WorkGiver_RemoveThickRoof.cs`、`JobDriver_RemoveThickRoof.cs`

## 5.0 先看全景：三个类如何接力

```
玩家框选一片厚岩顶
   │
   ▼  ① Designator_RemoveThickRoof     —— 负责"哪些格能选、选中了打标记"
在选中的每个格子创建一个 Designation（指派标记）
   │
   ▼  ② WorkGiver_RemoveThickRoof      —— 负责"发现标记 → 给空闲小人派活"
对每格标记，给勾选了"建造"的小人生成一个 Job（工作）
   │
   ▼  ③ JobDriver_RemoveThickRoof      —— 负责"这件活具体怎么一步步做"
小人走过去 → 一下一下凿 → 凿满就移除岩顶
```

这个"**指派(Designation) → 派活(WorkGiver) → 干活(JobDriver)**"三段式，是 RimWorld 几乎所有"玩家在地图上布置任务"的通用套路（挖矿、割除、拆除……都是这个结构）。学会它，你就掌握了一大类 mod 的写法。

---

## 5.1 Designator：框选工具

职责：在"命令"面板里提供一个可拖拽的按钮；决定**哪些格子能被选**；选中后**打上指派标记**。

继承 `Designator_Cells`（"按格子框选"的基类，自带拖拽框选逻辑），我们只要填几个空：

```csharp
public class Designator_RemoveThickRoof : Designator_Cells
{
    public Designator_RemoveThickRoof()
    {
        defaultLabel  = "PMM_Designator_RemoveThickRoof_Label".Translate();  // 按钮名字（走翻译）
        defaultDesc   = "PMM_Designator_RemoveThickRoof_Desc".Translate();   // 鼠标悬停说明
        icon          = ContentFinder<Texture2D>.Get("PMM/UI/RemoveThickRoof", true); // 图标
        useMouseIcon  = true;
        soundDragSustain = SoundDefOf.Designate_DragStandard;   // 拖拽时的音效
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        soundSucceeded   = SoundDefOf.Designate_Mine;           // 松手确认时的音效
    }

    public override int DraggableDimensions => 2;   // 2 = 拖出矩形；1 = 直线；0 = 单点
    public override bool DragDrawMeasurements => true; // 拖拽时显示"几乘几"
```

最关键的是下面两个方法——**"能不能选这格"** 和 **"选中了做什么"**：

```csharp
    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map)) return false;          // 越界/迷雾里不行
        RoofDef roof = Map.roofGrid.RoofAt(c);                        // 读这格的屋顶
        if (roof == null || !roof.isThickRoof) return false;          // 只认厚岩顶
        if (Map.designationManager.DesignationAt(c, PMM_DefOf.PMM_RemoveThickRoof) != null)
            return false;                                             // 已标记过的不重复
        return true;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        if (CanDesignateCell(c).Accepted)
            Map.designationManager.AddDesignation(new Designation(c, PMM_DefOf.PMM_RemoveThickRoof));
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        foreach (IntVec3 c in cells) DesignateSingleCell(c);          // 拖一片 = 逐格标记
    }
}
```

**新名词速查：**
- `IntVec3`：一个格子的整数坐标 (x, y, z)。
- `Map.roofGrid.RoofAt(c)`：查某格的屋顶类型；`roof.isThickRoof` 为真就是"厚岩顶/山体头顶"。
- `Map.designationManager`：管理整张地图所有"指派标记"。`AddDesignation` 打标记、`DesignationAt` 查标记。
- `AcceptanceReport`：返回值，`true`/`false` 或一句"为什么不行"的提示。

---

## 5.2 WorkGiver：给小人派活

职责：不断扫描地图，**发现哪些格子有我们的标记**，给合适的小人**生成一个 Job**。

继承 `WorkGiver_Scanner`（扫描型派活器基类），填三个方法：

```csharp
public class WorkGiver_RemoveThickRoof : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;  // 站到目标格"旁边"即可施工

    // ① 候选格：所有打了"移除厚岩顶"标记的格子
    public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
    {
        List<Designation> desList = pawn.Map.designationManager.AllDesignations;
        for (int i = 0; i < desList.Count; i++)
            if (desList[i].def == PMM_DefOf.PMM_RemoveThickRoof)
                yield return desList[i].target.Cell;
    }

    // ② 这格对某个小人来说"现在能干嘛"——一连串可行性检查
    public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
    {
        if (pawn.Map.designationManager.DesignationAt(c, PMM_DefOf.PMM_RemoveThickRoof) == null)
            return false;                                  // 没标记
        RoofDef roof = pawn.Map.roofGrid.RoofAt(c);
        if (roof == null || !roof.isThickRoof) return false; // 岩顶已经没了
        if (!pawn.CanReserve(c, 1, -1, null, forced)) return false; // 已被别人预定
        if (!pawn.CanReach(c, PathEndMode.Touch, Danger.Deadly)) return false; // 走不到旁边
        return true;
    }

    // ③ 通过检查 → 真正生成一个 Job，目标就是这一格
    public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
    {
        return JobMaker.MakeJob(PMM_DefOf.PMM_RemoveThickRoofJob, c);
    }
}
```

**为什么 `CanReserve` / `CanReach` 很重要：**
- `CanReserve` 防止两个小人都去凿同一格（先预定先得）。
- `CanReach` 保证小人**能走到目标格旁边**。山体深处四壁都是岩石、够不着的格子，不会派活——你得先挖矿接近，这很合理。

> 它挂到哪个工作类型（建造/挖矿/搬运…），由第 2 章 `WorkGiverDef` 里的 `<workType>` 决定，不是在这里写。

---

## 5.3 JobDriver：这件活具体怎么做（核心中的核心）

职责：描述小人**干活的每一步**。RimWorld 用"**Toil（工序）**"来描述一件工作的步骤序列。

先建立直觉：一个 Job = 一串 Toil。比如"移除岩顶"这个 Job 就两个 Toil：

```
Toil 1: 走到目标格旁边
Toil 2: 站在那一下一下凿，直到凿满
```

看 `JobDriver_RemoveThickRoof.cs` 的骨架：

```csharp
public class JobDriver_RemoveThickRoof : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
        => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);  // 开工前正式预定这格

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // “什么时候算失败”：岩顶没了，或玩家取消了标记
        AddFailCondition(() => {
            IntVec3 cell = TargetLocA;
            RoofDef roof = Map.roofGrid.RoofAt(cell);
            if (roof == null || !roof.isThickRoof) return true;
            return Map.designationManager.DesignationAt(cell, PMM_DefOf.PMM_RemoveThickRoof) == null;
        });

        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);  // Toil 1：走过去

        Toil work = new Toil();                                  // Toil 2：凿！
        work.defaultCompleteMode = ToilCompleteMode.Never;       // 不自动结束，由我们手动结束
        work.WithProgressBar(TargetIndex.A, Progress01, false, -0.5f); // 头顶进度条
        work.tickAction = TickWork;                              // 每个游戏 tick 都调用 TickWork
        yield return work;
    }
}
```

**几个关键点：**
- `MakeNewToils()` 用 `yield return` 依次交出工序。
- `Toils_Goto.GotoCell(...)` 是现成的"走过去"工序，直接用。
- 第二个工序我们自己造：`defaultCompleteMode = Never` 表示"别自己结束，听我号令"，`tickAction` 是**每 tick 要执行的逻辑**（真正的"凿"）。

**凿的逻辑 `TickWork`（每 tick 跑一遍）：**

```csharp
    private void TickWork()
    {
        Pawn actor = pawn;
        IntVec3 cell = TargetLocA;
        MapComponent_RoofRemoval comp = MapComponent_RoofRemoval.For(actor.Map);

        float done = comp.GetProgress(cell);                 // ① 读这格已经凿了多少
        done += actor.GetStatValue(StatDefOf.ConstructionSpeed); // ② 这一 tick 按"建造速度"加进度
        comp.SetProgress(cell, done);                        // ③ 存回去（关键：进度被持久化！）

        if (actor.skills != null)
            actor.skills.Learn(SkillDefOf.Construction, 0.1f, false); // 边凿边练建造

        if (done < MountainMovingMod.WorkAmount) return;     // 没凿满就下个 tick 继续

        // ---- 凿满了：移除岩顶 ----
        RoofDef roof = actor.Map.roofGrid.RoofAt(cell);
        if (roof != null && roof.isThickRoof)
            actor.Map.roofGrid.SetRoof(cell, null);          // 把屋顶设为 null = 露出天空

        comp.ClearProgress(cell);                            // 清掉这格的进度记录

        Designation des = actor.Map.designationManager.DesignationAt(cell, PMM_DefOf.PMM_RemoveThickRoof);
        if (des != null) actor.Map.designationManager.RemoveDesignation(des); // 顺便清掉标记

        actor.jobs.EndCurrentJob(JobCondition.Succeeded, true); // 宣告这件活"成功完成"
    }
```

**整件事的灵魂在第 ①②③ 步**：进度不是存在"这次工作"里，而是存进一个**跟着地图存档走的 MapComponent**（第 6 章专门讲）。
这就是为什么小人中途去吃饭/睡觉、甚至你存档再读档，回来后都能**接着上次的进度继续凿**——对于"极高工作量"这种要凿好几天的活，没有这一步永远都凿不完。

**进度条**由这个方法提供 0~1 的数值：

```csharp
    private float Progress01()
        => MapComponent_RoofRemoval.For(Map).GetProgress(TargetLocA) / MountainMovingMod.WorkAmount;
```

---

## 5.4 常见困惑 Q&A

- **Q：`TargetLocA` / `job.targetA` 是哪来的？**
  在 WorkGiver 的 `JobMaker.MakeJob(def, c)` 里把格子 `c` 设成了目标 A，JobDriver 里用 `TargetLocA` 就能取回这个格子。
- **Q：为什么凿完要手动 `EndCurrentJob`？**
  因为我们把工序设成了 `Never` 自动结束，所以凿满时要自己宣布"成功了"。
- **Q：`tickAction` 多久跑一次？**
  每个游戏 tick（1 秒现实时间 ≈ 60 tick，正常速度）。所以"工作量"用 tick 衡量，`120000 tick ≈ 2 个游戏日`。

## 动手练

1. 在 `TickWork` 的"凿满了"分支里加一句 `Log.Message("凿掉了一格岩顶！");`（记得 `using Verse;`），重新编译，看控制台有没有输出。
2. 把 `done += ...` 那行的 `ConstructionSpeed` 改成固定值 `1f`，体会"工作量与技能脱钩"的差别（再改回来）。

## 本章小结

- 三段式：**Designator（打标记）→ WorkGiver（派活）→ JobDriver（干活）**。
- JobDriver 用一串 **Toil** 描述步骤；`tickAction` 是每 tick 的核心逻辑。
- 进度持久化是"高工作量"能成立的关键，引出下一章。

> 下一章 → [第 6 章：持久化与 Mod 设置](06-持久化与设置.md)
