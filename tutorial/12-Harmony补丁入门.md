# 第 12 章：Harmony 补丁入门（修改原版行为）

> 本章目标：搞懂什么时候需要 Harmony，把它接进本工程，并亲手写出、编译、在游戏中验证你的第一个补丁。
> 打开对照：`About/About.xml`、`Source/MountainMoving/MountainMoving.csproj`，以及本章将新建的 `Source/MountainMoving/HarmonyPatches.cs`

## 12.1 为什么需要 Harmony？

第 4 章埋过一个伏笔："本项目特意**不依赖 Harmony**……等你以后要'改原版某个方法的行为'时，再学它。"现在到时候了。

回忆那条分界线：**XML 定义数据，C# 定义行为**。但 C# 的活也分两种：

| 你想做的 | 手段 |
|---|---|
| 加新武器/新建筑/新指令（像基础篇做的） | 写自己的 C# 类，游戏按 Def 加载它——**不需要 Harmony** |
| **改原版（或别的 mod）某个方法的行为** | 你拿不到它的源码，只能用 **Harmony** 在运行时拦截 |

比如："让原版的'移除屋顶'也能选厚岩顶"、"所有武器耐久掉一半"、"袭击概率翻倍"——这些都要在**别人的方法执行时插一脚**。

**Harmony 是什么**：一个运行时补丁库。它在游戏运行中把指定方法"拦下来"，让**你的代码先跑 / 后跑 / 干脆替它跑**——而原版 DLL 文件一个字节都没动。这和第 3 章 XML Patch 的精神一脉相承：不碰源文件，所以天然防冲突、游戏更新也不会覆盖你的改动。

## 12.2 Harmony vs XML Patch：各管一段

| | XML Patch（第 3 章） | Harmony（本章） |
|---|---|---|
| 管什么 | **数据**（Def 里的字段、列表） | **行为**（方法的逻辑、返回值） |
| 什么时候动手 | 游戏加载时（流水线阶段 C） | 方法每次被调用时 |
| 需要编译 | 不需要 | 需要（写 C#） |
| 冲突风险 | 低 | 中（多个 mod 补丁同一方法时要注意） |
| 学习难度 | 低 | 中高 |

一句话：**改数据用 XML Patch，改行为用 Harmony，纯新增什么都不用。**

## 12.3 新增依赖：两步把 Harmony 接进工程（重点）

基础篇的 mod 没做这两步，这是本章**唯一的工程改动**，别漏。

### 第 1 步：csproj 里引用 `0Harmony.dll`

RimWorld 自带的 `RimWorldWin64_Data/Managed/` 里就有 `0Harmony.dll`（游戏自己也在用 Harmony）。打开 `MountainMoving.csproj`，在引用 Assembly-CSharp 的那个 `<ItemGroup>` 里加：

```xml
<Reference Include="0Harmony">
  <HintPath>$(RimWorldPath)\RimWorldWin64_Data\Managed\0Harmony.dll</HintPath>
  <Private>false</Private>   <!-- 老规矩：只引用不打包，运行时由 Harmony mod 提供 -->
</Reference>
```

> 如果你的 Managed 文件夹里找不到它，就去 Harmony mod 自己的目录拿（Workshop 版在 `steamapps\workshop\content\294100\2009463077\` 下的 `Assemblies\` 里），把 HintPath 指过去即可。

### 第 2 步：About.xml 声明依赖 `brrainz.harmony`

补丁在运行时是靠 **Harmony mod**（作者 brrainz，Workshop 搜 "Harmony"，几乎每个 mod 玩家都订阅了）加载的。打开 `About/About.xml`，在 `</supportedVersions>` 后面加：

```xml
<modDependencies>
  <li>
    <packageId>brrainz.harmony</packageId>
    <displayName>Harmony</displayName>
  </li>
</modDependencies>
```

声明后游戏会自动把 Harmony 排在**本 mod 之前**加载（顺序错了补丁打不上）。自己测试时，记得订阅并勾选 Harmony。

## 12.4 入口类：游戏启动时"打上所有补丁"

新建 `Source/MountainMoving/HarmonyPatches.cs`，先写入口：

```csharp
using HarmonyLib;
using System.Reflection;
using Verse;

