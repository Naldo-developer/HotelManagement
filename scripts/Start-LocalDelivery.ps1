param(
    [Parameter(Mandatory = $true)][string]$PublishedDirectory,
    [ValidateRange(1024,65535)][int]$Port = 5086
)

# Lanzador exclusivo de la entrega local, en la misma cuenta Windows.
$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $PublishedDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $directory 'HotelManagement.Api.dll'))) {
    throw 'Falta HotelManagement.Api.dll en el directorio publicado.'
}
$previousConnection = $env:ConnectionStrings__DefaultConnection
$previousSigningKey = $env:Jwt__SigningKey
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
$previousUrls = $env:ASPNETCORE_URLS
try {
    if (-not $env:ConnectionStrings__DefaultConnection -or -not $env:Jwt__SigningKey) {
        # Production no lee User Secrets. Este lanzador local pasa solo la conexión
        # existente y la clave JWT por entorno, sin copiar secretos al publish.
        $project = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/HotelManagement.Api/HotelManagement.Api.csproj'
        $projectXml = [xml](Get-Content -LiteralPath $project -Raw)
        $secretId = $projectXml.Project.PropertyGroup.UserSecretsId | Where-Object { $_ } | Select-Object -First 1
        $secretPath = Join-Path $env:APPDATA "Microsoft/UserSecrets/$secretId/secrets.json"
        if (-not (Test-Path -LiteralPath $secretPath)) { throw 'Configura ConnectionStrings__DefaultConnection y Jwt__SigningKey o los User Secrets locales.' }
        $settings = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
        if (-not $env:ConnectionStrings__DefaultConnection) {
            $env:ConnectionStrings__DefaultConnection = $settings.'ConnectionStrings:DefaultConnection'
        }
        if (-not $env:Jwt__SigningKey) { $env:Jwt__SigningKey = $settings.'Jwt:SigningKey' }
    }
    if (-not $env:ConnectionStrings__DefaultConnection) { throw 'Falta la conexion local.' }
    if (-not $env:Jwt__SigningKey) { throw 'Falta Jwt__SigningKey.' }
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:DOTNET_ENVIRONMENT = 'Production'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    Write-Output "Entrega local: http://127.0.0.1:$Port (Ctrl+C para detener)"
    Push-Location -LiteralPath $directory
    try {
        & dotnet './HotelManagement.Api.dll'
        if ($LASTEXITCODE -ne 0) { throw 'La entrega local termino con error.' }
    } finally { Pop-Location }
} finally {
    $env:ConnectionStrings__DefaultConnection = $previousConnection
    $env:Jwt__SigningKey = $previousSigningKey
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
    $env:ASPNETCORE_URLS = $previousUrls
}
