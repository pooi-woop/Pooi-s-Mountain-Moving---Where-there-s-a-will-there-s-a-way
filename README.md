# Pooi's Mountain Moving（愚公移山）

让殖民者能够以**极高的工作量**移除**厚岩顶**（山体头顶 / overhead mountain）。

- 在「建筑师 → 命令」面板新增一个**「移除厚岩顶」区域指令**（可拖拽框选）。
- 被指派的厚岩顶由从事**建造(Construction)**工作的殖民者逐格凿除。
- **每格默认约 2 个游戏日**的工作量（可在 Mod 设置里调，0.1 ~ 10 个游戏日）。
- **进度按格保存**：吃饭、睡觉、存档/读档后都会接着上次进度继续凿，不会前功尽弃。
- 只针对**厚岩顶**，不影响普通屋顶/薄顶；纯新增内容，**不依赖 Harmony**。

> 📖 **新手教程**：本项目配套一套《[RimWorld Mod 开发新手教程](tutorial/README.md)》，
> 以本 mod 为示例，从零讲到发布（工具 → 加载原理 → XML Def/Patch → C# → 持久化 → 贴图本地化 → 测试发布）。

---

## 目录结构

```
Pooi-s-Mountain-Moving/
├── About/About.xml                  # Mod 元信息
├── Defs/
│   ├── DesignationDefs/PMM_Designations.xml   # 指派标记
│   ├── JobDefs/PMM_Jobs.xml                   # 工作定义
│   └── WorkGiverDefs/PMM_WorkGivers.xml       # 挂到"建造"工作类型
├── Patches/PMM_Architect.xml        # 把指令注入"命令"面板（XML Patch，无需 Harmony）
├── Assemblies/                      # 编译产物 PMM.MountainMoving.dll 放这里（构建后生成）
├── Textures/PMM/                    # 按钮图标 + 地图指派标记
├── Languages/                       # 英文回退 + 简体中文
├── Source/MountainMoving/           # C# 源码 + 工程文件
├── tools/generate_textures.ps1      # 重新生成图标的脚本
└── build.bat                        # 一键编译
```

## 构建（在装有 RimWorld 的电脑上）

1. 确保本机装有 **RimWorld**（编译要引用它的 DLL）和 **.NET SDK**（或 Visual Studio 2022）。
2. 双击运行 **`build.bat`**：
   - 脚本会自动探测常见 Steam 安装路径；找不到时会提示你设置环境变量
     `RIMWORLD_PATH`（指向 RimWorld 根目录）。
   - 编译成功后自动把 `PMM.MountainMoving.dll` 拷到 `Assemblies\`。
3. 首次编译若提示缺少 net472 目标包：工程已内置 `Microsoft.NETFramework.ReferenceAssemblies`，
   联网即可自动拉取；无网络则用 Visual Studio 的 MSBuild 编译。

> 也可以直接用 Visual Studio 打开 `Source/MountainMoving/MountainMoving.csproj`，
> 把 `RimWorldPath` 改成你的安装路径后手动编译，再把 DLL 放进 `Assemblies\`。

## 安装与测试

1. 把整个 **`Pooi-s-Mountain-Moving`** 文件夹复制到 `<RimWorld>/Mods/`。
2. 启动游戏 → Mod 列表勾选 **Pooi's Mountain Moving**（无需 Harmony）。
3. 进存档后：
   - 「建筑师 → 命令」里应看到 **「移除厚岩顶」** 按钮（灰山+红叉图标）。
   - 在厚岩顶（overhead mountain）上**框选**指派 → 格子出现琥珀色 X 标记。
   - 勾选「建造」的殖民者会走过来凿，头顶有**进度条**。
   - 中途让小人去吃饭/睡觉，回来后进度**应继续**而不是清零。
   - 凿满后该格岩顶消失、露出天空（变亮）。

### 快速验证（开发者模式）
- 开 Dev Mode，用 `God Mode` 随便铺一片厚岩顶或用山地地图测试。
- 想快速看到效果：Mod 设置里把「每格工作量」调到最小（约 0.1 天），几秒就能凿掉一格。

## Mod 设置

选项 → Mod 设置 → Pooi's Mountain Moving：
- **每格岩顶工作量**：滑块，6000~600000 tick（0.1~10 个游戏日），默认 120000（约 2 天）。
- **移除完成时发送提示**：默认关（避免大面积施工时刷屏）。

## 常见问题

| 现象 | 排查 |
|---|---|
| 按钮没出现 | 确认 `Assemblies\PMM.MountainMoving.dll` 已生成；看 `Player.log` 有无红字 |
| 红字 `Could not find type ... Designator_RemoveThickRoof` | DLL 没编译/没放进 Assemblies |
| 图标是空白 | 确认 `Textures\PMM\UI\RemoveThickRoof.png` 存在 |
| 小人不去凿 | 确认该格是厚岩顶、已被指派、且小人能走到**旁边一格**（山体深处够不着的格不会派活） |
| 进度被清零 | 不应发生（进度存 MapComponent）；若复现请反馈 Player.log |

## 卸载

本 mod 会向存档写入一个 MapComponent（保存凿岩进度）。**中途移除 mod** 会在读档时报一条
找不到类型的红字（一般可继续游戏）。建议在不再需要时先让进度清零再卸载。

## 后续（正规化 / 发布）

- 替换更精致的 `Textures\PMM\` 图标与 `About\Preview.png`（640×360）。
- `About.xml` 的 `packageId` / `author` 发布前改成你自己的 ID。