namespace PMM.MountainMoving
{
    [StaticConstructorOnStartup]   // 游戏加载完、进主菜单前，自动跑这个静态构造函数
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("pooi.mountainmoving");  // 补丁 ID：全局唯一，直接用 packageId 最稳
            harmony.PatchAll(Assembly.GetExecutingAssembly());     // 扫描本 DLL 里所有 [HarmonyPatch] 并打上
            Log.Message("pooi.mountainmoving: Harmony patches applied.");
        }
    }
}
```

三件套记牢：**`[StaticConstructorOnStartup]` + `new Harmony("唯一ID")` + `PatchAll(...)`**。之后只管往这个文件里加补丁类，`PatchAll` 会自动发现它们。

## 12.5 三种补丁 + 注入参数速览

### Prefix（前置）：在原方法**之前**跑

```csharp
[HarmonyPrefix]
public static bool Prefix(/* 参数 */)
{
    // 能做三件事：改参数（参数加 ref）、return false 直接跳过原方法、提前设置 __result
    return true;   // true = 放行，原方法照常执行；false = 拦截
}
```

### Postfix（后置）：在原方法**之后**跑

```csharp
[HarmonyPostfix]
public static void Postfix(/* 参数 */, ref float __result)
{
    // 读/改 __result（原方法的返回值）；常用来"在结果上微调"或打日志
}
```

### Transpiler：直接改写方法的 IL 指令

威力最大也最容易炸，新手阶段**知道有这东西就行**——99% 的需求用 Prefix + Postfix 组合都能解决。

### Harmony 的"魔法参数"（按名字注入，名字就是暗号）

| 写法 | 是什么 | 典型用途 |
|---|---|---|
| `__instance` | 被补丁方法所属的那个实例（静态方法没有） | 读对象当前状态 |
| `__result` | 原方法的返回值（加 `ref` 可改） | Postfix 改结果；Prefix 提前给结果 |
| `__state` | Prefix 和 Postfix 之间传的"小纸条" | Prefix 记录现场，Postfix 取用 |
| `___字段名` | 三个下划线：实例的**私有字段** | 读/改没有公开属性的内部数据 |
| 同名参数 | 与原方法签名同名的参数（如 `IntVec3 c`）直接注入 | 读这次调用传进来的参数 |

> 规则只有一条：**名字必须和暗号/原方法签名一字不差**。写错了不会报错，只会注入进来一个 null，非常坑。

## 12.6 完整示例：一格厚岩顶消失时打日志

光说不练假把式。我们来补丁原版的 `RoofGrid.SetRoof`（每当某格屋顶被设置/移除都会经过它）：**一格厚岩顶被移除时打一条日志**。巧的是，本 mod 自己凿岩顶时（第 5 章 JobDriver 里的 `SetRoof(cell, null)`）也会触发它——正好拿来验证。

把下面这个补丁类**追加**到 `HarmonyPatches.cs` 里（一个 .cs 文件可以放多个类）：

```csharp
    // 补丁目标：Verse.RoofGrid 类的 SetRoof 方法
    [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]  // nameof 防拼错；写字符串 "SetRoof" 也行
    public static class Patch_RoofGrid_SetRoof
    {
        // Prefix：变动【前】，先记下旧屋顶，塞进 __state 传给 Postfix
        [HarmonyPrefix]
        public static void Prefix(RoofGrid __instance, IntVec3 c, out RoofDef __state)
        {
            __state = __instance.RoofAt(c);   // RoofAt：查某格屋顶（第 5 章用过）
        }

        // Postfix：变动【后】对比——旧的是厚岩顶、现在变 null，就是"被移除了"
        [HarmonyPostfix]
        public static void Postfix(IntVec3 c, RoofDef def, RoofDef __state)
        {
            if (__state != null && __state.isThickRoof && def == null)
                Log.Message($"[PMM] 一格厚岩顶消失了：{c}");
        }
    }
