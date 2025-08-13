using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace UserManagementApi.Api.OpenApi;

/// <summary>
/// Small helpers to enrich OpenAPI with examples and standard problem responses.
/// </summary>
public static class OpenApiDocs
{
    public static OpenApiObject Problem(string title, int status, string detail) => new()
    {
        ["title"]  = new OpenApiString(title),
        ["status"] = new OpenApiInteger(status),
        ["detail"] = new OpenApiString(detail)
    };

    public static void Ensure409(OpenApiOperation op, string title, string detail)
    {
        if (!op.Responses.TryGetValue("409", out var cn))
            cn = op.Responses["409"] = new OpenApiResponse { Description = "Conflict" };

        cn.Content ??= new Dictionary<string, OpenApiMediaType>();
        cn.Content["application/json"] = new OpenApiMediaType
        {
            Example = Problem(title, 409, detail)
        };
    }

    public static void Ensure404(OpenApiOperation op, string detail)
    {
        if (!op.Responses.TryGetValue("404", out var nf))
            nf = op.Responses["404"] = new OpenApiResponse { Description = "Not Found" };

        nf.Content ??= new Dictionary<string, OpenApiMediaType>();
        nf.Content["application/json"] = new OpenApiMediaType
        {
            Example = Problem("Not Found", 404, detail)
        };
    }
}
