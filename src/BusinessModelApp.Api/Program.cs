using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.Repositories;
// using BusinessModelApp.Infrastructure.Data;
// using BusinessModelApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001") // Allow both ports
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.ToString());
});

// Configure DbContext with SQLite in-memory for testing
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlite("DataSource=:memory:"));

// Configure ASP.NET Core Identity
// Identity configuration commented out for testing
// builder.Services.AddIdentity<User, Role>(options => {
//     // For simplicity in this context, we can relax password requirements
//     options.Password.RequireDigit = false;
//     options.Password.RequiredLength = 6;
//     options.Password.RequireLowercase = false;
//     options.Password.RequireNonAlphanumeric = false;
//     options.Password.RequireUppercase = false;
// })
// .AddEntityFrameworkStores<AppDbContext>();

// Register application services and repositories
// Register Mock Services
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

// builder.Services.AddScoped<IAnalyticsService, AnalyticsService>(); // Need to mock this too if used
// builder.Services.AddScoped<IRealTimeMonitoringService, RealTimeMonitoringService>();
// builder.Services.AddScoped<IDataExportService,// Register AI Services
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddHttpClient<OpenRouterService>();
builder.Services.AddHttpClient<MistralService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<OpenRouterService>();
builder.Services.AddScoped<MistralService>();
builder.Services.AddScoped<AntigravityAIService>();
builder.Services.AddScoped<LocalLLMService>();
builder.Services.AddScoped<ModelManagerService>();

// Register Infrastructure Services ("The Hands")
builder.Services.AddScoped<ICommandExecutionService, CommandExecutionService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<BusinessModelApp.Core.Interfaces.IAgentBroadcaster, BusinessModelApp.Api.Services.SignalRAgentBroadcaster>();

// Register Agents ("The Brain")
builder.Services.AddScoped<BusinessModelApp.Core.Agents.AutonomousAgent>();

// Register Fallback Service as the primary IAIService
builder.Services.AddScoped<IAIService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FallbackAIService>>();
    var gemini = sp.GetRequiredService<GeminiService>();
    var openRouter = sp.GetRequiredService<OpenRouterService>();
    var mistral = sp.GetRequiredService<MistralService>();
    var antigravity = sp.GetRequiredService<AntigravityAIService>();
    var localLLM = sp.GetRequiredService<LocalLLMService>();
    
    // Define the fallback order: LocalLLM (if loaded) -> Gemini -> OpenRouter -> Mistral -> Antigravity
    // We prioritize LocalLLM if it's loaded because it's free and offline.
    var providers = new List<IAIService> { localLLM, gemini, openRouter, mistral, antigravity };
    
    return new FallbackAIService(providers, logger);
});
// builder.Services.AddHttpClient<IAIService, OpenRouterService>();
// builder.Services.AddHttpClient<ILocalModelService, LocalModelService>();

var app = builder.Build();

/*
// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        // Ensure the database is created. 
        var context = services.GetRequiredService<AppDbContext>();

        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Development environment. Deleting and recreating database...");
            context.Database.EnsureDeleted();
        }

        // Note: In a real app, you'd use migrations. For this context, EnsureCreated is simpler.
        context.Database.EnsureCreated(); 

        await SeedData.Initialize(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
*/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BusinessModelApp.Api.Hubs.AgentHub>("/agentHub");

app.Run();
