using System.Text;
using Favilonia.API.Authorization;
using Favilonia.API.Extensions;
using Favilonia.API.Middleware;
using Favilonia.API.Services;
using Favilonia.API.Settings;
using Favilonia.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiInfrastructure();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options => 
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy  .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Получаем JWT ключ из переменной окружения или конфигурации
var secretKey = Environment.GetEnvironmentVariable("Jwt__Key") 
    ?? builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 16)
{
    throw new InvalidOperationException(
        "JWT key must be configured via environment variable 'Jwt__Key' or in appsettings.json under Jwt:Key " +
        "and contain at least 16 characters. " +
        "For production, set environment variable: $env:Jwt__Key='your-secret-key-at-least-32-chars'");
}

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenService>();
// Замени ConsoleEmailService на реальную реализацию когда подключишь SMTP.
builder.Services.AddScoped<IEmailService, ConsoleEmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter("LoginLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("RegisterLimiter", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 3;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

// Initialize database with seed data

    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync();

            if (app.Environment.IsDevelopment())
            {
                await SeedData.InitializeAsync(context);
            }
        }
    }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseMiddleware<ApiExceptionHandlerMiddleware>();


app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