```

对照 12.5 的表读一遍：`__instance` 是 RoofGrid 本体，`c`/`def` 是这次调用的参数，`__state` 把 Prefix 看到的旧屋顶递给了 Postfix。类名 `Patch_类名_方法名` 是社区惯例，见名知意。

### 跑通闭环：引用 → 写补丁 → 编译 → 进游戏验证

1. 确认 12.3 两步依赖已加 → 跑 `build.bat` → 重启游戏。
2. Mod 列表确认 **Harmony 已勾选且排在本 mod 上方**。
3. 进存档开 Dev Mode，把"每格工作量"调到最小（第 8 章的技巧），框选一格厚岩顶让小人凿。
4. 凿掉瞬间打开日志：看到 `[PMM] 一格厚岩顶消失了：(x, y, z)` 就成功了！（启动时那行 `Harmony patches applied.` 也应该在。）

> 细品一下：触发补丁的正是**我们自己 mod 的代码**。Harmony 才不管谁调的——只要方法被执行，一律拦截。这就是它能改原版行为的原理。

## 12.7 安全实践与常见坑

**写好补丁的四条纪律：**

1. **ID 唯一**：`new Harmony("...")` 里用 packageId，别用 `"mypatch"` 这种大路货，避免和别的 mod 撞车。
2. **在 `[StaticConstructorOnStartup]` 里 PatchAll**：此时 Def 已就绪，时机刚刚好。
3. **新补丁先打日志**：第一版只写 `Log.Message("补丁被调用了")`，确认触发后再写真逻辑——先证明"拦到了"，再谈"改什么"。
4. **补丁类和补丁方法都必须 `static`**；`0Harmony.dll` 引用记得 `<Private>false</Private>`。

**常见坑对照表：**

| 症状 | 大概率原因 | 怎么办 |
|---|---|---|
| 补丁毫无效果、也没报错 | 没订阅/勾选 Harmony，或漏了 `modDependencies` | 回 12.3 第 2 步 |
| 编译报"找不到 HarmonyLib" | csproj 没加 0Harmony 引用 | 回 12.3 第 1 步 |
| 日志里有 Harmony 字样的红字 | 类名/方法名写错，补丁没打上 | 用 `nameof`；核对目标方法确实存在 |
| 参数/`__instance` 注入进来是 null | 参数名和原方法签名不一致 | 用 dnSpy 查原始签名（第 8 章提过） |
| 日志被刷屏 | 补丁没收窄触发条件 | 像 12.6 那样先判断再 Log |

## 动手练

1. 跑通 12.6 后，把 Postfix 里的条件（`__state.isThickRoof && def == null`）临时删掉，开上帝模式随手铺/拆各种屋顶，看日志被刷屏——亲身体会"补丁为什么要尽量收窄触发条件"，然后改回去。
2. 体验 Prefix 拦截：把 12.6 的 Prefix 换成下面这个带返回值的版本（一个补丁类里 Prefix 只能有一个），进游戏会发现**连本 mod 自己的指令都凿不动岩顶了**——因为它也走 `SetRoof`。练完记得改回去，否则本 mod 就废了。

```csharp
        [HarmonyPrefix]
        public static bool Prefix(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            RoofDef oldRoof = __instance.RoofAt(c);
            if (oldRoof != null && oldRoof.isThickRoof && def == null)
                return false;   // 拦截：谁也不许移除厚岩顶，原方法直接不执行
            return true;        // 其余情况照常放行
        }
```

## 本章小结

- **要改原版/别人 mod 的方法行为 → Harmony**；改数据用 XML Patch；纯新增（像基础篇）什么都不用。
- 接入两步：csproj 引用 `0Harmony.dll`（`Private=false`）+ About.xml 声明 `brrainz.harmony` 依赖。
- 入口三件套：`[StaticConstructorOnStartup]` + `new Harmony("pooi.mountainmoving")` + `PatchAll`。
- Prefix 可改参数、`return false` 拦截；Postfix 读写 `__result`；Transpiler 改 IL，先知道即可。
- 魔法参数：`__instance`、`__result`、`__state`、`___字段名`、同名参数——名字就是暗号，一字不能错。
- 调试心法：先 `Log.Message` 确认补丁被触发，再写真逻辑。

> 下一章 → [第 13 章：ThingComp 组件](13-ThingComp组件.md)
