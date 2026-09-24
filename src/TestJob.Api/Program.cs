using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TestJob.Api.Infrastructure;
using TestJob.Api.Models;
using TestJob.Api.Services;
using TestJob.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Relaxed escaping keeps Cyrillic and '&' readable instead of \uXXXX.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = context.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .Select(e => e.Value!.Errors[0].ErrorMessage is { Length: > 0 } m ? m : e.Value.Errors[0].Exception?.Message)
                .FirstOrDefault() ?? "Request body is invalid.";

            return new BadRequestObjectResult(ProcessResponse.Error(ErrorCodes.InvalidRequestBody, message));
        };
    });

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured."));

builder.Services.AddValidatorsFromAssemblyContaining<ProcessRequestValidator>(ServiceLifetime.Scoped);
builder.Services.AddScoped<PageProcessingService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "TestJob API", Version = "v1" });
    options.IncludeXmlComments(typeof(Program).Assembly);
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger(options => options.RouteTemplate = "api/swagger/{documentName}/swagger.json");
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/api/swagger/v1/swagger.json", "TestJob API v1");
    options.DocumentTitle = "TestJob API";
});

app.MapGet("/", () => Results.Redirect("/api/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
