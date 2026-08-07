# RimWorld Mod 开发新手教程 —— 以「Pooi's Mountain Moving」为例

> 面向：**零 mod 开发经验**的玩家。不需要会 C#，但需要会基本的文件操作、看得懂一点点英文。
> 目标：跟着做完，你将**完整理解一个含 XML + C# 的 mod 是怎么写出来的**，并能动手做自己的 mod。

## 为什么拿这个 mod 当教材？

「移除厚岩顶」功能不复杂，但它**恰好踩中了 mod 开发的几乎所有核心知识点**：

| 知识点 | 本项目里对应的文件 |
|---|---|
| Mod 元信息 | `About/About.xml` |
| 用 XML 声明"数据"（Def） | `Defs/` 三个文件 |
| 用 XML 补丁修改原版（Patch） | `Patches/PMM_Architect.xml` |
| 用 C# 实现"行为" | `Source/MountainMoving/*.cs` |
| 存档持久化（进度保存） | `Source/.../MapComponent_RoofRemoval.cs` |
| Mod 设置界面 | `Source/.../MountainMovingSettings.cs` |
| 贴图资源 | `Textures/PMM/*.png` |
| 中/英本地化 | `Languages/` |
| 编译与构建 | `build.bat` + `*.csproj` |
| 测试与排错 | 第 8 章 |

学会这一个，就掌握了做绝大多数 mod 的通用套路。

## 核心心智模型（先记住这两句话）

1. **XML 定义"数据是什么"，C# 定义"行为怎么做"。**
   一把武器的伤害值、一个工作叫什么——是 XML（数据）。
   "殖民者怎么一步步把岩顶凿掉"——是 C#（行为）。
2. **游戏加载 mod 是分阶段的流水线**，理解了它，90% 的"为什么我的 mod 不生效"都有了答案（第 1 章讲）。

## 学习路线 / 目录

### 基础篇（01–08）：跟着示例 mod 从零到发布

| 章 | 标题 | 你会学到 |
|---|---|---|
| [01](01-准备与原理.md) | 准备与原理 | 所需工具；**mod 加载流水线**；最小可运行 mod |
| [02](02-XML-Def声明数据.md) | XML Def 声明数据 | Def 是什么；写好本 mod 的 3 个 Def |
| [03](03-XML-Patch修改原版.md) | XML Patch 修改原版 | PatchOperation；把按钮注入"命令"面板 |
| [04](04-CSharp搭建与入口.md) | C# 搭建与入口 | 何时需要 C#；建工程；引用 DLL；Mod 入口类 |
| [05](05-CSharp核心三件套.md) | C# 核心三件套 | **指令 → 派活 → 干活** 三个类逐行精讲（本教程核心） |
| [06](06-持久化与设置.md) | 持久化与 Mod 设置 | 为什么进度要存 MapComponent；设置界面 |
| [07](07-贴图与本地化.md) | 贴图与本地化 | 贴图路径规则；Keyed vs DefInjected 翻译 |
| [08](08-测试调试与发布.md) | 测试、调试与发布 | Dev 模式；读 Player.log；常见红字；正规化与发布 |

### 进阶篇（09–13）：示例 mod 没用到、但很常见的主题

| 章 | 标题 | 你会学到 |
|---|---|---|
| [09](09-添加物品与武器.md) | 添加物品与武器 | ThingDef；ParentName 继承链；纯 XML 做一把武器 |
| [10](10-添加建筑.md) | 添加建筑 | Building ThingDef；建造菜单；costList / WorkToBuild |
| [11](11-配方与研究.md) | 配方与研究 | RecipeDef 挂工作台；ResearchProjectDef 科技树 |
| [12](12-Harmony补丁入门.md) | Harmony 补丁入门 | 修改原版行为；Prefix/Postfix；0Harmony 依赖 |
| [13](13-ThingComp组件.md) | ThingComp 组件 | 给物品挂自定义行为；机制选型对照 |

## 怎么用这本教程

- **边看边对照源码**：每一章都标了"打开哪个文件"，请把真实文件打开对着看。
- **动手改一改**：每章末尾有"动手练"，改坏了大不了删了重来。
- **遇到红字别慌**：第 8 章有"常见错误对照表"。

## 先备知识自查

- 会在 Windows 上新建/编辑文本文件（推荐 VS Code，免费）。
- 知道 RimWorld 的安装目录在哪、知道 `Mods` 文件夹。
- （可选）会装 .NET SDK 或 Visual Studio——只有写 C# 时才需要（第 4 章）。

> 开始吧 → [第 1 章：准备与原理](01-准备与原理.md)
