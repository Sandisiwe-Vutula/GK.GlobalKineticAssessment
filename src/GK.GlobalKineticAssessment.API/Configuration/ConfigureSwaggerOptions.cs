using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GK.GlobalKineticAssessment.API.Configuration;

public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var desc in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(desc.GroupName, new OpenApiInfo
            {
                Title       = "GlobalKinetic Assessment API",
                Version     = desc.ApiVersion.ToString(),
                Description = "Customer Management API"
            });
        }

        options.AddSecurityDefinition("BasicAuth", new OpenApiSecurityScheme
        {
            Type   = SecuritySchemeType.Http,
            Scheme = "basic",
            Description = "Basic Auth. Username: gkadmin | Password: GK@Assessment2026!"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "BasicAuth" }
                },
                Array.Empty<string>()
            }
        });
    }
}
