using System.Net;
using System.Text.Json;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Domain.Exceptions;

namespace GK.GlobalKineticAssessment.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await Handle(ctx, ex); }
    }

    private async Task Handle(HttpContext ctx, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        var (code, response) = ex switch
        {
            NotFoundException nfe      => (HttpStatusCode.NotFound, new ErrorResponse(nfe.Message)),
            DuplicateEmailException de => (HttpStatusCode.Conflict, new ErrorResponse(de.Message)),
            ValidationException ve     => (HttpStatusCode.UnprocessableEntity, new ErrorResponse(ve.Message, ve.Errors)),
            _                          => (HttpStatusCode.InternalServerError, new ErrorResponse("An unexpected error occurred."))
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)code;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, Opts));
    }
}
