﻿using Newtonsoft.Json;
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
		private JsonSerializerSettings JsonSerializerSettings { get; }
		private NumberFormatVisitor NumberFormatVisitor { get; } = new NumberFormatVisitor();
        private RequiredVisitor RequiredVisitor { get; } = new RequiredVisitor();
		private Config Config { get; set; }
		private StringBuilder Output { get; } = new StringBuilder();

		public Generator(string configFile, string outputFile)
		{
			ConfigFile = configFile;
			OutputFile = outputFile;
			JsonSerializerSettings = new JsonSerializerSettings()
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
				Config = JsonConvert.DeserializeObject<Config>(json, JsonSerializerSettings);
			}
			else
			{
				Config = Config.NewDefault();
				var json = JsonConvert.SerializeObject(Config, JsonSerializerSettings);
				File.WriteAllText(ConfigFile, json);
				Console.Error.WriteLine("Wrote default config file.");
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
				foreach (var serviceName in api.Services)
				{
					Console.Error.WriteLine("Generating client for {0}...", serviceName);
					var docUrl = string.Format(api.ServiceDoc, serviceName);
					var json = client.GetStringAsync(docUrl).GetAwaiter().GetResult();
					var doc = SwaggerDocument.FromJsonAsync(json).GetAwaiter().GetResult();

					if(api.ConvertNumbersToDecimal)
					{
						NumberFormatVisitor.VisitAsync(doc).GetAwaiter().GetResult();
					}

                    if(api.IgnoreRequired)
                    {
                        RequiredVisitor.VisitAsync(doc).GetAwaiter().GetResult();
                    }

					if(api.BasePath != null)
					{
						doc.SetBasePath(api.BasePath);
					}

					/* Generate code. */
					var settings = Clone(Config.Settings);
					var nsPrefix = api.Namespace ?? settings.CSharpGeneratorSettings.Namespace ?? DEFAULT_NAMESPACE;
					// TODO more comprehensive validation of ClassName and Namespace
					// provide collection of previous results to guarantee uniqueness
					// see https://stackoverflow.com/a/950651/8773089
					settings.CSharpGeneratorSettings.Namespace =
						(nsPrefix.Length == 0 ? nsPrefix : nsPrefix + ".") + serviceName.Replace("-", "");
					settings.ClassName = serviceName.Replace(".", "").Replace("-", "").Replace("_", "");
					var gen = new SwaggerToCSharpClientGenerator(doc, settings);
					var code = gen.GenerateFile();

					/* If UseBaseUrl is true, NSwag generates relative paths with leading slashes.
					 * This conflicts with HttpClient, which requires that BaseAddress ends with a
					 * slash and relative paths do not begin with slashes. */
					if(settings.UseBaseUrl)
					{
						code = code.Replace("Append(\"/", "Append(\"");
						if(settings.GenerateBaseUrlProperty)
						{
							/* Instead of removing the trailing slash from BaseUrl, require it. */
							code = code.Replace("BaseUrl.TrimEnd('/')", "BaseUrl.EndsWith(\"/\") ? BaseUrl : BaseUrl + \"/\"");
							if(!api.KeepBaseUrl)
							{
								code = code.Replace("_baseUrl = \"", "_baseUrl = null; // \"");
							}
						}
						else
						{
							/* Remove code referencing non-existent property. */
							code = code.Replace(".Append(BaseUrl != null ? BaseUrl.TrimEnd('/') : \"\")", "");
						}
					}

					/* YATTA! */
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
			return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj, JsonSerializerSettings), JsonSerializerSettings);
		}
	}
}
