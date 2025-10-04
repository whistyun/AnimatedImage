Param($BuildFlg)
$path = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $path

# mutex
$createdNew=$false
$mutex = [System.Threading.Mutex]::new($false, "global_build_nativenupkg", [ref]$createdNew)
if (-not $createdNew) {
    echo "build-natvenupkg: running dupplication"
    exit
}

$projName="AnimatedImage.Native"

# read current version
$props = [xml](Get-Content "./AnimatedImage.props")
$version  = $props.SelectNodes("//MyProjectVersion")[0].InnerText.Trim()
$repoFolder = $props.SelectNodes("//TestPackageFolderName")[0].InnerText.Trim()

# create version text
if($BuildFlg -eq "test"){
    $dist    = [System.DateTime]::Now - [System.DateTime]::new(2020, 1, 1)
    $distTxt = $dist.TotalSeconds.ToString("0000000000")

    $versionText  = "$version-alpha$distTxt"
    $anotherPacks = "$repoFolder\$projName.$version.nupkg"
}
else{
    $versionText  = $version
    $anotherPacks = "$repoFolder\$projName.$version.nupkg"
}

echo "build-natvenupkg: check pack exists $anotherPacks"
if( Test-Path $anotherPacks ){
    echo "build-natvenupkg: '$anotherPacks' already exists"
    exit
}

echo "build-natvenupkg: packaging  >>>> $repoFolder"
nuget pack "${projName}\${projName}.nuspec" `
           -OutputDirectory $repoFolder     `
           -version         $versionText 
