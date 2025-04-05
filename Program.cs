using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Back_HR.Models;
using Back_HR.Models.enums;
using Back_HR.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<OllamaService>();


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    }); ;
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HR API",
        Version = "v1",
        Description = "API for managing HR operations including authentication, job offers, and surveys."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token (e.g., 'Bearer eyJhbGciOi...')."
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
    c.OperationFilter<JsonResponseOperationFilter>();
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add DbContext
builder.Services.AddDbContext<HRContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<HRContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    options.MapInboundClaims = false;
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<HRContext>();
            var tokenString = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(tokenString))
            {
                context.Fail("No token provided");
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(tokenString))
            {
                context.Fail("Invalid token format");
                return;
            }

            var jwtToken = tokenHandler.ReadJwtToken(tokenString);
            if (await dbContext.RevokedTokens.AnyAsync(bt => bt.Token == tokenString))
            {
                context.Fail("Token is revoked");
            }
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RHOnly", policy => policy.RequireClaim("UserType", UserType.RH.ToString()));
    options.AddPolicy("EmployeOnly", policy => policy.RequireClaim("UserType", UserType.EMPLOYE.ToString()));
    options.AddPolicy("CandidatOnly", policy => policy.RequireClaim("UserType", UserType.CANDIDAT.ToString()));
});

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] roleNames = { "RH", "Employe", "Candidat" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpper()
            });
        }
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HR API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAngular");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// Custom Operation Filter to enforce JSON responses
public class JsonResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Remove non-JSON content types from responses
        foreach (var response in operation.Responses)
        {
            var content = response.Value.Content;
            if (content != null)
            {
                var contentTypesToRemove = content.Keys
                    .Where(k => k != "application/json")
                    .ToList();
                foreach (var key in contentTypesToRemove)
                {
                    content.Remove(key);
                }

                // Ensure application/json exists
                if (!content.ContainsKey("application/json"))
                {
                    content["application/json"] = new OpenApiMediaType
                    {
                        Schema = content.FirstOrDefault().Value?.Schema ?? new OpenApiSchema { Type = "string" }
                    };
                }
            }
            else
            {
                response.Value.Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema { Type = "string" }
                    }
                };
            }
        }

        // Ensure request bodies only use application/json
        if (operation.RequestBody?.Content != null)
        {
            var requestContent = operation.RequestBody.Content;
            var contentTypesToRemove = requestContent.Keys
                .Where(k => k != "application/json")
                .ToList();
            foreach (var key in contentTypesToRemove)
            {
                requestContent.Remove(key);
            }
        }
    }
}