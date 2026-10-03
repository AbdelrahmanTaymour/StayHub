using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StayHub.Api.OpenApi;

public sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum && schema is OpenApiSchema concreteSchema)
        {
            concreteSchema.Type = JsonSchemaType.String;
            concreteSchema.Format = null;

            concreteSchema.Enum = Enum.GetNames(context.Type)
                .Select(name => JsonValue.Create(name))
                .Cast<JsonNode?>()
                .ToList()!;
        }
    }
}