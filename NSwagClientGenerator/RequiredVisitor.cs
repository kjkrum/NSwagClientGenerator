using NJsonSchema;
using NJsonSchema.Visitors;
using System.Threading.Tasks;

namespace NSwagClientGenerator
{
    /// <summary>
    /// Removes required properties. This is a hack to allow consuming third
    /// party APIs that lie about the nullability of their properties.
    /// </summary>
    class RequiredVisitor : JsonSchemaVisitorBase
    {
        protected override Task<JsonSchema4> VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint)
        {
            schema.RequiredProperties.Clear();
            return Task.FromResult(schema);
        }
    }
}
