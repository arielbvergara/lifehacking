using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        const string bearerSchemeId = "bearer"; // lowercase per RFC 7235

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LifeHacking API",
                Version = "v1"
            });

            // Include XML documentation comments so controller and action summaries
            // and remarks appear in the generated OpenAPI spec and Swagger UI.
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            // Map IFormFile to a file upload schema for multipart/form-data
            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "binary"
            });

            // Add operation filter to handle IFormFile parameters with [FromForm]
            options.OperationFilter<FileUploadOperationFilter>();

            // Enable JWT bearer token support in Swagger UI
            options.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = bearerSchemeId,
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Paste only the JWT, without the 'Bearer ' prefix."
            });

            // Swashbuckle 10 / Microsoft.OpenApi v2+ expects a delegate here
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(bearerSchemeId, document)] = []
            });
        });

        return services;
    }

    /// <summary>
    /// Operation filter to properly handle IFormFile parameters with [FromForm] attribute.
    /// This ensures Swagger correctly generates the schema for file upload endpoints.
    /// </summary>
    private class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.ModelMetadata.ModelType == typeof(IFormFile))
                .ToList();

            if (!fileParameters.Any())
            {
                return;
            }

            // Clear existing parameters for file uploads
            operation.Parameters?.Clear();

            // Set request body for multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = fileParameters.ToDictionary<ApiParameterDescription, string, IOpenApiSchema>(
                                p => p.Name,
                                _ => new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Format = "binary"
                                }),
                            Required = fileParameters
                                .Where(p => p.IsRequired)
                                .Select(p => p.Name)
                                .ToHashSet()
                        }
                    }
                }
            };
        }
    }
}
