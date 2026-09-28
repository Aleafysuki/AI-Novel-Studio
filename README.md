# AI Novel Writer

基于 **WinUI 3 + .NET 10** 的 Windows 桌面 AI 小说创作工具，提供大纲 cascade 编辑、章节写作、AI 辅助续写、差异对比、素材管理、世界观设定等能力。

## 功能特性

- **大纲 Cascade 编辑**：卷 → 章 → 节的多级大纲树，支持级联冲突检测（CascadeConflict）
- **AI 辅助写作**：内置 AIService，支持参与度调节（AIParticipation）、AI 历史记录与回滚
- **差异对比**：DiffService 生成 DiffResult，直观查看 AI 修改与手写差异
- **素材管理**：MaterialDocument 素材库 + TextAnalysisService 文本分析
- **世界观与角色**：Character / Location / WorldSetting 结构化设定，支持联网检索补充（WebSearchService）
- **自动保存**：AutoSaveService 后台定时保存，ProjectService 管理工程文件
- **设置持久化**：AppSettings / SettingsService，API 密钥等敏感配置仅保存在本机，不写入源码

## 技术栈

| 项目 | 说明 |
|------|------|
| UI 框架 | WinUI 3（Windows App SDK） |
| 运行时 | .NET 10（自包含发布，unpackaged 独立 exe 或 MSIX） |
| 平台 | Windows 10 1809 (17763) 及以上，x86 / x64 / ARM64 |

## 构建与运行

```powershell
# 还原并编译（默认 x64）
dotnet build -c Release

# 发布为自包含独立 exe（免安装，输出在 bin 目录）
dotnet publish -c Release -p:Platform=x64

# 如需 MSIX 安装包，需自备自签名证书：
# 1. 生成 AINovelWriter_TemporaryKey.pfx 并放到项目根目录
# 2. 通过命令行传入密码，不要写进源码
dotnet publish -p:PublishProfile=msix-x64.pubxml -c Release -p:Platform=x64 -p:PackageCertificatePassword=你的密码
```

> 证书密码不写入仓库。仓库中不包含 .pfx / .cer 证书文件，请自行生成自签名证书。

## 目录结构

```
AI_Novel_Writer/
├── Controls/      # 自定义控件
├── Converters/    # XAML 值转换器
├── Helpers/       # 辅助工具类
├── Models/        # 数据模型（章节、角色、大纲、设置等）
├── Services/      # 业务服务（AI、保存、差异、素材、检索等）
├── ViewModels/    # MVVM 视图模型
├── Views/         # XAML 页面
└── Properties/    # 清单与发布配置
```

## 隐私与安全

- 所有工程数据保存在本地
- AI / 联网检索所需的 API 密钥通过应用内设置录入，仅存于本机用户配置，源码中无任何硬编码密钥

## 许可证

本项目基于 [MIT License](LICENSE) 开源。
