$cakeVersion = Get-Content $(Join-Path $pwd "cake.version")
$toolsDirectory = "tools"

function InitializeDeployScript {
    Param (
        [String]$ProjectPath
    )

    dotnet ./$toolsDirectory/Cake.CoreCLR/Cake.dll ./$ProjectPath/Initialize/Initialize.cake -target=Cache | Out-Null
    Move-Item ./$ProjectPath/Initialize/$toolsDirectory\* ./$toolsDirectory -Force -ErrorAction SilentlyContinue | Out-Null
    Remove-Item ./$ProjectPath/Initialize/$toolsDirectory -Recurse -Force | Out-Null
}

Find-Package -ProviderName NuGet -Name "Cake" -RequiredVersion $cakeVersion | Install-Package -ExcludeVersion -Destination $toolsDirectory -Force | Out-Null
Find-Package -ProviderName NuGet -Name "Cake.CoreCLR" -RequiredVersion $cakeVersion | Install-Package -ExcludeVersion -Destination $toolsDirectory -Force | Out-Null
Find-Package -ProviderName NuGet -Name "Cake.Bakery" -RequiredVersion "0.3.0" | Install-Package -ExcludeVersion -Destination $toolsDirectory -Force | Out-Null

.\build.ps1 -Target Cache | Out-Null
InitializeDeployScript -ProjectPath "OctopusTest.Host"
