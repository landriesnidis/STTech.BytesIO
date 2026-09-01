#Requires -Version 5.1
<#
.SYNOPSIS
    编译 STTech.BytesIO 库的 Release 版本 NuGet 包到 Install 文件夹。

.DESCRIPTION
    编译 STTech.BytesIO 相关主库项目（测试、示例等项目不参与打包）。
    生成的 .nupkg 输出到仓库根目录下的 Install 文件夹。
    该文件夹已加入 .gitignore，不会被上传到 Git。

.EXAMPLE
    .\build-release.ps1
    .\build-release.ps1 -OutputDir "D:\MyPackages"
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# 脚本所在目录作为仓库根目录
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

# 待打包的项目列表
$projects = @(
    (Join-Path $repoRoot 'STTech.BytesIO\STTech.BytesIO.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.Modbus\STTech.BytesIO.Modbus.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.P2P\STTech.BytesIO.P2P.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.Quic\STTech.BytesIO.Quic.csproj'),
    (Join-Path $repoRoot 'STTech.BytesIO.SerialPortStream\STTech.BytesIO.SerialPortStream.csproj')
)

# 如果未指定输出目录，默认输出到仓库根目录下的 Install 文件夹
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $outDir = Join-Path $repoRoot 'Install'
} else {
    if ([System.IO.Path]::IsPathRooted($OutputDir)) {
        $outDir = $OutputDir
    } else {
        $outDir = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDir))
    }
}

# 检查 dotnet 命令是否可用
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw '未找到 dotnet 命令，请先安装 .NET SDK（https://dotnet.microsoft.com/download）'
}

# 检查所有项目文件是否存在
foreach ($proj in $projects) {
    if (-not (Test-Path -LiteralPath $proj)) {
        throw "未找到项目文件：$proj"
    }
}

# 创建输出目录
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# 清理旧包，避免遗留不同版本的包造成混淆
Get-ChildItem -Path $outDir -Filter '*.nupkg' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

Write-Host "==> 开始编译 STTech.BytesIO Release 版本 NuGet 包 ..." -ForegroundColor Cyan
Write-Host "    输出目录: $outDir" -ForegroundColor Gray

foreach ($proj in $projects) {
    Write-Host "
--> 打包项目: $proj" -ForegroundColor Cyan
    # 编译并打包（传递 -p:GeneratePackageOnBuild=false 避免多目标框架下引发 NU5026 时序冲突）
    & $dotnet.Path pack $proj -c Release -o $outDir --nologo -p:GeneratePackageOnBuild=false
    if ($LASTEXITCODE -ne 0) {
        throw "项目 $proj 打包失败，退出码：$LASTEXITCODE"
    }
}

# 列出生成的包
$packages = @(Get-ChildItem -Path $outDir -Filter '*.nupkg' -File)
if ($packages.Count -eq 0) {
    throw '打包完成但未找到生成的 .nupkg 文件'
}

Write-Host "
==> 打包成功，共生成 $($packages.Count) 个 NuGet 包：" -ForegroundColor Green
$packages | ForEach-Object {
    $sizeKB = [Math]::Round($_.Length / 1KB, 1)
    Write-Host ("    {0}  ({1} KB)" -f $_.Name, $sizeKB) -ForegroundColor Green
}

Write-Host "
Install 文件夹已加入 .gitignore，不会上传到 Git。" -ForegroundColor DarkGray