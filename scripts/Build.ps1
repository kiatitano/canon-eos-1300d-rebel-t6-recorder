param([switch]$Package)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'dist'
$obj = Join-Path $root 'obj'
New-Item -ItemType Directory -Path $output, $obj -Force | Out-Null
$framework = Join-Path ([Environment]::GetFolderPath('Windows')) 'Microsoft.NET\Framework\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found. Install .NET Framework 4.8.' }

# Original app icon, generated locally; no Canon artwork or proprietary binaries.
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object Drawing.Bitmap 64,64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::FromArgb(28,31,35))
$pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(244,243,238)),3
$brush = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(235,98,88))
$graphics.DrawRectangle($pen,11,20,42,29)
$graphics.FillEllipse($brush,24,27,17,17)
$graphics.FillRectangle($brush,17,14,15,7)
$icon = [Drawing.Icon]::FromHandle($bitmap.GetHicon())
$iconPath = Join-Path $obj 'App.ico'
$stream = [IO.File]::Create($iconPath)
try { $icon.Save($stream) } finally { $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $pen.Dispose(); $brush.Dispose() }

$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll','System.Windows.Forms.dll','Microsoft.CSharp.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' -Recurse | Sort-Object FullName | ForEach-Object FullName
$arguments = @('/nologo','/target:winexe','/platform:x86','/optimize+','/utf8output',('/out:'+(Join-Path $output 'EOS1300DRecorder.exe')),('/win32icon:'+$iconPath),('/win32manifest:'+(Join-Path $root 'src\app.manifest')),('/resource:'+(Join-Path $root 'src\Views\MainWindow.xaml')+',MainWindow.xaml'),('/resource:'+(Join-Path $root 'src\Localization\fa.json')+',fa.json'),('/resource:'+$iconPath+',App.ico')) + $references + $sources
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath (Join-Path $root 'src\app.config') -Destination (Join-Path $output 'EOS1300DRecorder.exe.config')
foreach ($name in @('README.md','LICENSE','NOTICE.md')) { if (Test-Path -LiteralPath (Join-Path $root $name)) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $output } }
Write-Output (Join-Path $output 'EOS1300DRecorder.exe')
if ($Package) {
    $archive = Join-Path $root 'EOS1300DRecorder-1.1.1-win-x86.zip'
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
    Compress-Archive -Path (Join-Path $output 'EOS1300DRecorder.exe'),(Join-Path $output 'EOS1300DRecorder.exe.config'),(Join-Path $root 'README.md'),(Join-Path $root 'LICENSE'),(Join-Path $root 'NOTICE.md') -DestinationPath $archive
    Get-FileHash -LiteralPath $archive -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_.Path))" } | Set-Content -LiteralPath (Join-Path $root 'SHA256SUMS.txt') -Encoding ASCII
    Write-Output $archive
}
