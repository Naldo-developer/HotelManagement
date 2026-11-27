using HotelManagement.Api.Configuration;
using HotelManagement.Application;
using HotelManagement.Infrastructure;
using HotelManagement.Infrastructure.Data.Seeders;

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
    builder.Services.AddApiSwagger();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDemoAccountsAsync(
        builder.Configuration["DemoAccounts:Password"]);
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = _ => true
});

if (app.Environment.IsDevelopment())
{
    app.UseApiSwagger();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();