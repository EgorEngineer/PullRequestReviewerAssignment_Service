using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Errors;
using PullRequest_Service.Infrastructure.Data;
using PullRequest_Service.Services;
using Swashbuckle.AspNetCore.Swagger;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Prometheus.DotNetRuntime;
using Prometheus;

namespace PullRequest_Service
{
    public class Program
    {
        public static async Task Main()
        {
            var builder = WebApplication.CreateBuilder();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.Seq(
                    builder.Configuration.GetValue<string>("SEQ_URL") ?? "http://seq:5341",
                    restrictedToMinimumLevel: LogEventLevel.Error
                ).CreateLogger();

            builder.Host.UseSerilog();

            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                    opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "PR Reviewer Assignment Service",
                    Version = "v1",
                    Description = "Сервис автоматического назначения ревьюверов для Pull Request"
                });
            });

            builder.Configuration.AddEnvironmentVariables();

            var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            var database = Environment.GetEnvironmentVariable("POSTGRES_DB");
            var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");

            Log.Information("Database configuration: Host={host}, Port={port}, User={user}, Database={database}, password={password}",
                host, port, user, database, password);

            var connStr = $"Server={host};Port={port};Database={database};Username={user};Password={password}";

            if (string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(port))
            {
                connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("DataBase environment variables are not set");

            builder.Services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseNpgsql(connStr, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
            );

            builder.Services.AddScoped<TeamService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<PullRequestService>();
            builder.Services.AddScoped<ReviewerAssignmentService>();
            builder.Services.AddScoped<HealthCheckService>();
            builder.Services.AddScoped<StatisticsService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
            });

            var collector = DotNetRuntimeStatsBuilder.Default().StartCollecting();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                try
                {
                    Log.Information("Applying EF migrations...");
                    db.Database.Migrate();
                    Log.Information("Migrations applied successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Migration failed!");
                    throw;
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseCors("AllowAll");

            app.UseHttpMetrics();
            app.MapMetrics("/metrics");

            app.MapControllers();

            await app.RunAsync();
        }


        private static async Task RetryAsync(Func<Task> action, int maxAttempts, TimeSpan delay, Serilog.ILogger logger)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await action();
                    logger.Information("Operation completed successfully");
                    break;
                }
                catch (Exception ex)
                {
                    if (i == maxAttempts - 1)
                    {
                        logger.Error(ex, "Failed after {MaxAttempts} attempts", maxAttempts);
                        throw;
                    }

                    logger.Warning(ex, "Attempt {Attempt}/{MaxAttempts} failed", i + 1, maxAttempts);
                    await Task.Delay(delay);
                }
            }
        }
    }
}