using AppController;
using Application;
using Infrastructure;
using DependencyInjection = AppController.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllerServices();
builder.Services.AddApplicationServices();
builder.Services.AddCorsServices(builder.Configuration);
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
app.UseCors(DependencyInjection.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.Run();