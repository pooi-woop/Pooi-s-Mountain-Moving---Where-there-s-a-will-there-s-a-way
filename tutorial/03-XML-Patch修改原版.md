# 第 3 章：XML Patch 修改原版

> 本章目标：学会用 PatchOperation **在不改原版文件的前提下**修改原版数据，并把我们的按钮塞进"命令"面板。
> 打开对照：`Patches/PMM_Architect.xml`

## 3.1 为什么需要 Patch？

我们想往游戏自带的「建筑师 → 命令」面板里**加一个新按钮**。但"命令"面板是原版定义的，直接改原版文件有两个大问题：

1. 游戏一更新，你的改动就被覆盖。
2. 两个 mod 都改同一个文件会互相冲突。

**PatchOperation（XML 补丁）**解决了这个问题：它不碰原版文件，而是在**游戏加载时**（回忆第 1 章流水线的阶段 C）动态地把你的修改"打"到内存里的 Def 数据上。

## 3.2 补丁文件长什么样

所有补丁文件放在 `Patches/` 下，根节点是 `<Patch>`，里面是一个个 `<Operation>`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationAdd">
    <xpath>/Defs/DesignationCategoryDef[defName="Orders"]/specialDesignatorClasses</xpath>
    <value>
      <li>PMM.MountainMoving.Designator_RemoveThickRoof</li>
    </value>
  </Operation>
</Patch>
```

三部分：

| 部分 | 作用 |
|---|---|
| `Class="PatchOperationAdd"` | 用哪种操作。这里是"**新增**"。 |
| `<xpath>` | **改哪里**：用 XPath 定位到某个 Def 的某个字段。 |
| `<value>` | **改成什么**：要插入/替换的内容。 |

## 3.3 逐行拆解本项目的补丁

**目标**：往"命令"面板的 `specialDesignatorClasses`（特殊指令类列表）里加一行我们自己的设计器类名。

```xml
<xpath>/Defs/DesignationCategoryDef[defName="Orders"]/specialDesignatorClasses</xpath>
```

这句 XPath 的意思是：

```
/Defs                                                  根节点
  /DesignationCategoryDef[defName="Orders"]            找 defName 等于 "Orders" 的那个分类（即"命令"面板）
    /specialDesignatorClasses                          它下面的 specialDesignatorClasses 字段
```

```xml
<value>
  <li>PMM.MountainMoving.Designator_RemoveThickRoof</li>
</value>
```

`PatchOperationAdd` 会把 `<value>` 里的内容**追加**到 xpath 定位到的那个列表里。
`specialDesignatorClasses` 是一个"类名列表"，游戏在解析引用阶段（流水线阶段 D）会把每个类名实例化成真正的按钮。于是我们写的 `<li>完整类名</li>` 就变成了"命令"面板里的一个新按钮。

> 结果：「建筑师 → 命令」里多出"移除厚岩顶"按钮，而我们**一行原版文件都没改**。

## 3.4 常用 PatchOperation 速查

| 操作 | 干什么 | 典型用途 |
|---|---|---|
| `PatchOperationAdd` | 追加节点 | 往列表里加一项（本章用的） |
| `PatchOperationReplace` | 替换节点 | 改某个数值/字段 |
| `PatchOperationRemove` | 删除节点 | 去掉原版的某样东西 |
| `PatchOperationAttributeSet` | 设置属性 | 改标签上的 attribute |
| `PatchOperationConditional` | 条件判断 | "如果存在某节点才改"，防止报错 |
| `PatchOperationSequence` | 按顺序执行一串 | 组合多个操作 |
| `PatchOperationFindMod` | 判断某 mod 是否启用 | 做兼容补丁 |

> 进阶提示：改一个不保证存在的节点时，先用 `PatchOperationConditional` 包一层 `<xpath>` 探测，能避免"补丁打不上"的红字。

## 3.5 XPath 入门（够用就行）

- `/Defs/ThingDef` —— 所有 ThingDef。
- `/Defs/ThingDef[defName="X"]` —— 只要 defName 为 X 的那个。
- `.../statBases/WorkToBuild` —— 继续往下钻到某字段。
- `*` 匹配任意节点，`[1]` 取第一个，更多语法可自行搜索，但做补丁 90% 只用到上面这几种。

## 3.6 调试补丁

补丁打不上时，游戏会在日志里报 `Patch operation failed` 之类的错（第 8 章教你看日志）。常见原因：

- xpath 写错 → 定位不到节点。
- 目标字段名记错（又回到那条铁律：**先查原版**确认字段名，比如 `specialDesignatorClasses` 是查出来的，不是猜的）。
- `PatchOperationAdd` 的目标必须是**已存在**的节点；不存在要用 `PatchOperationAdd` 的父级或改用别的操作。

## 动手练

1. 把补丁里的 `Orders` 改成 `Floors`（一个不存在的、或不该放这的分类），重启看补丁报错的提示长什么样，再改回来。
2. 试着再写一个 `PatchOperationReplace`，把某个原版建筑的 `WorkToBuild` 改小，体会"替换"和"新增"的区别。

## 本章小结

- Patch = 加载时动态修改 Def，不碰原版文件，天然防冲突。
- 三要素：操作类型 + xpath 定位 + value 内容。
- 本项目用一次 `PatchOperationAdd` 就把新按钮加进了"命令"面板。

> 下一章 → [第 4 章：C# 搭建与入口](04-CSharp搭建与入口.md)
