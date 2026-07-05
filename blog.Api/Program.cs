using blog.Api.Extensions;
using blog.Application;
using blog.Infrastructure;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting();

builder.Services.AddControllers();
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
    app.UseRequestLogging();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseApiCors();

app.UseExceptionMiddleware();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();