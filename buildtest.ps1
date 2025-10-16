Function BuildTestDefaultSetup{
	Write-Host "##########################################################################"
	Write-Host "#"
	Write-Host "#  buildtest default setup"
	Write-Host "#"
	Write-Host "##########################################################################"

	$command       = @("build", "publish")
	$configuration = @("Debug", "Release")
	$framework     = @("net462", "net472", "netcoreapp3.1", "net9.0")
	$runtime       = @("", "win-x64", "win-x86")

	if( Test-Path "./build" ){
		Remove-Item "./build" -Recurse -Force
	}

	foreach($cmd in $command){
	foreach($c in $configuration){
	foreach($f in $framework){
	foreach($r in $runtime){
		$rid = ""
		if($r -ne ""){
			$rid = "-r"
		}
		Write-Host "+-------------------------------------------------------------------------"
		Write-Host "|  cmd:$cmd configuration:$c, framework:$f, runtime:$r"
		Write-Host "+-------------------------------------------------------------------------"

		$outputdir= "./build/$c/$cmd/$f-$r"

		dotnet $cmd   ConsoleApp/ConsoleApp.csproj -c $c -f $f $rid $r -o $outputdir
		if(-not $? ){
			exit -1
		}

		&"$outputdir/ConsoleApp.exe" "$outputdir/BouncingBeachBallWebP.webp" frame 5
		if(-not $?){
			exit -1
		}

		# check output file
		$hasOldAsmDir = Test-Path "$outputdir/x64/libsharpyuv.dll"
		$hasUnflatDir = Test-Path "$outputdir/runtimes/win-x64/native/libsharpyuv.dll"
		if ($f -eq "net462" -or $f -eq "net472"){
			if(-not $hasOldAsmDir){
				Write-Host "Error: x64/libsharpyuv.dll not found"
				exit -1
			}
			if($hasUnflatDir){
				Write-Host "Error: runtimes/win-x64/libsharpyuv.dll found"
				exit -1
			}
		}
		else{
			if($r -eq ""){
				if(-not $hasUnflatDir){
					Write-Host "Error: runtimes/win-x64/libsharpyuv.dll not found"
					exit -1
				}
			}
			else{
				if($hasUnflatDir){
					Write-Host "Error: runtimes/win-x64/libsharpyuv.dll found"
					exit -1
				}
			}
		}
	}}}}
}

Function BuildTestLibFolderSpecified{
	Write-Host "##########################################################################"
	Write-Host "#"
	Write-Host "#  buildtest with native library folder specified"
	Write-Host "#"
	Write-Host "##########################################################################"

	$command       = @("build", "publish")
	$configuration = @("Debug", "Release")
	$framework     = @("net462", "net472", "netcoreapp3.1", "net9.0")
	$runtime       = @("", "win-x64", "win-x86")

	if( Test-Path "./build" ){
		Remove-Item "./build" -Recurse -Force
	}

	foreach($cmd in $command){
	foreach($c in $configuration){
	foreach($f in $framework){
	foreach($r in $runtime){
		$rid = ""
		if($r -ne ""){
			$rid = "-r"
		}
		Write-Host "+-------------------------------------------------------------------------"
		Write-Host "|  cmd:$cmd configuration:$c, framework:$f, runtime:$r"
		Write-Host "+-------------------------------------------------------------------------"

		$outputdir= "./build/$c/$cmd/$f-$r"

		dotnet $cmd   ConsoleApp/ConsoleAppCustomLibDir.csproj -c $c -f $f $rid $r -o $outputdir
		if(-not $? ){
			exit -1
		}

		&"$outputdir/ConsoleAppCustomLibDir.exe" "$outputdir/BouncingBeachBallWebP.webp" frame 5
		if(-not $? ){
			exit -1
		}
	}}}}
}

Function BuildTestAOT{
	Write-Host "##########################################################################"
	Write-Host "#"
	Write-Host "#  buildtest with AOT"
	Write-Host "#"
	Write-Host "##########################################################################"


	$configuration = @("Debug", "Release")
	$runtime       = @("win-x64", "win-x86")

	if( Test-Path "./build" ){
		Remove-Item "./build" -Recurse -Force
	}

	foreach($c in $configuration){
	foreach($r in $runtime){
		Write-Host "+-------------------------------------------------------------------------"
		Write-Host "|  cmd:publish-aot configuration:$c, runtime:$r"
		Write-Host "+-------------------------------------------------------------------------"

		$outputdir= "./build/$c/publish-aot/$r"

		dotnet publish  ConsoleApp/ConsoleAppAot.csproj -f net9.0-windows -c $c -r $r -o $outputdir
		if(-not $? ){
			exit -1
		}

		&"$outputdir/ConsoleAppAot.exe" "$outputdir/BouncingBeachBallWebP.webp" frame 5
		if(-not $? ){
			exit -1
		}

		# check output file
		$hasUnflatDir = Test-Path "$outputdir/runtimes/win-x64/native/libsharpyuv.dll"
		if($hasUnflatDir){
			Write-Host "Error: runtimes/win-x64/libsharpyuv.dll found"
			exit -1
		}
	}}
}


BuildTestDefaultSetup
BuildTestLibFolderSpecified
BuildTestAOT