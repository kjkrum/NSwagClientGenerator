using Newtonsoft.Json;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NSwagClientGenerator
{
	public class Generator
	{
		public const string DEFAULT_NAMESPACE = "Generated";

		private string ConfigFile { get; }
		private string OutputFile { get; }
		private JsonSerializerSettings SerializerSettings { get; }
		private Config Config { get; set; }
		private StringBuilder Output { get; } = new StringBuilder();

		public Generator(string configFile, string outputFile)
		{
			ConfigFile = configFile;
			OutputFile = outputFile;
			SerializerSettings = new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented,
				TypeNameHandling = TypeNameHandling.Auto
			};
		}

		public void Start()
		{
			LoadConfig();
			ExtractUsings();
			foreach (var api in Config.Apis.Where(o => o.Services.Count > 0))
			{
				Generate(api);
			}
			WriteOutput();
		}

		private void LoadConfig()
		{
			if(File.Exists(ConfigFile))
			{
				var json = File.ReadAllText(ConfigFile);
				Config = JsonConvert.DeserializeObject<Config>(json, SerializerSettings);
			}
			else
			{
				var config = Config.NewDefault();
				var json = JsonConvert.SerializeObject(config, SerializerSettings);
				File.WriteAllText(ConfigFile, json);
				throw new Exception("Wrote default config file. Please edit and rebuild.");
			}
		}

		private void ExtractUsings()
		{
			if(Config.Settings.AdditionalNamespaceUsages.Length > 0)
			{
				foreach(var ns in Config.Settings.AdditionalNamespaceUsages)
				{
					Output.AppendFormat("using {0};\n", ns);
				}
				Output.AppendLine();
				Config.Settings.AdditionalNamespaceUsages = Array.Empty<string>();
			}
		}

		private void Generate(Api api)
		{
			using(var client = api.NewClient())
			{
				foreach(var service in api.Services)
				{
					var json = client.GetStringAsync(string.Format(api.Url, service)).GetAwaiter().GetResult();
					var doc = SwaggerDocument.FromJsonAsync(json).GetAwaiter().GetResult();
					var settings = Clone(Config.Settings);
					settings.ClassName = service.Replace(".", "");
					settings.CSharpGeneratorSettings.Namespace =
						(string.IsNullOrWhiteSpace(api.Namespace) ? string.IsNullOrWhiteSpace(settings.CSharpGeneratorSettings.Namespace) ? DEFAULT_NAMESPACE : settings.CSharpGeneratorSettings.Namespace : api.Namespace)
						+ "." + service;
					var gen = new SwaggerToCSharpClientGenerator(doc, settings);
					var code = gen.GenerateFile();
					Output.Append(code);
				}
			}
		}

		private void WriteOutput()
		{
			File.WriteAllText(OutputFile, Output.ToString());
		}

		private T Clone<T>(T obj)
		{
			return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj, SerializerSettings), SerializerSettings);
		}
	}
}
