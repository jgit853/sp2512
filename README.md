# 合规人机协同内容发布助手 (MVP)

本地 WPF (.NET 8) 应用，用于知乎等平台的合规发帖/回帖流程管理。默认 **仅支持人工确认**，不提供自动提交或群发能力。

## 目录结构
- `src/ComplianceAssistant.sln` 解决方案
- `src/App` WPF 主程序（MVVM + EF Core + Serilog）
- `src/App.Tests` xUnit 单元测试
- `build.ps1` 一键还原/构建脚本
- `installer/` 安装脚本占位（可扩展为 MSIX / Inno Setup）

## 快速开始
1. 安装 .NET 8 SDK（Windows）。
2. 运行 `pwsh build.ps1` 还原并编译。
3. 启动 `src/App` 项目（WPF，仅 Windows）。首次运行会创建本地 SQLite 数据库和默认平台/账号。

## 核心特性
- 任务管理：创建平台/账号对应的发帖或回帖任务，跟踪状态、截止时间与排期。
- 草稿与版本：Markdown 编辑、模板变量替换，每次保存生成版本，可复制/导出（Markdown/HTML）。
- 合规质检：通用+插件规则检查，医疗健康示例规则，重复度检查（Jaccard shingling）。
- 排期与频控：账号每日上限与最短间隔校验，排期后显示提醒说明（仅人工发布）。
- 插件机制：
  - `ZhihuPlugin`：打开知乎入口、复制草稿、导出，不做自动提交。
  - `GenericWebPlugin`：通用平台的打开/复制/导出动作。
- 审计日志：草稿保存、复制、导出、插件操作均写入本地日志。
- 安全：账号 token 应使用 Windows DPAPI 加密存储（接口预留）。

## 默认配置示例
平台与账号在首次运行自动创建：
```json
[
  {"name": "知乎", "pluginId": "zhihu", "notes": "仅人工发布"},
  {"name": "自定义网页", "pluginId": "generic-web"}
]
```

规则示例（`src/App/Rules/default-rules.json`）：
```json
{
  "general": [
    {"id": "manual_only", "title": "人工确认", "message": "禁止自动提交或群发", "severity": "High"},
    {"id": "avoid_medical_claims", "title": "医疗健康边界", "message": "避免夸大疗效或诊断性表述", "severity": "Medium"}
  ],
  "keywords": {
    "blacklist": ["治愈", "秒杀", "唯一"],
    "whitelist": ["示例"]
  }
}
```

## 已实现 / 未实现
- ✅ 任务/草稿管理、质检、排期频控校验、插件框架、Zhihu + 通用插件、导出/复制、模板变量替换、审计日志。
- ✅ EF Core + SQLite 持久化，Serilog 文件日志，xUnit 单元测试（规则引擎、模板替换）。
- ⚠️ 未实现：官方 API 发布（需平台官方支持和显式启用），MSIX/安装脚本仅占位，UI 仅为 MVP 级样式，Windows 通知触达需后续完善。

## 合规与安全声明
- 程序默认“人工确认模式”，无任何自动提交或绕过平台机制的代码。
- 对非官方 API 平台，仅提供“打开页面/复制/导出”能力。
- 内置频控，超过每日上限或最短间隔将阻止排期。
- 所有关键操作均记录到本地日志，便于审计。
