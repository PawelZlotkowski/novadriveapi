// Api/Program.cs
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using NovaDrive.Api.Endpoints.Admin;
using NovaDrive.Api.Endpoints.Public;
using NovaDrive.Api.Endpoints.Vehicle;
using NovaDrive.Api.Middleware;
using NovaDrive.Application.Services;
using NovaDrive.Application.Validators;
using NovaDrive.Infrastructure.External;
using NovaDrive.Infrastructure.MongoDb;
using NovaDrive.Infrastructure.Persistence;
using NovaDrive.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────
// SERILOG
// ──────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ──────────────────────────────────────
// DATABASE — PostgreSQL
// ──────────────────────────────────────
builder.Services.AddDbContext<NovaDriveDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql")));

// ──────────────────────────────────────
// DATABASE — MongoDB
// ──────────────────────────────────────
builder.Services.AddSingleton<MongoDbContext>();

// ──────────────────────────────────────
// REPOSITORIES
// ──────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IRideRepository, RideRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();
builder.Services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddSingleton<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddSingleton<ISensorDiagnosticRepository, SensorDiagnosticRepository>();

// ──────────────────────────────────────
// SERVICES
// ──────────────────────────────────────
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<ISensorDiagnosticService, SensorDiagnosticService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IEmailService, EmailSender>();

// ──────────────────────────────────────
// FLUENT VALIDATION
// ──────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<CreatePassengerRequestValidator>();

// ──────────────────────────────────────
// AUTH0 — JWT AUTHENTICATION
// ──────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth0:Authority"];       // https://YOUR_DOMAIN.auth0.com/
        options.Audience = builder.Configuration["Auth0:Audience"];         // https://api.novadrive.com
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// ──────────────────────────────────────
// AUTHORIZATION POLICIES
// ──────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PassengerPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("permissions", "read:passenger"))
    .AddPolicy("AdminPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("permissions", "manage:admin"));

// ──────────────────────────────────────
// CORS
// ──────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:DashboardUrl"] ?? "http://localhost:3000",
                builder.Configuration["Cors:PassengerUrl"] ?? "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ──────────────────────────────────────
// GRAPHQL — Hot Chocolate
// ──────────────────────────────────────
builder.Services
    .AddGraphQLServer()
    .AddQueryType<NovaDrive.GraphQL.Queries.Query>()
    .AddMutationType<NovaDrive.GraphQL.Mutations.Mutation>()
    .AddAuthorization();

// ──────────────────────────────────────
// SWAGGER / OPENAPI (optional, helpful for testing)
// ──────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Nova Drive API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ──────────────────────────────────────
// MIDDLEWARE PIPELINE
// ──────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ──────────────────────────────────────
// MAP ENDPOINTS
// ──────────────────────────────────────

// Public endpoints (passenger-facing)
app.MapGroup("/api/public/auth").MapAuthEndpoints();
app.MapGroup("/api/public/passengers").MapPublicPassengerEndpoints();
app.MapGroup("/api/public/rides").MapPublicRideEndpoints();
app.MapGroup("/api/public/payments").MapPublicPaymentEndpoints();
app.MapGroup("/api/public/pricing").MapPricingEndpoints();
app.MapGroup("/api/public/support-tickets").MapPublicSupportTicketEndpoints();
app.MapGroup("/api/public/discount-codes").MapPublicDiscountCodeEndpoints();
app.MapGroup("/api/public/vehicles").MapPublicVehicleEndpoints();

// Admin endpoints (internal management)
app.MapGroup("/api/admin/users").MapAdminUserEndpoints();
app.MapGroup("/api/admin/passengers").MapAdminPassengerEndpoints();
app.MapGroup("/api/admin/vehicles").MapAdminVehicleEndpoints();
app.MapGroup("/api/admin/rides").MapAdminRideEndpoints();
app.MapGroup("/api/admin/payments").MapAdminPaymentEndpoints();
app.MapGroup("/api/admin/maintenance").MapAdminMaintenanceEndpoints();
app.MapGroup("/api/admin/support-tickets").MapAdminSupportTicketEndpoints();
app.MapGroup("/api/admin/discount-codes").MapAdminDiscountCodeEndpoints();
app.MapGroup("/api/admin/telemetry").MapAdminTelemetryEndpoints();
app.MapGroup("/api/admin/diagnostics").MapAdminDiagnosticEndpoints();

// Vehicle system endpoints (API key auth)
app.MapGroup("/api/vehicle").MapVehicleSystemEndpoints();

// GraphQL
app.MapGraphQL("/graphql");

// ──────────────────────────────────────
// AUTO-MIGRATE DATABASE ON STARTUP (dev only)
// ──────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NovaDriveDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Make the implicit Program class accessible for integration tests
public partial class Program { }