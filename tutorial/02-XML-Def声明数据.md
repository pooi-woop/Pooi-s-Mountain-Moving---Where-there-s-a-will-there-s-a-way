# 第 2 章：XML Def 声明数据

> 本章目标：理解 Def 是什么，并彻底看懂本项目的 3 个 Def 文件。
> 打开对照：`Defs/DesignationDefs/PMM_Designations.xml`、`Defs/JobDefs/PMM_Jobs.xml`、`Defs/WorkGiverDefs/PMM_WorkGivers.xml`

## 2.1 Def 是什么？

**Def（Definition）= 用 XML 写给游戏看的"数据表"。** 游戏里一切"内容"都是 Def：武器、建筑、植物、工作、指令标记……

一个 Def 至少有三样东西：

```xml
<JobDef>                                  <!-- ① 用哪种 Def（决定这是"工作"） -->
  <defName>PMM_RemoveThickRoofJob</defName>  <!-- ② 唯一 ID（别的代码靠它找到这个 Def） -->
  <label>...</label>                       <!-- ③ 一堆字段（具体属性） -->
</JobDef>
```

- **根节点永远是 `<Defs>`**，里面可以放很多个 Def。
- `<defName>` 是**内部 ID**，玩家看不到，但 C# 和其它 Def 都靠它引用。**必须全局唯一**，所以一律加你的专属前缀（本项目用 `PMM_`）。
- `<label>` 是**显示名**，玩家看得到。

## 2.2 defName 命名规范（重要，养成习惯）

```
<前缀>_<名称>     例如：PMM_RemoveThickRoof
```

- 前缀 2~4 个字符，是你 mod 的"商标"，防止和别的 mod 撞名。
- **C# 里的 DefOf 字段名要和 defName 完全一致**（第 4 章会讲为什么）。

## 2.3 本项目的三个 Def，逐个看

### ① DesignationDef —— "指派标记"（框选后打在格子上的那个记号）

文件：`Defs/DesignationDefs/PMM_Designations.xml`

```xml
<DesignationDef>
  <defName>PMM_RemoveThickRoof</defName>
  <texturePath>PMM/Designations/RemoveThickRoof</texturePath>
  <targetType>Cell</targetType>
</DesignationDef>
```

| 字段 | 含义 |
|---|---|
| `texturePath` | 指派后画在地图格子上的图案。指向 `Textures/PMM/Designations/RemoveThickRoof.png`（**不含 `Textures/` 前缀、不含扩展名**） |
| `targetType` | 指派目标是 `Cell`（格子）还是 `Thing`（物品/生物）。移除岩顶当然是按格子 → `Cell` |

> "designation（指派）"就是玩家在地图上做的标记，比如"挖矿""拆除"。它只是**一个标记**，本身不干活；真正派活的是下面的 WorkGiver。

### ② JobDef —— "一件要做的事"

文件：`Defs/JobDefs/PMM_Jobs.xml`

```xml
<JobDef>
  <defName>PMM_RemoveThickRoofJob</defName>
  <driverClass>PMM.MountainMoving.JobDriver_RemoveThickRoof</driverClass>
  <reportString>removing thick roof.</reportString>
</JobDef>
```

| 字段 | 含义 |
|---|---|
| `driverClass` | **关键**：这件事"怎么做"，由哪个 C# 类实现。这里指向我们第 5 章要写的 `JobDriver_RemoveThickRoof`。**要写完整命名空间**。 |
| `reportString` | 小人状态栏显示的"正在做什么"。中文翻译在 `Languages/` 里（第 7 章）。 |

> 注意：这里就是第 1 章流水线"阶段 C 引用阶段 B 的类"的典型例子——`driverClass` 写的是 C# 类名，所以它必须先被编译进 DLL。

### ③ WorkGiverDef —— "把活派给谁"

文件：`Defs/WorkGiverDefs/PMM_WorkGivers.xml`

```xml
<WorkGiverDef>
  <defName>PMM_RemoveThickRoof</defName>
  <label>remove thick roofs</label>
  <giverClass>PMM.MountainMoving.WorkGiver_RemoveThickRoof</giverClass>
  <workType>Construction</workType>
  <priorityInType>110</priorityInType>
  <verb>remove thick roof</verb>
  <gerund>removing thick roof</gerund>
  <requiredCapacities>
    <li>Manipulation</li>
  </requiredCapacities>
</WorkGiverDef>
```

| 字段 | 含义 |
|---|---|
| `giverClass` | "怎么找到活、怎么派活"的 C# 类（第 5 章）。 |
| `workType` | 挂在哪个**工作类型**上。这里挂到 `Construction`（建造）——于是"工作"面板里勾选了建造的小人就会来凿岩顶。 |
| `priorityInType` | 在该工作类型**内部**的优先级，数字越大越先干。 |
| `verb` / `gerund` | 派活提示用语（"去 移除厚岩顶" / "正在 移除厚岩顶"）。 |
| `requiredCapacities` | 干这活需要的身体能力。`Manipulation`（操作能力，手）——手残的小人干不了。 |

## 2.4 ParentName 继承（先了解，本项目没用到）

很多 Def 可以用 `ParentName="某某Base"` 继承一串默认值，避免重复。比如造武器时常继承 `BaseMeleeWeapon_Sharp_Quality`：

```xml
<ThingDef ParentName="BaseMeleeWeapon_Sharp_Quality">
  <defName>My_Sword</defName>
  ...只写和父类不同的部分...
</ThingDef>
```

> 本项目这三个 Def 都很小、没有合适的父类，所以直接全写出来，反而更清楚。

## 2.5 铁律：写 Def 之前先查原版

**不要凭记忆/猜测写字段名和取值。** 每个字段都该去原版确认：

- 方法一（推荐）：用 RimSage MCP 搜，例如查"WorkGiverDef 有哪些字段"。
- 方法二：直接去 `<RimWorld>/Data/Core/Defs/` 里 `grep` 类似的东西，照着抄结构。

> 反面教材：`techLevel` 的合法值是 `Ultra`，不是凭直觉的 `Ultratech`。一次搜索就能避免的错。

## 动手练

1. 把 `PMM_Jobs.xml` 里的 `reportString` 改成 `chipping the mountain.`（纯 XML，不用重启也能重载），进游戏看小人状态栏文字变了没。
2. 把 `priorityInType` 从 `110` 改成 `1`，观察小人是不是"把凿岩顶排在最后"了。

## 本章小结

- Def = XML 写的数据表；根节点 `<Defs>`；`defName` 是唯一内部 ID，要加前缀。
- 本项目靠三个 Def 声明了"指派标记 / 一件工作 / 派活规则"。
- `driverClass` / `giverClass` 把 XML 数据和 C# 行为连了起来。
- 写字段前先查原版。

> 下一章 → [第 3 章：XML Patch 修改原版](03-XML-Patch修改原版.md)
