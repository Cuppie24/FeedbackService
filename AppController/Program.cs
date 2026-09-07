using System.Runtime.CompilerServices;
using AppController.DependencyInjection;
using Application.Crypto;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddControllers();

//todo remove
builder.Services.AddScoped<ICryptoService, CryptoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "FeedbackService v1"));
}

app.MapControllers();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


var scope = app.Services.CreateScope();
var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
Console.WriteLine("Enter password");
var inputPassword = Console.ReadLine();
var result = cryptoService.Md5Hash(inputPassword ?? "");
Console.WriteLine($"Password: {result}");

app.Run();