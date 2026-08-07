# 第 13 章：ThingComp 组件（给物品挂自定义行为）

> 本章目标：理解 ThingComp 这套"给任意东西挂可复用行为"的机制，学会 `CompProperties` + `ThingComp` 的配对写法，亲手做一个**会自我修复的墙**，并建立"什么时候该用哪种机制"的选型直觉。
> 打开对照：本章示例是**全新写**的，需要你新建两个文件——`Source/MountainMoving/CompPMMRegen.cs`（C#）和 `Defs/PMM_LivingWall.xml`（Def）。类名可自定，下文统一用这套。

## 13.1 先建立心智模型：什么是 Comp？

RimWorld 里几乎所有"东西"——墙、武器、盔甲、小人、电池——都是一个 **Thing**。
而 **ThingComp（组件）** 就是**挂在 Thing 身上的一块可插拔的"功能模块"**：一个 Thing 可以同时挂好几个 Comp，每个 Comp 各管一摊事。

原版自己就满地是 Comp：
- 电池"能存电/放电" → 一个 Comp；
- 食物"会腐烂" → 一个 Comp；
- 装备"会磨损" → 一个 Comp；
- 容器"能装东西" → 一个 Comp。

**为什么要用 Comp，而不是直接给 Thing 写个子类？** 因为 **组合优于继承**：同一个"自我修复"Comp，既能挂到墙上、也能挂到剑上、挂到生物上——不用为每种东西各写一个新 Thing 子类。行为变成了一块块能随便拼的积木。

**Comp 永远成对出现**，是一对好搭档：

| 类 | 角色 | 类比 |
|---|---|---|
| `CompProperties_Xxx` | **数据定义**：这类东西有哪些"出厂参数"，在 XML 里填；所有实例共享同一份 | 产品说明书 / 模具 |
| `CompXxx`（继承 `ThingComp`） | **行为实现**：每个实例自己那份运行时状态和逻辑，各记各的 | 每台机器自己的运转状态 |

一堵活化石墙和另一堵，用的是**同一份说明书**（比如"每次回 2 点血"），但各自的**计时器、当前血量**是分开的——这正是 Comp 的精髓。

## 13.2 配对写法：两处"接线"是关键

写一对 Comp，90% 的错都出在两处"接线"上，先记住：

1. **`CompProperties` 的构造函数里**：写 `compClass = typeof(你的ThingComp类);` —— 告诉游戏"行为由谁实现"。
2. **`ThingComp` 里**：用 `Props` 把那份数据取回来 —— `public CompProperties_Xxx Props => (CompProperties_Xxx)props;`
   （框架把 CompProperties 存在字段 `props` 里，`Props` 只是包了一层强转，用着顺手。）

骨架长这样（完整可编译版在 13.4）：

```csharp
public class CompProperties_PMMRegen : CompProperties
{
    public int healAmount = 2;
    public CompProperties_PMMRegen() { compClass = typeof(CompPMMRegen); }   // 接线①
}

public class CompPMMRegen : ThingComp
{
    public CompProperties_PMMRegen Props => (CompProperties_PMMRegen)props;  // 接线②
}
```

## 13.3 XML 怎么接：comps / li / Class

在 ThingDef 里加一个 `<comps>`，里面每个 `<li>` 挂一个 Comp。**注意：`Class=` 指向的是 `CompProperties` 那个类（不是 ThingComp），而且要写带命名空间的全名**：

```xml
<comps>
  <li Class="PMM.MountainMoving.CompProperties_PMMRegen">
    <healAmount>2</healAmount>     <!-- 字段名要和 CompProperties 里的公开字段一字不差 -->
    <tickInterval>120</tickInterval>
  </li>
</comps>
```

`<li>` 里的每个子标签，都会按名字填进 `CompProperties` 的对应公开字段——这是"XML 定义数据、C# 定义行为"的又一次体现。

## 13.4 完整示例：会自我修复的活化石墙

我们做一堵"活化石墙"：被打坏后不用派人修理，它自己一点点长回来。

