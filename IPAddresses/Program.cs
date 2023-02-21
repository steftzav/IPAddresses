using IPAddresses.Models;
using static IPAddresses.Models.IpaddressesContext;
using IPAddresses.Services;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<IpaddressesContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("IpaddressesContext"),
    sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    });
});
builder.Services.AddScoped<IDBService, DBService>();
builder.Services.AddHostedService<PeriodicUpdateService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
