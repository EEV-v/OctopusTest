[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target = "Default",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Normal",
    [string]$NuGetFeeds = "",
    [string]$DockerSource = "",
    [Parameter(Position=0, Mandatory=$false, ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip {
    Param(
        [string]$zipfile,
        [string]$outpath
    )

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function EscapeParameter {
    Param (
        [string]$parameter
    )
    $index = $parameter.IndexOf('=')

    return "$($parameter.Substring(0, $index))=`"$($parameter.Substring($index + 1))`""
}

$IsRunningOnLinux = $IsLinux -eq $true #IsLinux exists only when running on PowerShell Core, classic PowerShell will return $null

Write-Output "Preparing to run build script."

if ($DockerSource -eq "") {
    $DockerSource = $env:R1_DOCKER_REGISTRY
}

$CAKE_ROOT = $pwd
$CAKE_CACHE_ROOT = $CAKE_ROOT
$TOOLS_DIR = Join-Path $CAKE_CACHE_ROOT "tools"
$CAKE_VERSION = Get-Content $(Join-Path $pwd "cake.version")
$CAKE_PACKAGE = "Cake.CoreCLR"
$CAKE_DIRECTORY = Join-Path $TOOLS_DIR $CAKE_PACKAGE
$CAKE_EXE = Join-Path $CAKE_DIRECTORY "Cake.dll"

$ADDINS_DIR = Join-Path $TOOLS_DIR "Addins"
$MODULES_DIR = Join-Path $TOOLS_DIR "Modules"

if (-not (Test-Path $TOOLS_DIR)) {
    New-Item -Path $TOOLS_DIR -Type Directory | Out-Null
}

if (-not (Test-Path $CAKE_EXE)) {
    Push-Location
    Set-Location $CAKE_ROOT

    if ([System.String]::IsNullOrWhiteSpace($NuGetFeeds)) {
        $nugetConfigPath = "$([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ApplicationData))/NuGet/NuGet.Config"
        $feeds = Select-Xml -Path $nugetConfigPath -XPath "/configuration/packageSources/add" | Select-Object -ExpandProperty Node | Where-Object -Property protocolVersion -ne "3" | Select-Object -ExpandProperty value
    } else {
        $feeds = $NuGetFeeds.Split(';')
    }

    $cakeZipPath = Join-Path $TOOLS_DIR "$CAKE_PACKAGE$CAKE_VERSION.nupkg"

    foreach ($feed in $feeds) {
        $url = "$($feed.TrimEnd('/'))/package/$CAKE_PACKAGE/$CAKE_VERSION"
        try {
            (New-Object System.Net.WebClient).DownloadFile($url, $cakeZipPath)
            break
        }
        catch {
        }
    }

    if (-not (Test-Path $cakeZipPath)) {
        Write-Error "Can't restore Cake using NuGet."
        exit 42
    } else {
        try {
            Unzip $cakeZipPath $CAKE_DIRECTORY
        } finally {
            $cakeZipPath | Remove-Item -Force | Out-Null
        }
    }

    Pop-Location
}

$cakeArguments = @("`"$Script`"")
$cakeArguments += "-target=`"$Target`""
$cakeArguments += "-configuration=`"$Configuration`""
$cakeArguments += "-verbosity=`"$Verbosity`""

$customDockerSource = $false
$dockerSourcePrefix = "-docker_source="
$previous = ""
$current = ""
foreach ($item in $ScriptArgs) {
    $current = $item
    if ($item.StartsWith("-") -and $previous -ne "") {
        $tmp = $previous
        $previous = $item
        $item = $tmp
    } else {
        if (-not $item.StartsWith("-")) {
            $item = "$($previous):$item"
            $previous = ""
        } else {
            $previous = $item
            continue
        }
    }

    if ($item.StartsWith($dockerSourcePrefix)) {
        $customDockerSource = $true
    }

    $cakeArguments += EscapeParameter -parameter $item
}

if ($previous -ne "") {
    if ($previous.StartsWith($dockerSourcePrefix)) {
        $customDockerSource = $true
    }

    $cakeArguments += EscapeParameter -parameter $current
}

if (-not $customDockerSource) {
    $cakeArguments += "$dockerSourcePrefix`"$DockerSource`""
}

$cakeArguments += "--settings_skipverification=true"
$cakeArguments += "--nuget_useinprocessclient=true"
$cakeArguments += "--nuget_loaddependencies=false"
$cakeArguments += "--paths_tools=`"$TOOLS_DIR`""
$cakeArguments += "--paths_addins=`"$ADDINS_DIR`""
$cakeArguments += "--paths_modules=`"$MODULES_DIR`""
if (-not ([System.String]::IsNullOrWhiteSpace($NuGetFeeds))) { $cakeArguments += "--nuget_source=`"$NuGetFeeds`"" }
Write-Output "Running build script's $Script $Target target with $Configuration configuration and $Verbosity logging."
if ($Verbosity -eq "Diagnostic") {
    Write-Output "dotnet $CAKE_EXE $cakeArguments"
}

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
}
else {
    Write-Error "dotnet executable is not found."
    exit -1
}

&dotnet $CAKE_EXE $cakeArguments
exit $LASTEXITCODE
