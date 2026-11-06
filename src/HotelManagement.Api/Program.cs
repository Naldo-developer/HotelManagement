using HotelManagement.Api.Configuration;
using HotelManagement.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddApiErrorHandling();
builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
        options.SwaggerDoc("v1", new() { Title = "HotelManagement.Api", Version = "v1" }));
}

var app = builder.Build();

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

app.MapControllers();

app.Run();
