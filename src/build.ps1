<#
    build.ps1 — 把 src 下的源码编译成 ..\dist\WindowsUpdateController.exe
    用法:
        powershell -NoProfile -ExecutionPolicy Bypass -File ".\src\build.ps1"
    依赖: 只用 Windows 自带的 .NET Framework 4.x 编译器,不需要安装 SDK
#>

$ErrorActionPreference = 'Stop'

$srcDir  = $PSScriptRoot
$rootDir = Split-Path -Parent $srcDir
$distDir = Join-Path $rootDir 'dist'

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw '找不到 csc.exe,系统缺少 .NET Framework 4.x'
}

$sources  = Get-ChildItem -Path $srcDir -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName }
$outExe   = Join-Path $distDir 'WindowsUpdateController.exe'
$manifest = Join-Path $srcDir 'app.manifest'

& $csc /nologo /target:winexe /optimize+ /platform:anycpu `
    "/out:$outExe" `
    "/win32manifest:$manifest" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "编译失败,退出码 $LASTEXITCODE"
}

$fi = Get-Item $outExe
'编译成功: {0}  ({1:N1} KB)' -f $fi.FullName, ($fi.Length / 1KB)
