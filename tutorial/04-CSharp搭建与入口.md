# 第 4 章：C# 搭建与入口

> 本章目标：搞懂什么时候必须写 C#，把工程搭起来、能编译出 DLL，并理解两个"入口级"类。
> 打开对照：`Source/MountainMoving/MountainMoving.csproj`、`MountainMovingMod.cs`、`PMM_DefOf.cs`、`build.bat`

## 4.1 什么时候必须写 C#？

回到那句话：**XML 定义数据，C# 定义行为。**

| 你想做的 | 纯 XML 够吗 |
|---|---|
| 加一把武器/建筑/植物/服装（数值+贴图） | ✅ 纯 XML |
| 改原版的某个数值 | ✅ XML Patch |
| **全新的交互/逻辑**（比如"按极高工作量凿岩顶"、"进度要能存起来"） | ❌ 必须 C# |

本项目的核心——"殖民者怎么一格一格凿、凿了要记进度"——是**行为**，所以需要 C#。

> 本项目特意**不依赖 Harmony**（一个用来"拦截/改写原版方法"的库）。因为我们是**新增**内容，不是改原版逻辑，所以不需要它，构建也更简单。等你以后要"改原版某个方法的行为"时，再学 Harmony（见第 8 章进阶）。

## 4.2 工程文件 `.csproj` 逐行讲

`Source/MountainMoving/MountainMoving.csproj` 是告诉编译器"怎么编译"的文件。关键部分：

```xml
<TargetFramework>net472</TargetFramework>     <!-- 目标框架：.NET Framework 4.7.2（RimWorld 用它） -->
<AssemblyName>PMM.MountainMoving</AssemblyName> <!-- 编译出的 DLL 名字：PMM.MountainMoving.dll -->
<LangVersion>7.3</LangVersion>                 <!-- C# 语言版本，net472 配套用 7.3 -->
```

**引用 RimWorld 的 DLL**（编译时要"看得见"游戏里的类）：

```xml
<Reference Include="Assembly-CSharp">
  <HintPath>$(RimWorldPath)\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
  <Private>false</Private>   <!-- 关键：只引用，不把游戏的 DLL 拷进我们的 mod（版权红线） -->
</Reference>
```

- `Assembly-CSharp.dll` 里装着 RimWorld 的全部游戏代码（`Verse`、`RimWorld` 命名空间）。
- `UnityEngine*.dll` 是 Unity 引擎的（`Rect`、`Texture2D` 这些）。
- `<Private>false</Private>` 很重要：**我们绝不把原版 DLL 打进自己的 mod**——那是侵权。我们只是"引用"它来通过编译。

> `$(RimWorldPath)` 是 RimWorld 的安装目录，`build.bat` 会自动探测后传进来。

## 4.3 一键编译 `build.bat`

`build.bat` 做的事，说白了就是三步：

```
1. 找到 RimWorld 装在哪（自动探测常见 Steam 路径，找不到就让你设 RIMWORLD_PATH）
2. 调用 dotnet build 编译 .csproj
3. 把生成的 PMM.MountainMoving.dll 拷到 mod 的 Assemblies\ 目录
```

双击运行即可。看到 `[成功] 已生成 Assemblies\PMM.MountainMoving.dll` 就说明编译过了。
**改完任何 .cs，都要重新跑 build.bat 并重启游戏**（回忆第 1 章：DLL 要重新加载）。

## 4.4 Mod 入口类：游戏怎么"唤醒"你的 C#

游戏加载 DLL 后，会自动找到继承自 `Verse.Mod` 的那个类并实例化——这就是入口。
看 `MountainMovingMod.cs`：

```csharp
public class MountainMovingMod : Mod
{
    public static MountainMovingSettings settings;

    public MountainMovingMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<MountainMovingSettings>();  // 读取出存档无关的全局设置
    }

    public override void DoSettingsWindowContents(Rect inRect)  // 画"Mod 设置"窗口
    {
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory() => "Pooi's Mountain Moving"; // 设置列表里的名字
}
```

要点：
- 一个 DLL 里**最多一个** `Mod` 子类，游戏靠它找到入口。
- `GetSettings<T>()` 负责加载/创建设置对象（第 6 章细讲设置本身）。
- `DoSettingsWindowContents` / `SettingsCategory` 是可选的，有了它你的 mod 就有自己的设置面板。

## 4.5 DefOf：在 C# 里"拿到"XML 定义的 Def

第 2 章我们用 XML 定义了 `defName` 为 `PMM_RemoveThickRoof` 的指派标记。C# 里要用它，最稳妥的方式是 **DefOf**。
看 `PMM_DefOf.cs`：

```csharp
[DefOf]
public static class PMM_DefOf
{
    public static DesignationDef PMM_RemoveThickRoof;   // 字段名 == XML 里的 defName
    public static JobDef         PMM_RemoveThickRoofJob;

    static PMM_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PMM_DefOf));
    }
}
```

- `[DefOf]` + 这个静态构造函数，会让游戏在"解析引用"阶段（流水线阶段 D）**按字段名自动填入对应的 Def**。
- 所以**字段名必须和 defName 一字不差**——这就是第 1、2 章反复提的那条铁律的由来。
- 之后代码里直接用 `PMM_DefOf.PMM_RemoveThickRoof`，比手写字符串 `DefDatabase<...>.GetNamed("PMM_RemoveThickRoof")` 更安全（拼错会在加载时就暴露，而不是运行时才崩）。

## 4.6 命名空间规范

所有 .cs 都用同一个命名空间：

```csharp
namespace PMM.MountainMoving { ... }   // 前缀.Mod名
```

XML 里的 `driverClass` / `giverClass` / 补丁里的类名，都要写**带命名空间的完整类名**，例如 `PMM.MountainMoving.JobDriver_RemoveThickRoof`，游戏才能找到。

## 动手练

1. 跑一次 `build.bat`，确认能在 `Assemblies/` 看到 DLL。如果报"找不到 RimWorld"，按提示设 `RIMWORLD_PATH`。
2. 把 `SettingsCategory()` 返回的名字改成你自己的，重新编译重启，看设置列表里的名字变了。

## 本章小结

- 行为逻辑用 C#；本项目是"新增内容"，不需要 Harmony。
- `.csproj` 负责编译配置，引用游戏 DLL 时务必 `<Private>false</Private>`。
- `Mod` 子类是入口；`[DefOf]` 把 XML 的 Def 安全地接到 C#。
- 改 C# → 重跑 build.bat → 重启游戏。

> 下一章 → [第 5 章：C# 核心三件套](05-CSharp核心三件套.md)
