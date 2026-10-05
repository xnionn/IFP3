$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$project = Join-Path $PSScriptRoot 'IFP3.csproj'
$evidence = Join-Path $PSScriptRoot 'evidence'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
& dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
$checks = & dotnet run --project $project -c Release --no-build -- --check 2>&1
$checkExit = $LASTEXITCODE
$checks | Set-Content (Join-Path $evidence 'checks.txt') -Encoding UTF8
$checks | Write-Output
if ($checkExit -ne 0) { throw 'Runtime checks failed.' }

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$probe = Join-Path $tempRoot ('ifp3-guards-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probe | Out-Null
try {
    [xml]$projectXml = Get-Content -LiteralPath $project -Raw
    $target = $projectXml.Project.PropertyGroup.TargetFramework
    $reference = [Security.SecurityElement]::Escape($project)
    $probeProject = Join-Path $probe 'Probe.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>$target</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="$reference" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $probeProject -Encoding UTF8
    $source = Join-Path $probe 'Probe.cs'
    $prefix = "using Bookstore;`r`nvar original = new Book(`"X`", `"x`", 1m, 0);`r`n"
    ($prefix + 'Console.WriteLine(original.StockCount);') | Set-Content $source -Encoding UTF8
    $positive = & dotnet build $probeProject -c Release --nologo 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Positive compile control failed: $positive" }
    $invalid = @'
original.StockCount = -1;
_ = original with { StockCount = -1 };
_ = new Book("X", "x", 1m, 0) { StockCount = -1 };
_ = original with { Title = "Changed" };
_ = original with { Isbn = "changed" };
_ = original with { Price = -1m };
'@
    ($prefix + $invalid) | Set-Content $source -Encoding UTF8
    $diagnostics = & dotnet build $probeProject -c Release --nologo 2>&1
    $negativeExit = $LASTEXITCODE
    $log = @('Positive compile control: PASS', "Invalid caller compiler exit: $negativeExit")
    $log += $diagnostics
    if ($negativeExit -eq 0) { throw 'Invalid public writes unexpectedly compiled.' }
    foreach ($line in 3..8) {
        if (($diagnostics -join "`n") -notmatch "Probe.cs\($line,\d+\): error CS(0272|8852|0200)") {
            throw "Expected an access/readonly error on probe line $line. $diagnostics"
        }
    }
    $log += 'All 6 compile guards passed (expected errors).'
    $log | Set-Content (Join-Path $evidence 'compile-guards.txt') -Encoding UTF8
    Write-Output $log[-1]
}
finally {
    $resolvedProbe = [IO.Path]::GetFullPath($probe)
    if ([IO.Path]::GetDirectoryName($resolvedProbe) -ne $tempRoot -or
        [IO.Path]::GetFileName($resolvedProbe) -notlike 'ifp3-guards-*') {
        throw "Refusing cleanup outside the verification temp directory: $resolvedProbe"
    }
    Remove-Item -LiteralPath $resolvedProbe -Recurse -Force
}