**XML（新建 `Defs/PMM_LivingWall.xml`）：**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <ThingDef ParentName="BuildingBase">
    <defName>PMM_LivingWall</defName>
    <label>活化石墙</label>
    <description>一块会缓慢自我修复的活化岩石。受损后不必派人修理——它自己就会长回来。</description>

    <!-- 先借第 5 章那张图标当贴图，能显示就行；正式贴图的做法见第 7 章 -->
    <graphicData>
      <texPath>PMM/UI/RemoveThickRoof</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>

    <passability>Impassable</passability>
    <rotatable>false</rotatable>
    <tickerType>Normal</tickerType>   <!-- 关键：Normal 才会每 tick 调用 CompTick -->

    <statBases>
      <MaxHitPoints>300</MaxHitPoints>   <!-- 血上限，自我修复要封顶到它 -->
    </statBases>

    <comps>
      <li Class="PMM.MountainMoving.CompProperties_PMMRegen">
        <healAmount>2</healAmount>
        <tickInterval>120</tickInterval>
      </li>
    </comps>
  </ThingDef>
</Defs>
```

**C#（新建 `Source/MountainMoving/CompPMMRegen.cs`）：**

```csharp
using Verse;

namespace PMM.MountainMoving
{
    // ===== ① 数据定义：XML 里能调哪些"出厂参数"，在这里声明 =====
    public class CompProperties_PMMRegen : CompProperties
    {
        public int healAmount = 2;      // 每次回多少点血
        public int tickInterval = 120;  // 隔多少 tick 回一次（60 tick ≈ 1 秒）

        public CompProperties_PMMRegen()
        {
            compClass = typeof(CompPMMRegen);   // 接线①：行为由哪个 ThingComp 实现
        }
    }

    // ===== ② 行为实现：每个实例自己那份状态和逻辑 =====
    public class CompPMMRegen : ThingComp
    {
        public CompProperties_PMMRegen Props => (CompProperties_PMMRegen)props;  // 接线②

        private int tickCounter;   // 每个实例自己的计时器（这堵墙和那堵墙各算各的）

        // 生成到地图上时调用；读档重新生成也会调，靠参数区分
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 计时器由 PostExposeData 在读档时恢复，这里不用动。
            // 若有"只在第一次生成做一次"的初始化，写进 if (!respawningAfterLoad) { ... }
        }

        // 每 tick 调一次（前提：ThingDef 的 tickerType 是 Normal）
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;    // 不在地图上（箱子/物品栏里）就别跑
            if (parent.HitPoints >= parent.MaxHitPoints) { tickCounter = 0; return; }  // 满血不浪费

            tickCounter++;
            if (tickCounter < Props.tickInterval) return;   // 没攒够就下个 tick 再来
            tickCounter = 0;

