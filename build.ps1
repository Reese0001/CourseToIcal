$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$publish = Join-Path $root 'publish'
$build = Join-Path $root 'build'

function Invoke-Checked([string]$File, [string[]]$Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE" }
}

New-Item -ItemType Directory -Force -Path $publish, $build | Out-Null

$sdkList = @(& dotnet --list-sdks 2>$null)
if ($sdkList.Count -gt 0) {
    Write-Host 'Using .NET SDK build.'
    Invoke-Checked 'dotnet' @('restore', (Join-Path $root 'CourseToIcal.sln'))
    Invoke-Checked 'dotnet' @('build', (Join-Path $root 'CourseToIcal.sln'), '-c', 'Release', '--no-restore', '--nologo')
    Invoke-Checked 'dotnet' @('run', '--project', (Join-Path $root 'tests\CourseToIcal.Tests\CourseToIcal.Tests.csproj'), '-c', 'Release', '--no-build', '--no-restore')
    if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
    New-Item -ItemType Directory -Force -Path $publish | Out-Null
    Invoke-Checked 'dotnet' @('publish', (Join-Path $root 'src\CourseToIcal.App\CourseToIcal.App.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-o', $publish)
    Write-Host "Published to $publish"
    exit 0
}

Write-Host 'No .NET SDK found; using the installed .NET Framework C# compiler for local verification.'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "Could not find legacy C# compiler at $csc" }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$coreDll = Join-Path $build 'CourseToIcal.Core.dll'
$testExe = Join-Path $build 'CourseToIcal.Tests.exe'
$appExe = Join-Path $publish 'CourseToIcal.exe'
$coreSources = Get-ChildItem (Join-Path $root 'src\CourseToIcal.Core') -Filter '*.cs' -Recurse | ForEach-Object FullName
$appSources = Get-ChildItem (Join-Path $root 'src\CourseToIcal.App') -Filter '*.cs' -Recurse | ForEach-Object FullName
$testSources = Get-ChildItem (Join-Path $root 'tests\CourseToIcal.Tests') -Filter '*.cs' -Recurse | ForEach-Object FullName
$commonRefs = @(
    "/reference:$framework\System.dll",
    "/reference:$framework\System.Core.dll",
    "/reference:$framework\System.Xml.dll",
    "/reference:$framework\System.Xml.Linq.dll",
    "/reference:$framework\System.IO.Compression.dll",
    "/reference:$framework\System.IO.Compression.FileSystem.dll",
    "/reference:$framework\System.IO.Compression.ZipFile.dll",
    "/reference:$framework\System.Security.dll"
)
Invoke-Checked $csc (@('/nologo', '/utf8output', '/target:library', "/out:$coreDll") + $commonRefs + $coreSources)
Invoke-Checked $csc (@('/nologo', '/utf8output', '/target:exe', "/out:$testExe", "/reference:$coreDll") + $commonRefs + $testSources)
Invoke-Checked $testExe @()
Invoke-Checked $csc (@('/nologo', '/utf8output', '/target:winexe', '/optimize+', "/out:$appExe", "/reference:$coreDll", "/reference:$framework\System.Drawing.dll", "/reference:$framework\System.Windows.Forms.dll") + $commonRefs + $appSources)
Copy-Item $coreDll (Join-Path $publish 'CourseToIcal.Core.dll') -Force
Write-Host "Published local fallback build to $publish"
