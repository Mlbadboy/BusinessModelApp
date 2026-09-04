using System.Text;
using System.Text.Json;
using BusinessModelApp.Api.Health;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Observability;
using BusinessModelApp.Core.Repositories;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Repositories;
using BusinessModelApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

// 4. Configure Database with Append-Only Audit Interceptor and Connection Resiliency
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "businessmodelapp.db");
builder.Services.AddSingleton<AppendOnlyAuditInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AppendOnlyAuditInterceptor>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("(localdb)"))
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        }).AddInterceptors(interceptor);
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

// 8. Register OmniRoute AI Gateway & Infrastructure
builder.Services.Configure<BusinessModelApp.Infrastructure.AI.OmniRoute.OmniRouteOptions>(
    builder.Configuration.GetSection(BusinessModelApp.Infrastructure.AI.OmniRoute.OmniRouteOptions.SectionName));

builder.Services.AddHttpClient<BusinessModelApp.Infrastructure.AI.OmniRoute.IOmniRouteClient, BusinessModelApp.Infrastructure.AI.OmniRoute.OmniRouteClient>();
builder.Services.AddScoped<BusinessModelApp.Core.AI.IAIRoutingPolicyService, BusinessModelApp.Api.Services.AIRoutingPolicyService>();
builder.Services.AddScoped<BusinessModelApp.Core.AI.Governance.IAIDataMinimizationService, BusinessModelApp.Core.AI.Governance.AIDataMinimizationService>();
builder.Services.AddScoped<BusinessModelApp.Infrastructure.Services.IBudgetReservationService, BusinessModelApp.Infrastructure.Services.BudgetReservationService>();
builder.Services.AddScoped<BusinessModelApp.Core.AI.Governance.IApprovalService, BusinessModelApp.Infrastructure.Services.ApprovalService>();
builder.Services.AddScoped<BusinessModelApp.Core.AI.IAIInferenceGateway, BusinessModelApp.Api.Services.AIInferenceGateway>();
builder.Services.AddScoped<BusinessModelApp.Core.Services.IAIROIService, BusinessModelApp.Core.Services.AIROIService>();

// 8.5 Register Governed Voice Telephony Services
builder.Services.Configure<BusinessModelApp.Infrastructure.Options.VoiceOptions>(
    builder.Configuration.GetSection(BusinessModelApp.Infrastructure.Options.VoiceOptions.SectionName));
builder.Services.AddVoiceTelephonyServices();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.IToolExecutionAdapter, BusinessModelApp.Core.Agents.VoiceCallAdapter>();

// 8.6 Register Charlie Connect Integration Control Plane & Credential Vault
builder.Services.AddScoped<BusinessModelApp.Core.Interfaces.IConnectorVaultService, BusinessModelApp.Infrastructure.Services.ConnectorVaultService>();
builder.Services.AddScoped<BusinessModelApp.Core.Interfaces.IConnectorCapabilityService, BusinessModelApp.Infrastructure.Services.ConnectorCapabilityService>();
builder.Services.AddScoped<BusinessModelApp.Core.Interfaces.IConnectorHealthService, BusinessModelApp.Infrastructure.Services.ConnectorHealthService>();

// 8.7 Register Real Autonomous Prospect Discovery & ICP Scoring Engine
builder.Services.AddScoped<BusinessModelApp.Core.Prospecting.ICompanyIntelligenceProvider, BusinessModelApp.Infrastructure.Prospecting.CompanyIntelligenceProvider>();
builder.Services.AddScoped<BusinessModelApp.Core.Prospecting.IICPScoringEngine, BusinessModelApp.Infrastructure.Prospecting.ICPScoringEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Prospecting.IDecisionMakerDiscoveryProvider, BusinessModelApp.Infrastructure.Prospecting.DecisionMakerDiscoveryProvider>();
builder.Services.AddScoped<BusinessModelApp.Core.Prospecting.IProspectDiscoveryService, BusinessModelApp.Infrastructure.Prospecting.ProspectDiscoveryService>();

// 8.8 Register Charlie OS v5 Reality & Truth Engine (NO EVIDENCE => NO FACT)
builder.Services.AddScoped<BusinessModelApp.Core.Reality.IRealityEngine, BusinessModelApp.Infrastructure.Reality.RealityEngine>();

// 8.9 Register Charlie OS v5 Phase 1 World Model, Objective & Strategy Intelligence
builder.Services.AddScoped<BusinessModelApp.Core.WorldModel.ICompanyWorldModel, BusinessModelApp.Infrastructure.WorldModel.CompanyWorldModel>();
builder.Services.AddScoped<BusinessModelApp.Core.Objectives.IObjectiveEngine, BusinessModelApp.Infrastructure.Objectives.ObjectiveEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Strategy.IStrategyEngine, BusinessModelApp.Infrastructure.Strategy.StrategyEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Constitution.ICompanyConstitutionService, BusinessModelApp.Infrastructure.Constitution.CompanyConstitutionService>();
builder.Services.AddScoped<BusinessModelApp.Core.Decisions.IDecisionEngine, BusinessModelApp.Infrastructure.Decisions.DecisionEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Missions.IDurableMissionOrchestrator, BusinessModelApp.Infrastructure.Missions.DurableMissionOrchestrator>();

