$path = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $path

# read current version
[xml]$props = Get-Content "./AnimatedImage.props"
$version = $props.Project.PropertyGroup.MyProjectVersion

# create sub version for test
$dist    = [System.DateTime]::Now - [System.DateTime]::new(2020, 1, 1)
$distTxt = $dist.TotalSeconds.ToString("0000000000")
$testVersion = "$version-test$distTxt"

# package
$outputDir=".\test_build"
if (-not (Test-Path $outputDir)) {
    mkdir $dirPath
} 

$projects=@("AnimatedImage", "AnimatedImage.Wpf", "AnimatedImage.Avalonia")
foreach($project in $projects){
    $projectFile=[System.IO.Path]::Combine($project, $project + ".csproj")
    dotnet pack $projectFile -c Debug -o $outputDir -p:Version=$testVersion
}

