using HotelManagement.Api.Configuration;
using HotelManagement.Application.Configuration;
using HotelManagement.Infrastructure.Configuration;
using HotelManagement.Infrastructure.Security;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta configurar ConnectionStrings:DefaultConnection.");

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddApplication();
builder.Services.AddApiErrorHandling();
builder.Services.AddApiAuthentication();
builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "HotelManagement.Api", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Inicia sesión en /api/auth/login y pega solo accessToken."
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        options.OperationFilter<PublicEndpointOperationFilter>();
    });
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDemoAccountsAsync(
        builder.Configuration["DemoAccounts:Password"]);
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // El handler registra solo metadatos seguros; evitar diagnósticos con mensajes sensibles.
    SuppressDiagnosticsCallback = _ => true
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelManagement.Api v1"));
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
