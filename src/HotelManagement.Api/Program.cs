using HotelManagement.Application.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
        options.SwaggerDoc("v1", new() { Title = "HotelManagement.Api", Version = "v1" }));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelManagement.Api v1"));
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
