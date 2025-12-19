# 安装包占位

建议使用 MSIX 封装本应用：
1. 在 Windows 上打开 `src/ComplianceAssistant.sln`。
2. 新建 Windows 应用打包项目（MSIX），引用 `ComplianceAssistant.App` 输出。
3. 证书签名后生成 .msix 安装包。

若需 Inno Setup，可在此目录添加脚本，仍需保持“人工确认模式”与合规限制的提示文案。
