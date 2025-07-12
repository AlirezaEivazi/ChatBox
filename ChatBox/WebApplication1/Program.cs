using System.Reflection;
using System.Text;
using ChatAppBackend.Data;
using ChatAppBackend.Hubs;
using ChatAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot" 
});

// 1. Load Configuration
var config = builder.Configuration;
var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not configured");
var jwtKey = config["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT secret key not configured");

// Add services to the container
builder.Services.AddDbContext<ChatAppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(10, 5, 12)), // Match your MariaDB version
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatApp API",
        Version = "v1",
        Description = "API for ChatApp with file upload support"
    });

    // JWT support
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer eyJhbGci...')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
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

    c.OperationFilter<SwaggerFileOperationFilter>();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(
        config.GetValue<int>("SignalR:ClientTimeoutInterval", 30));
    options.KeepAliveInterval = TimeSpan.FromSeconds(
        config.GetValue<int>("SignalR:KeepAliveInterval", 15));
    options.MaximumReceiveMessageSize = 1024 * 1024 * 10;
});

// Custom Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApp API V1");
        c.DisplayRequestDuration();
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");

// Auto Migrate
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatAppDbContext>();
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("✅ Database migrated successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database migration failed: {ex.Message}");
}

app.Run();


// ========== SwaggerFileOperationFilter ========== //
public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) ||
                       (p.ParameterType.IsGenericType &&
                        p.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                        p.ParameterType.GetGenericArguments()[0] == typeof(IFormFile)))
            .ToList();

        var formParams = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttribute<FromFormAttribute>() != null &&
                        p.ParameterType != typeof(IFormFile))
            .ToList();

        if (!fileParams.Any() && !formParams.Any()) return;

        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        foreach (var param in fileParams)
        {
            var paramName = param.Name ?? "file";
            schema.Properties[paramName] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary",
                Description = "File to upload"
            };
            schema.Required.Add(paramName);
        }

        foreach (var param in formParams)
        {
            foreach (var prop in param.ParameterType.GetProperties())
            {
                var (type, format) = GetOpenApiType(prop.PropertyType);
                schema.Properties[prop.Name] = new OpenApiSchema
                {
                    Type = type,
                    Format = format,
                    Nullable = !prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null
                };
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            }
        };
    }

    private (string type, string? format) GetOpenApiType(Type type)
    {
        if (type == typeof(string)) return ("string", null);
        if (type == typeof(int) || type == typeof(long)) return ("integer", "int64");
        if (type == typeof(float) || type == typeof(double)) return ("number", "double");
        if (type == typeof(bool)) return ("boolean", null);
        if (type == typeof(DateTime)) return ("string", "date-time");
        if (type == typeof(Guid)) return ("string", "uuid");
        return ("string", null);
    }
}