            parent.HitPoints += Props.healAmount;           // 回血
            if (parent.HitPoints > parent.MaxHitPoints)     // 手动封顶
                parent.HitPoints = parent.MaxHitPoints;
        }

        // 存档 / 读档：把要存的字段交给 Scribe（呼应第 6 章）
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }

        // 选中它时，在左下角检查面板追加一行自定义文字（调试用超好用）
        public override string CompInspectStringExtra()
        {
            if (parent.HitPoints >= parent.MaxHitPoints)
                return "活化石墙：完好 " + parent.HitPoints + " / " + parent.MaxHitPoints;
            return "自我修复中：" + parent.HitPoints + " / " + parent.MaxHitPoints
                 + "（计时 " + tickCounter + "/" + Props.tickInterval + "）";
        }
    }
}
```

**怎么在游戏里看到效果：**
1. 编译 DLL 放进 `Assemblies/`（第 4 章）、XML 放进 `Defs/`（第 2 章），重启游戏。
2. 开 Dev 模式（第 8 章）→ Debug Actions → Spawn thing → 搜 `PMM_LivingWall` 放一块。
3. **选中它**：左下角检查面板多出一行"活化石墙：完好 300/300"——这行字本身就是 Comp 已挂上、且在跑的铁证。
4. 打掉点血（Dev 的伤害工具，或让小人攻击它）：检查行变成"自我修复中…（计时 73/120）"，看着血一点点涨回去。

## 13.5 常用钩子速查

Comp 的生命周期钩子就这几个，按需重写：

| 钩子 | 何时被调用 | 典型用途 / 注意 |
|---|---|---|
| `PostSpawnSetup(respawningAfterLoad)` | 东西被生成到地图时（读档重生成也会，用参数区分） | 初始化；`if (!respawningAfterLoad)` 里做"仅首次"的事 |
| `CompTick()` | **每 tick**（需 `tickerType=Normal`） | 高响应周期逻辑；**千万别在里面做重型操作**，会像上面那样攒够间隔再动手 |
| `CompTickRare()` / `CompTickLong()` | 每 250 / 2000 tick | 低频逻辑，比 CompTick 省性能，能用它俩就别用 CompTick |
| `PostExposeData()` | 存档 / 读档 | 用 `Scribe_*` 存字段（第 6 章那套，一字不差） |
| `CompInspectStringExtra()` | 选中时刷新检查面板 | 显示自定义状态，验证 Comp 是否生效的最佳手段 |

## 13.6 常见坑速查

- **`Class=` 漏了命名空间** → 报"找不到类型"红字。我们的类在 `PMM.MountainMoving` 里，XML 必须写全名 `PMM.MountainMoving.CompProperties_PMMRegen`。
- **两个类搞反** → `compClass = typeof(...)` 指向 **ThingComp** 类；XML `Class=` 指向 **CompProperties** 类。别颠倒。
- **Comp 根本不 tick** → 检查 ThingDef 的 `<tickerType>`，必须是 `Normal`（或对应频率）才会调 CompTick。
- **XML 字段名拼错/大小写不对** → 那个值不会生效也不报错，对照 CompProperties 的公开字段名逐字检查。
- **要存的数据忘了进 `PostExposeData`** → 读档就丢（就是第 6 章那个坑）。
- **在 `parent.Spawned == false` 时乱动** → 东西在箱子/物品栏里时很多逻辑该直接 `return`。

## 13.7 该用哪种机制：一张对照表

进阶篇学了好几种"写行为"的手段，新手最容易纠结"这个需求该用哪个"。一句话口诀：**小人动 → Job；东西自己有行为/状态 → Comp；改现成方法 → Harmony；纯数据 → XML。**

| 你想做的事 | 该用 | 一句话理由 |
|---|---|---|
| 只是声明数值/配方/贴图/翻译等**纯数据**，没有运行时行为 | **纯 XML**（第 2、3 章） | 能不写 C# 就不写，最省事 |
| 给**某一类东西**挂"每个实例各自记一份"的行为/状态（自己动、自己记数据、要跟存档） | **ThingComp**（本章） | 一个 Thing 挂一份 Comp，天然是"每实例自己的数据"，自带 CompTick 和存档 |
| 让**小人**去做一件事（走过去、干活、一连串动作） | **JobDriver / WorkGiver**（第 5 章） | 小人的"行为"本质是 Job；Comp 管不了小人的腿和工作队列 |
| 改**原版/别人已有代码**的运行结果（你不拥有那段逻辑） | **Harmony**（第 12 章） | 改不到源码，只能在它的方法前后"插一手"；Comp 则是给你能控制的 Thing 加"自身行为" |

> 顺带：只想给某个 Def **多挂几个静态配置字段**（所有实例共享、不需要每实例状态、不需要 tick），可以用更轻的 **DefModExtension**，不必动用 ThingComp。

## 动手练

1. **只改 XML，不动 C#**：把 `<healAmount>` 改成 `10`、`<tickInterval>` 改成 `30`，重新进游戏（不用重新编译），感受恢复速度的变化——体会"出厂参数在 XML、逻辑在 C#"的分离。
2. **加一个会跟存档走的字段**：给 `CompPMMRegen` 加 `int totalHealed`，每次回血累加；在 `PostExposeData` 补一行 `Scribe_Values.Look` 存它；再在检查字符串里多显示一行"累计自愈 X 点"。（提示：忘了加 Scribe 那行，读档就归零——正好复习第 6 章。）

## 本章小结

- **ThingComp = 挂在 Thing 上的可插拔功能模块**，组合优于继承，一个 Thing 能挂多个。
- 成对写：`CompProperties`（数据，构造函数里 `compClass = typeof(...)`）+ `ThingComp`（行为，用 `Props` 取回数据）。
- XML 在 `<comps><li Class="命名空间.CompProperties_Xxx">` 里接，字段名逐字对应。
- 钩子：`PostSpawnSetup` / `CompTick`（别做重活）/ `PostExposeData`（配 Scribe 存档）/ `CompInspectStringExtra`。
- 选型口诀：**小人动 → Job；东西自己有行为 → Comp；改现成方法 → Harmony；纯数据 → XML。**

> 回到 [教程首页](README.md) ｜ 进阶篇到这里就完结了——从 XML 到 C#、从派活到组件、从补丁到持久化，你已经握齐了做一个完整 mod 的全套工具。剩下的，就是把你脑子里那个想法真正做出来。祝你玩得开心，也期待在创意工坊见到你的作品！
