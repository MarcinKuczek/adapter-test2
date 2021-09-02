using Liberis.OrchestrationAdapter.Core.Models.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public static class HealthCheckFormatter
{
    private const string ContentType = "application/json; charset=urf-8";

    public static async Task ReadinessResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = ContentType;

        var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;

        var response = new ReadinessHealthCheckResponse
        {
            Status = report.Status.ToString(),
            HealthChecks = report.Entries.Select(hc => new HealthCheck
            {
                Component = hc.Key,
                Status = hc.Value.Status.ToString(),
                Description = hc.Value.Description
            }),
            Uptime = $"{uptime:dd\\d\\:hh\\h\\:mm\\m\\:ss\\s}"
        };

        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
    public static async Task LivenessResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = ContentType;

        var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Uptime = $"{uptime:dd\\d\\:hh\\h\\:mm\\m\\:ss\\s}"
        };

        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }


}
