Param(
    [String]$encodedArgs = ""
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip {
    Param(
        [string]$zipfile,
        [string]$outpath
    )

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

$DeployJson = "deploy.json"
$BuildJson = "build.json"

$argsExists = $($encodedArgs -ne $null -and $encodedArgs -ne "")

if (-not $argsExists -and -not (Test-Path $DeployJson)) {
    Write-Error "encodedArgs argument is missing and no $DeployJson found."
    exit 1
}

if (-not (Test-Path $BuildJson)) {
    Write-Error "$BuildJson is missing."
    exit 1
}

$buildJsonContent = Get-Content $BuildJson -Raw | ConvertFrom-Json

$jsonString = "{}"
if ($argsExists) {
    $decodedBytes = [System.Convert]::FromBase64String($encodedArgs)
    $jsonString = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
    $jsonString | Out-File -FilePath $DeployJson -Encoding utf8
} else {
    $jsonString = Get-Content $DeployJson -Raw
}

$Json = ConvertFrom-Json $jsonString

$variables = @{}
foreach($variable in $Json.PSObject.Properties) {
    $variables.add($variable.Name, $variable.Value)
}

$IsRunningOnLinux = $IsLinux -eq $true #IsLinux exists only when running on PowerShell Core, classic PowerShell will return $null

Write-Output "Preparing to run deploy script"

$Verbosity = "Normal"
if ($variables.ContainsKey("Cake:Verbosity")) {
    $Verbosity = $variables["Cake:Verbosity"]
}

$NuGetFeeds = [string]::Join(";", $buildJsonContent.NuGetFeeds)

$CAKE_ROOT = $home
if ([string]::IsNullOrWhiteSpace($CAKE_ROOT)) {
    if ($IsRunningOnLinux) {
        $CAKE_ROOT = $env:HOME
    }
    else {
        $CAKE_ROOT = $env:USERPROFILE
    }
}

$CAKE_CACHE_ROOT =  Join-Path $CAKE_ROOT ".cake"
$TOOLS_DIR = Join-Path $CAKE_CACHE_ROOT "tools"
$CAKE_VERSION = $buildJsonContent.CakeVersion
$CAKE_PACKAGE = "Cake.CoreCLR"
$CAKE_DIRECTORY = Join-Path $CAKE_CACHE_ROOT "$CAKE_PACKAGE/$CAKE_VERSION"
$CAKE_EXE = Join-Path $CAKE_DIRECTORY "Cake.dll"

$ADDINS_DIR = Join-Path $TOOLS_DIR "Addins"
$MODULES_DIR = Join-Path $TOOLS_DIR "Modules"

if (-not (Test-Path $CAKE_CACHE_ROOT)) {
    New-Item -Path $CAKE_CACHE_ROOT -Type Directory | Out-Null
}

if (-not (Test-Path $CAKE_EXE)) {
    Push-Location
    Set-Location $CAKE_CACHE_ROOT

    if ([System.String]::IsNullOrWhiteSpace($NuGetFeeds)) {
        $nugetConfigPath = "$([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ApplicationData))/NuGet/NuGet.Config"
        $feeds = Select-Xml -Path $nugetConfigPath -XPath "/configuration/packageSources/add" | Select-Object -ExpandProperty Node | Where-Object -Property protocolVersion -ne "3" | Select-Object -ExpandProperty value
    } else {
        $feeds = $NuGetFeeds.Split(';')
    }

    $cakeZipPath = Join-Path $CAKE_CACHE_ROOT "$CAKE_PACKAGE$CAKE_VERSION.nupkg"

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

$cakeArguments = @("`"Initialize.cake`"")
$cakeArguments += "-target=`"Initialize`""
$cakeArguments += "-verbosity=`"$Verbosity`""
$cakeArguments += "--settings_skipverification=true"
$cakeArguments += "--nuget_useinprocessclient=true"
$cakeArguments += "--nuget_loaddependencies=false"
$cakeArguments += "--paths_tools=`"$TOOLS_DIR`""
$cakeArguments += "--paths_addins=`"$ADDINS_DIR`""
$cakeArguments += "--paths_modules=`"$MODULES_DIR`""
if (-not ([System.String]::IsNullOrWhiteSpace($NuGetFeeds))) { $cakeArguments += "--nuget_source=`"$NuGetFeeds`"" }
Write-Output "Running deploy script's Initialize.cake Initialize target with $Verbosity logging."
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
