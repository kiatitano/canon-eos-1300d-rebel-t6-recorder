$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Build.ps1')
$framework = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET\Framework\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:'+(Join-Path $framework $_) }
$app = Join-Path $root 'dist\EOS1300DRecorder.exe'
$tests = @(
    @{ Name='BeeperTests'; Sources=@('tests\BeeperTests.cs','src\Services\BatteryBeeper.cs') },
    @{ Name='TransferVerificationTests'; Sources=@('tests\TransferVerificationTests.cs','src\Services\MovieTransfer.cs','src\Services\AppPreferences.cs','src\Interop\CameraSdk.cs','src\Interop\TransferSdk.cs','src\Models\RecordedTake.cs') },
    @{ Name='UiTests'; Sources=@('tests\UiTests.cs'); App=$true }
)
foreach ($test in $tests) {
    $output = Join-Path $root ('dist\'+$test.Name+'.exe')
    $arguments = @('/nologo','/target:exe','/platform:x86','/utf8output',('/out:'+$output)) + $references
    if ($test.App) { $arguments += '/reference:'+$app }
    $arguments += $test.Sources | ForEach-Object { Join-Path $root $_ }
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw "Could not compile $($test.Name)" }
    & $output
    if ($LASTEXITCODE -ne 0) { throw "$($test.Name) failed" }
}
& (Join-Path $root 'dist\UiTests.exe') --render-fa
if ($LASTEXITCODE -ne 0) { throw 'Persian screenshot render failed' }
Write-Output 'All offline tests passed. No camera access or source deletion was performed.'
