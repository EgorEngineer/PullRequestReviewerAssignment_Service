using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Infrastructure.Data;
using System.Diagnostics;
using System.Reflection;
using Bogus;

namespace PullRequest_Service.Services
{
    public class HealthCheckService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HealthCheckService> _logger;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public HealthCheckService(ApplicationDbContext db, ILogger<HealthCheckService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
        {
            var result = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow,
                Service = new ServiceInfo
                {
                    Name = "pullrequest-service",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
                    UptimeSeconds = (long)(DateTime.UtcNow - _startTime).TotalSeconds,
                    Hostname = Environment.MachineName
                }
            };

            try
            {
                var sw = Stopwatch.StartNew();
                var canConnect = await _db.Database.CanConnectAsync(ct);
                sw.Stop();

                result.Database = new DatabaseHealth
                {
                    Status = canConnect ? "healthy" : "unhealthy",
                    ResponseTime = sw.ElapsedMilliseconds
                };

                if (!canConnect)
                {
                    result.Status = "unhealthy";
                    return result;
                }

                result.Database.Stats = new DatabaseStats
                {
                    TeamsCount = await _db.Teams.CountAsync(ct),
                    UsersCount = await _db.Users.CountAsync(ct),
                    PullRequestsCount = await _db.PullRequests.CountAsync(ct)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");

                result.Status = "unhealthy";
                result.Database = new DatabaseHealth
                {
                    Status = "unhealthy",
                    Error = ex.Message
                };
                return result;
            }

            result.Status = "healthy";
            return result;
        }
    }

    // ===== MODELS =====

    public class HealthCheckResult
    {
        public string Status { get; set; } = "healthy";
        public DateTime Timestamp { get; set; }
        public ServiceInfo Service { get; set; }
        public DatabaseHealth? Database { get; set; }

        public bool IsHealthy =>
            Status == "healthy" &&
            (Database?.Status != "unhealthy");
    }

    public class ServiceInfo
    {
        public string Name { get; set; }
        public string? Version { get; set; }
        public string Environment { get; set; }
        public long UptimeSeconds { get; set; }
        public string Hostname { get; set; }
    }

    public class DatabaseHealth
    {
        public string Status { get; set; } = string.Empty;
        public long? ResponseTime { get; set; }
        public string? Error { get; set; }
        public DatabaseStats? Stats { get; set; }
    }

    public class DatabaseStats
    {
        public int TeamsCount { get; set; }
        public int UsersCount { get; set; }
        public int PullRequestsCount { get; set; }
    }
}
