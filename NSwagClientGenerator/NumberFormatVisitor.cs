using NJsonSchema;
using NJsonSchema.Visitors;
using NSwag;
using System.Threading.Tasks;

namespace NSwagClientGenerator
{
	/// <summary>
	/// Changes numeric properties and parameters to decimals. The Swagger
	/// specification only standardizes "float" and "double" formats for
	/// "number" types. However, the spec explicitly allows for unspecified or
	/// non-standard formats. NSwag's C# client generator supports a "decimal"
	/// format to avoid the well-known problems with using floating points for
	/// currency.
	/// </summary>
	public class NumberFormatVisitor : JsonSchemaVisitorBase
	{
		protected override Task<JsonSchema4> VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint)
		{
			if (schema is SwaggerParameter || schema is JsonProperty)
			{
				if (schema.Type == JsonObjectType.Number)
				{
					schema.Format = JsonFormatStrings.Decimal;
				}
			}
			return Task.FromResult(schema);
		}
	}
}
