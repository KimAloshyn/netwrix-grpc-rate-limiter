using Microsoft.AspNetCore.Server.Kestrel.Core;
using YourRateLimiterService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure gRPC server to listen on port 5000.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, listen =>
    {
        listen.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container.
builder.Services.AddGrpc();

// Register one shared instance of or rate limiter.
builder.Services.AddSingleton<RateLimiterEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<RateLimiterGrpcService>();

app.Run();
