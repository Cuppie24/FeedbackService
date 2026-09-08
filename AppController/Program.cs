using System.Runtime.CompilerServices;
using AppController.DependencyInjection;
using Application;
using Application.Crypto;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllerServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddControllers();


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
//
// var scope = app.Services.CreateScope();
// var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
// Console.WriteLine("Input login:");
// var username = Console.ReadLine();
// Console.WriteLine("Input password:");
// var password = Console.ReadLine();
// var hash = cryptoService.Md5Hash(password + username?.ToUpper() ?? "");
// Console.WriteLine(hash);

app.Run();