// Phase 1 v1.2: Autonomous Commercial Officer Runtime & Business Hive
builder.Services.AddScoped<BusinessModelApp.Core.Strategy.IReverseFunnelEngine, BusinessModelApp.Infrastructure.Strategy.ReverseFunnelEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Strategy.IDeterministicStrategySimulator, BusinessModelApp.Infrastructure.Strategy.DeterministicStrategySimulator>();
builder.Services.AddScoped<BusinessModelApp.Core.Strategy.IAIStrategySimulator, BusinessModelApp.Infrastructure.Strategy.AIStrategySimulator>();
builder.Services.AddScoped<BusinessModelApp.Core.Constitution.IConstitutionPolicyEngine, BusinessModelApp.Core.Constitution.ConstitutionPolicyEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Strategy.ICommercialStrategyEngine, BusinessModelApp.Infrastructure.Strategy.CommercialStrategyEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.AgentPolicyEngine>();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.IAgentRuntime, BusinessModelApp.Core.Agents.AgentRuntime>();
builder.Services.AddSingleton<BusinessModelApp.Core.Agents.IAgentMessageBus, BusinessModelApp.Core.Agents.InMemoryAgentMessageBus>();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.AgentHeartbeatService>();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.AgentRecoveryService>();
builder.Services.AddScoped<BusinessModelApp.Core.Services.ICharlieExecutiveService, BusinessModelApp.Infrastructure.Services.CharlieExecutiveService>();

// Phase 1.5 v1.3.1 Production Hardening Services (Batch 1: H0 - H3)
builder.Services.AddSingleton<BusinessModelApp.Core.Domain.WorldModel.IPhase1BaselineGuard, BusinessModelApp.Core.Domain.WorldModel.Phase1BaselineGuard>();
builder.Services.AddSingleton<BusinessModelApp.Core.AI.IModelRegistry, BusinessModelApp.Infrastructure.AI.ModelRegistry>();
builder.Services.AddSingleton<BusinessModelApp.Core.AI.IModelRouter, BusinessModelApp.Infrastructure.AI.ModelRouter>();
builder.Services.AddSingleton<BusinessModelApp.Core.Domain.Reality.IEvidenceGraph, BusinessModelApp.Infrastructure.Reality.EvidenceGraphService>();
builder.Services.AddSingleton<BusinessModelApp.Core.Domain.Reality.IRealityDecayEngine, BusinessModelApp.Infrastructure.Reality.RealityDecayEngine>();

// Phase 1.5 v1.3.1 Production Hardening Services (Batch 2: H4 - H7)
builder.Services.AddSingleton<BusinessModelApp.Core.Agents.ITrustScoreEngine, BusinessModelApp.Infrastructure.Agents.TrustScoreEngine>();
builder.Services.AddSingleton<BusinessModelApp.Core.Agents.IHierarchicalWalletManager, BusinessModelApp.Infrastructure.Agents.HierarchicalWalletManager>();
builder.Services.AddSingleton<BusinessModelApp.Core.Domain.Missions.IMissionEventStore, BusinessModelApp.Infrastructure.Missions.EventSourcedMissionStore>();
builder.Services.AddScoped<BusinessModelApp.Core.Domain.Missions.IMissionForkEngine, BusinessModelApp.Infrastructure.Missions.MissionForkEngine>();

// Gate 6: Autonomous Agent Orchestrator & Governed Tool Registry
builder.Services.AddScoped<BusinessModelApp.Core.Agents.IGovernedToolRegistry, BusinessModelApp.Core.Agents.GovernedToolRegistry>();
builder.Services.AddSingleton<BusinessModelApp.Core.Services.IAgentOrchestratorService>(sp => 
    new BusinessModelApp.Core.Services.AgentOrchestratorService(sp.GetRequiredService<IServiceScopeFactory>()));

// 9. Register Production Health Checks
builder.Services.AddScoped<AppDbContextHealthCheck>();
builder.Services.AddScoped<OmniRouteHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<AppDbContextHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<OmniRouteHealthCheck>("omni_route", tags: new[] { "ai" });

// Backward compatibility adapter
#pragma warning disable CS0618
builder.Services.AddScoped<IAIService, BusinessModelApp.Api.Services.AIServiceAdapter>();
#pragma warning restore CS0618

// 10. Register Infrastructure Services & Agents
builder.Services.AddScoped<ICommandExecutionService, CommandExecutionService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IAgentBroadcaster, SignalRAgentBroadcaster>();
builder.Services.AddScoped<BusinessModelApp.Core.Agents.AutonomousAgent>();

var app = builder.Build();

// 11. Auto-Seed Database strictly in Development or Testing environment
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

// 12. Security Headers & Correlation Middleware
app.Use(async (context, next) =>
{
    // Security Headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Correlation ID
    if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }
    context.Response.Headers.Append("X-Correlation-Id", correlationId);

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// 13. Production Health Probes (Sanitized JSON Output)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"status\":\"Live\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}");
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteSanitizedHealthResponseAsync
});

app.MapHealthChecks("/health/ai-gateway", new HealthCheckOptions
{
    Predicate = check => check.Name == "omni_route",
    ResponseWriter = WriteSanitizedHealthResponseAsync
});

app.MapControllers();
app.MapHub<BusinessModelApp.Api.Hubs.AgentHub>("/agentHub");

app.Run();

static async Task WriteSanitizedHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = TelemetrySanitizer.Sanitize(e.Value.Description),
            durationMs = e.Value.Duration.TotalMilliseconds
        })
    };

    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    await context.Response.WriteAsync(json);
}

// Export Program class for integration test fixture
public partial class Program { }
