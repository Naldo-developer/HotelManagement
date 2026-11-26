param(
    [Parameter(Mandatory = $true)][string]$PublishedDirectory,
    [ValidateRange(1024,65535)][int]$Port = 5086,
    [switch]$RequireDemoData
)

$ErrorActionPreference = 'Stop'
foreach ($name in @('ConnectionStrings__DefaultConnection', 'DELIVERY_USERNAME', 'DELIVERY_PASSWORD')) {
    if (-not [Environment]::GetEnvironmentVariable($name)) { throw "Falta variable $name" }
}
$directory = (Resolve-Path -LiteralPath $PublishedDirectory).Path
$dll = Join-Path $directory 'HotelManagement.Api.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Falta el DLL publicado' }
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
    throw 'El puerto de prueba ya esta ocupado. Elige otro puerto.'
}
foreach ($configFile in Get-ChildItem -LiteralPath $directory -Filter 'appsettings*.json') {
    $config = Get-Content -LiteralPath $configFile.FullName -Raw | ConvertFrom-Json
    if ($config.ConnectionStrings.DefaultConnection -or $config.DemoAccounts.Password) {
        throw 'El publish contiene conexion o password demo: configura esos valores externamente.'
    }
}
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
$previousUrls = $env:ASPNETCORE_URLS
$process = $null
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:DOTNET_ENVIRONMENT = 'Production'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
    $process = Start-Process dotnet -ArgumentList ('"{0}"' -f $dll) -WorkingDirectory $directory -WindowStyle Hidden -PassThru
    $base = "http://127.0.0.1:$Port"
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($process.HasExited) { throw 'El proceso publicado termino antes de estar listo' }
        try {
            $health = Invoke-RestMethod "$base/api/health" -TimeoutSec 3
            if ($health.status -eq 'ok') { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw 'No responde salud del proceso publicado' }
    foreach ($path in @('/', '/js/app.js', '/css/styles.css')) {
        if ((Invoke-WebRequest "$base$path" -UseBasicParsing).StatusCode -ne 200) { throw "Fallo $path" }
    }
    # Verifica también los recursos de marca referenciados por el HTML publicado.
    $html = (Invoke-WebRequest "$base/" -UseBasicParsing).Content
    foreach ($match in [regex]::Matches($html, '(?:src|href)="((?:images|css|js)/[^" ]+)"')) {
        $null = Invoke-WebRequest "$base/$($match.Groups[1].Value)" -UseBasicParsing -TimeoutSec 5
    }
    foreach ($entry in @(@('/swagger',404), @('/api/no-existe',404), @('/api/auth/me',401))) {
        try { $null = Invoke-WebRequest "$base$($entry[0])" -UseBasicParsing; throw "Respuesta inesperada $($entry[0])" }
        catch { if ([int]$_.Exception.Response.StatusCode -ne $entry[1]) { throw } }
    }
    $body = @{ username = $env:DELIVERY_USERNAME; password = $env:DELIVERY_PASSWORD } | ConvertTo-Json
    $invalidBody = @{ username = $env:DELIVERY_USERNAME; password = [guid]::NewGuid().ToString('N') } | ConvertTo-Json
    try { $null = Invoke-WebRequest "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body $invalidBody -UseBasicParsing; throw 'Login invalido aceptado' }
    catch { if ([int]$_.Exception.Response.StatusCode -ne 401) { throw } }
    $null = Invoke-RestMethod "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body $body -SessionVariable session
    $me = Invoke-RestMethod "$base/api/auth/me" -WebSession $session
    if ($me.username -ne $env:DELIVERY_USERNAME) { throw 'Sesion incorrecta' }
    $rooms = Invoke-RestMethod "$base/api/rooms" -WebSession $session
    $guests = Invoke-RestMethod "$base/api/guests" -WebSession $session
    if ($RequireDemoData) {
        if ('DEMO-101' -notin $rooms.roomNumber) { throw 'No aparecen las habitaciones ficticias de R01.' }
        if ('ana@example.invalid' -notin $guests.email) { throw 'No aparecen los huespedes ficticios de R01.' }
    }
    # El reinicio debe conservar la cookie con las claves del mismo usuario Windows.
    Stop-Process -Id $process.Id
    $process.WaitForExit()
    $process = Start-Process dotnet -ArgumentList ('"{0}"' -f $dll) -WorkingDirectory $directory -WindowStyle Hidden -PassThru
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($process.HasExited) { throw 'Fallo al reiniciar la entrega' }
        try {
            $null = Invoke-RestMethod "$base/api/health" -TimeoutSec 3
            $ready = $true
            break
        } catch { Start-Sleep -Milliseconds 250 }
    }
    if (-not $ready) { throw 'No responde despues del reinicio' }
    $me = Invoke-RestMethod "$base/api/auth/me" -WebSession $session
    if ($me.username -ne $env:DELIVERY_USERNAME) { throw 'La sesion no sobrevivio al reinicio' }
    $null = Invoke-RestMethod "$base/api/auth/logout" -Method Post -WebSession $session
    try { $null = Invoke-WebRequest "$base/api/auth/me" -WebSession $session -UseBasicParsing; throw 'Logout no cerro sesion' }
    catch { if ([int]$_.Exception.Response.StatusCode -ne 401) { throw } }
    Write-Output 'R06: Production, estaticos y marca, rutas 401/404, login valido/invalido, PostgreSQL, reinicio con cookie y logout correctos.'
} finally {
    if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit() }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
    $env:ASPNETCORE_URLS = $previousUrls
}
