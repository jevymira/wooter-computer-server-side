using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Model;
using Server;
using Server.Services;
using Server.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new()
    {
        Description = "APIs for Wooter Computer",
        Title = "Wooter Computer APIs.",
        Version = "V1"
    });
    OpenApiSecurityScheme jwtSecurityScheme = new()
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Please enter *only* JWT token",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
     {
         { jwtSecurityScheme, [] }
     });
});
builder.Services.AddDbContext<WootComputersSourceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddIdentity<WooterComputerUser, IdentityRole>()
    .AddEntityFrameworkStores<WootComputersSourceContext>();
builder.Services.AddScoped<JwtHandler>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        RequireAudience = true,
        RequireExpirationTime = true,
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecurityKey"]))
            ?? throw new InvalidOperationException()
    };
});
// Rather than instantiating (multiple) HttpClients directly, see:
// https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
builder.Services.AddHttpClient<WootService>();
builder.Services.AddScoped<WootService>();
builder.Services.AddScoped<IWootClient, WootClient>();
builder.Services.AddHostedService<WootWorkerService>();
builder.Services.AddScoped<OfferService>();
builder.Services.AddScoped<BookmarkService>();

// Logging WootWorkerService.
builder.Services.AddLogging(builder => builder.AddConsole());

// Prevent unhandled exceptions in the BackgroundService from stopping the Host, from
// https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling
builder.Services.Configure<HostOptions>((hostOptions) =>
{
    // By default: BackgroundServiceExceptionBehavior.StopHost
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(option => option.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseAuthorization();

app.MapControllers();

app.Run();
