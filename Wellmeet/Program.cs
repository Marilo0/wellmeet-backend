using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Wellmeet.Configuration;
using Wellmeet.Data;
using Wellmeet.Helpers;
using Wellmeet.Repositories;
using Wellmeet.Services;
using Serilog;
using System.Text;
using Wellmeet.Services.Interfaces;
using Wellmeet.Core.Enums;
using Wellmeet.Security;

namespace Wellmeet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Required Env variables
            //var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost\\SQLEXPRESS";
            //var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "WellMeetDB2";
            //var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "MarialenaWM";
            //var dbPass = Environment.GetEnvironmentVariable("DB_PASS");
            //var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            //if (new[] { "DB_SERVER", "DB_NAME", "DB_USER", "DB_PASS", "JWT_SECRET_KEY" }
            //    .Any(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var))))
            //    throw new InvalidOperationException("Missing required environment variables");

            //var connString = builder.Configuration.GetConnectionString("DefaultConnection");
            //connString = connString!
            //    .Replace("{DB_SERVER}", dbServer)
            //    .Replace("{DB_NAME}", dbName)
            //    .Replace("{DB_USER}", dbUser)
            //    .Replace("{DB_PASS}", dbPass);

            //builder.Services.AddDbContext<WellmeetDbContext>(options => options.UseSqlServer(connString));        //-------THE SQL SERVER OPTION
            builder.Services.AddDbContext<WellmeetDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); //-------THE POSTGRES OPTION



            // Add SERVICES to the IoC container


            // UnitOfWork DI 
            builder.Services.AddRepositories();

            builder.Services.AddScoped<IApplicationService, ApplicationService>();

            builder.Services.AddScoped<IJwtService, JwtService>();


            // AutoMapper 
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperConfig>());

            // Serilog for structured logging
            builder.Host.UseSerilog((ctx, lc) =>
                lc.ReadFrom.Configuration(ctx.Configuration));


            // This configures JWT-based authentication using bearer tokens
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("Jwt");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],

                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])),

                    ValidateLifetime = true

                };
            });

            // Enables Cross-Origin Resource Sharing
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    b => b.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());

                //or could be seperated for front and back end
            });

            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
            });

            builder.Services.AddEndpointsApiExplorer();

            // Configures Swagger UI and security schemes
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Wellmeet App", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your valid token."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement  // to add the lock icon in each endpoint in swagger
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


            var app = builder.Build();

            // Admin seeding
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WellmeetDbContext>();
                if (!context.Users.Any(u => u.UserRole == UserRole.Admin))
                {
                    var adminPassword = builder.Configuration["AdminPassword"] ?? "Admin1Pass!";

                    var superAdmin = new User
                    {
                        Username = "admin",
                        Email = "admin@wellmeet.com",
                        Password = EncryptionUtil.Encrypt(adminPassword),
                        Firstname = "Super",
                        Lastname = "Administrator",
                        UserRole = UserRole.Admin
                    };
                    context.Users.Add(superAdmin);
                    context.SaveChanges();
                }
            }


            // Configure the HTTP request pipeline.

            // Logs every HTTP request + status code + duration
            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wellmeet API v1"));
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
