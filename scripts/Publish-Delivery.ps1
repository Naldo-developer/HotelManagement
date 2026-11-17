param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'src/HotelManagement.Api/HotelManagement.Api.csproj'
$destination = [IO.Path]::GetFullPath($OutputDirectory)

& dotnet publish $project --configuration Release --output $destination --nologo
if ($LASTEXITCODE -ne 0) { throw 'Fallo dotnet publish.' }
foreach ($file in @('HotelManagement.Api.dll', 'wwwroot/index.html', 'wwwroot/js/app.js', 'wwwroot/css/styles.css')) {
    if (-not (Test-Path -LiteralPath (Join-Path $destination $file))) {
        throw "Falta un archivo publicado: $file"
    }
}
Write-Output "Publicacion verificada en $destination"
