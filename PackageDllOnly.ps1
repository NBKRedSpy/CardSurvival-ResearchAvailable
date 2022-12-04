# Get the file name from the C# project name
$ProjectName = Get-Item ./src/*.csproj | % {$_.Name -replace $_.Extension}

Remove-Item -Force -Recurse ./Package -ErrorAction SilentlyContinue

$PackageFolder = "./Package/$ProjectName"
mkdir $PackageFolder | Out-Null

$Dll = Get-ChildItem -Recurse "src\bin\Release\*\$ProjectName.dll"

Copy-Item $Dll -Destination ./$PackageFolder
copy-Item ./src/ResearchIcon.png ./$PackageFolder
Compress-Archive -Path ./$PackageFolder -Force -DestinationPath ./$ProjectName.zip
