using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using System.Collections.Generic;

namespace NSwagClientGenerator
{
	public class Config
	{
		public List<Api> Apis {get; set; }
		public SwaggerToCSharpClientGeneratorSettings Settings { get; set; }

		public static Config NewDefault()
		{
			var config = new Config()
			{
				Apis = new List<Api>() { new Api() },
				Settings = new SwaggerToCSharpClientGeneratorSettings()
				{
					OperationNameGenerator = new SingleClientFromOperationIdOperationNameGenerator()
				}
			};
			config.Settings.CSharpGeneratorSettings.Namespace = "Generated";
			return config;
		}
	}
}
