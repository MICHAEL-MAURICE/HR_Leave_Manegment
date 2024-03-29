

using HR.LeaveManagement.Api.Middleware;
using HR.LeaveManagement.Identity;
using HR.LeaveManagement.Infrastructure;
using HrManegment.Api.Middleware;
using HrManegment.Application;
using HrManegment.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);




// Add services to the container.


builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .WriteTo.Console()
    .ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddControllers();


builder.Services.AddCors(options =>
{
    options.AddPolicy("all", builder => builder.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Fixed", HttpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: HttpContext.Connection.RemoteIpAddress.ToString(),
        factory:Partition=>new FixedWindowRateLimiterOptions { 
            
            PermitLimit=10,
        Window=TimeSpan.FromSeconds(10)
        }
        ));

});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseRateLimiter();





app.UseSerilogRequestLogging();
app.UseMiddleware<CheckUsersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("all");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
