using System.Text;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Repositories;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 2. Add controllers and SignalR
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// 3. Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BusinessModelApp API", Version = "v1" });
    c.CustomSchemaIds(type => type.ToString());

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 4. Configure Database with Append-Only Audit Interceptor
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "businessmodelapp.db");
builder.Services.AddSingleton<AppendOnlyAuditInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AppendOnlyAuditInterceptor>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("(localdb)"))
    {
        options.UseSqlServer(connectionString).AddInterceptors(interceptor);
    }
    else
    {
        options.UseSqlite($"Data Source={dbPath}").AddInterceptors(interceptor);
    }
});

// 5. Configure ASP.NET Core Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 6. Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SecureSecretKeyForBusinessModelAppAuthentication2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BusinessModelApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BusinessModelAppClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 7. Register Repositories & Services
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICommercialRepository, CommercialRepository>();
builder.Services.AddScoped<IBusinessHealthEngine, BusinessHealthEngine>();
builder.Services.AddScoped<IExecutiveBriefService, ExecutiveBriefService>();

// Register Domain Mock Services for peripheral modules
builder.Services.AddScoped<IProductService, MockProductService>();
builder.Services.AddScoped<IBusinessModelRepository, MockBusinessModelRepository>();
builder.Services.AddScoped<IUserRepository, MockUserRepository>();
builder.Services.AddScoped<IRoleRepository, MockRoleRepository>();
builder.Services.AddScoped<BusinessModelApp.Core.Interfaces.ITaskRepository, MockTaskRepository>();
builder.Services.AddScoped<IExpenseService, MockExpenseService>();
builder.Services.AddScoped<IRevenueService, MockRevenueService>();
builder.Services.AddScoped<IStrategyService, MockStrategyService>();
builder.Services.AddScoped<IAgentService, MockAgentService>();
builder.Services.AddScoped<IRecommendationService, MockRecommendationService>();

// 8. Register AI Services
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddHttpClient<OpenRouterService>();
builder.Services.AddHttpClient<MistralService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<OpenRouterService>();
builder.Services.AddScoped<MistralService>();
builder.Services.AddScoped<AntigravityAIService>();
builder.Services.AddScoped<LocalLLMService>();
builder.Services.AddScoped<ModelManagerService>();

// 9. Register Infrastructure Services & Agents
builder.Services.AddScoped<ICommandExecutionService, CommandExecutionService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IAgentBroadcaster, SignalRAgentBroadcaster>();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.AutonomousAgent>();

// AI Fallback Service
builder.Services.AddScoped<IAIService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FallbackAIService>>();
    var gemini = sp.GetRequiredService<GeminiService>();
    var openRouter = sp.GetRequiredService<OpenRouterService>();
    var mistral = sp.GetRequiredService<MistralService>();
    var antigravity = sp.GetRequiredService<AntigravityAIService>();
    var localLLM = sp.GetRequiredService<LocalLLMService>();

    var providers = new List<IAIService> { localLLM, gemini, openRouter, mistral, antigravity };
    return new FallbackAIService(providers, logger);
});

var app = builder.Build();

// 10. Auto-Seed Database strictly in Development or Testing environment
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        await SeedData.Initialize(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during development database seeding.");
    }
}

// 11. Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BusinessModelApp.Api.Hubs.AgentHub>("/agentHub");

app.Run();

// Export Program class for integration test fixture
public partial class Program { }
