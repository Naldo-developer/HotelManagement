param(
    [Parameter(Mandatory = $true)][string]$PublishedDirectory,
    [int]$Port = 5086
)

$ErrorActionPreference = 'Stop'
foreach ($name in @('ConnectionStrings__DefaultConnection', 'DELIVERY_USERNAME', 'DELIVERY_PASSWORD')) {
    if (-not [Environment]::GetEnvironmentVariable($name)) { throw "Falta variable $name" }
}
$directory = (Resolve-Path -LiteralPath $PublishedDirectory).Path
$dll = Join-Path $directory 'HotelManagement.Api.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Falta el DLL publicado' }
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousUrls = $env:ASPNETCORE_URLS
$process = $null
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    $process = Start-Process dotnet -ArgumentList ('"{0}"' -f $dll) -WorkingDirectory $directory -WindowStyle Hidden -PassThru
    $base = "http://127.0.0.1:$Port"
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($process.HasExited) { throw 'El proceso publicado termino antes de estar listo' }
        try {
            $health = Invoke-RestMethod "$base/api/health"
            if ($health.status -eq 'ok') { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw 'No responde salud del proceso publicado' }
    foreach ($path in @('/', '/js/app.js', '/css/styles.css')) {
        if ((Invoke-WebRequest "$base$path" -UseBasicParsing).StatusCode -ne 200) { throw "Fallo $path" }
    }
    foreach ($entry in @(@('/swagger',404), @('/api/no-existe',404), @('/api/auth/me',401))) {
        try { $null = Invoke-WebRequest "$base$($entry[0])" -UseBasicParsing; throw "Respuesta inesperada $($entry[0])" }
        catch { if ([int]$_.Exception.Response.StatusCode -ne $entry[1]) { throw } }
    }
    $body = @{ username = $env:DELIVERY_USERNAME; password = $env:DELIVERY_PASSWORD } | ConvertTo-Json
    $null = Invoke-RestMethod "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body $body -SessionVariable session
    $me = Invoke-RestMethod "$base/api/auth/me" -WebSession $session
    if ($me.username -ne $env:DELIVERY_USERNAME) { throw 'Sesion incorrecta' }
    $null = Invoke-RestMethod "$base/api/rooms" -WebSession $session
    $null = Invoke-RestMethod "$base/api/auth/logout" -Method Post -WebSession $session
    try { $null = Invoke-WebRequest "$base/api/auth/me" -WebSession $session -UseBasicParsing; throw 'Logout no cerro sesion' }
    catch { if ([int]$_.Exception.Response.StatusCode -ne 401) { throw } }
    Write-Output 'R06: Production, estaticos, rutas, login, consulta PostgreSQL y logout correctos.'
} finally {
    if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:ASPNETCORE_URLS = $previousUrls
}
