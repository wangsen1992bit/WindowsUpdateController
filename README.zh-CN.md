# WindowsUpdateController

一个极简的 WinForms 小工具,用"官方软禁用"的方式暂停 Windows 更新:只写几个注册表值,不关闭服务、不改计划任务,一键即可还原。

[English documentation](README.md)

## 功能

- **暂停更新** 1–3650 天(质量更新与功能更新一起暂停)
- **放开暂停上限**(`FlightSettingsMaxPauseDays`),避免 Windows 把暂停时间截断在默认的 5 周
- **锁定当前功能更新版本**(`TargetReleaseVersion`),阻止跨大版本升级,同时保留月度安全补丁
- **一键恢复** —— 清除工具写过的全部注册表值
- **状态卡片** —— 已暂停 / 已到期 / 未暂停、截止时间、剩余天数、锁定版本
- **操作日志**,以及直接打开"设置 → Windows 更新"的按钮

## 运行要求

| 项目 | 说明 |
| --- | --- |
| 系统 | Windows 10 / 11(已在 Windows 11 25H2 上实测) |
| 权限 | 管理员 —— 内嵌清单会自动请求提权 |
| 运行时 | .NET Framework 4.x,Windows 自带,无需安装 |
| 编译工具 | 无需额外安装,使用 Windows 自带的 C# 编译器 |

## 编译

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "src\build.ps1"
```

产物:`dist\WindowsUpdateController.exe`(约 17 KB)。

脚本使用 `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319` 下的 `csc.exe`,嵌入 `src\app.manifest`(`requireAdministrator` + 高 DPI),并编译 `src\*.cs` 全部源码。不依赖 NuGet、不依赖 SDK、没有工程文件。

## 使用

1. 双击 `dist\WindowsUpdateController.exe`,同意 UAC 提示。
2. 选择暂停天数(默认 **365** 天);如果不希望跨大版本升级,保持"同时锁定当前版本"勾选。
3. 点击 **暂停并锁定版本**。
4. 到 设置 → Windows 更新 确认,页面应显示更新已暂停。
5. 需要恢复时点击 **恢复更新**,再点"检查更新"。

## 会修改的注册表

所有写入都在 `HKLM` 下,需要管理员权限。

**暂停 —— `HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings`**

| 值名称 | 类型 | 写入内容 |
| --- | --- | --- |
| `PauseFeatureUpdatesStartTime` | REG_SZ | 当前时间(ISO 8601 UTC) |
| `PauseQualityUpdatesStartTime` | REG_SZ | 当前时间 |
| `PauseFeatureUpdatesEndTime` | REG_SZ | 当前时间 + N 天 |
| `PauseQualityUpdatesEndTime` | REG_SZ | 当前时间 + N 天 |
| `PauseUpdatesExpiryTime` | REG_SZ | 当前时间 + N 天 |
| `FlightSettingsMaxPauseDays` | REG_DWORD | N + 30 |

**版本锁定 —— `HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate`**

| 值名称 | 类型 | 写入内容 |
| --- | --- | --- |
| `TargetReleaseVersion` | REG_DWORD | `1` |
| `TargetReleaseVersionInfo` | REG_SZ | 当前 `DisplayVersion`,例如 `25H2` |

"恢复更新"会删除以上全部值。除此之外不做任何改动 —— 不碰服务、不碰计划任务、不创建组策略对象。

## 注意事项

- 暂停期间**收不到月度安全补丁**,请把它当作中短期措施,自行权衡风险。
- 暂停到期后系统会自动恢复更新,状态卡片会显示"已到期"。
- 这是软禁用而非永久屏蔽:功能更新或系统维护若重置了这些注册表值,更新仍可能重新开始。
- `TargetReleaseVersion` 只锁定**功能更新**版本,质量更新照常安装。

## 目录结构

```
WindowsUpdateController/
├─ src/
│  ├─ Program.cs          入口 + 全局异常处理
│  ├─ MainForm.cs         界面(纯代码构建,无 designer 文件)
│  ├─ UpdateManager.cs    全部注册表读写逻辑
│  ├─ app.manifest        requireAdministrator、高 DPI
│  └─ build.ps1           编译到 ../dist
└─ dist/
   └─ WindowsUpdateController.exe
```
