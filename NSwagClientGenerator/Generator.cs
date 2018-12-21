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
				// TODO allow this to be null?
				var apiNamespace = string.IsNullOrWhiteSpace(api.Namespace) ?
					string.IsNullOrWhiteSpace(Config.Settings.CSharpGeneratorSettings.Namespace) ?
					DEFAULT_NAMESPACE : Config.Settings.CSharpGeneratorSettings.Namespace : api.Namespace;
				foreach (var serviceName in api.Services)
				{
					var docUrl = string.Format(api.ServiceDoc, serviceName);
					var json = client.GetStringAsync(docUrl).GetAwaiter().GetResult();
					var doc = SwaggerDocument.FromJsonAsync(json).GetAwaiter().GetResult();
					var settings = Clone(Config.Settings);
					// TODO replace ALL invalid characters
					settings.ClassName = serviceName.Replace(".", "").Replace("-", "").Replace("_", "");
					settings.CSharpGeneratorSettings.Namespace = apiNamespace + "." + serviceName.Replace("-", "");
					var gen = new SwaggerToCSharpClientGenerator(doc, settings);
					var code = gen.GenerateFile();

					/* When UseBaseUrl is true, NSwag generates relative paths with leading
					 * slashes. This is incompatible with HttpClient.BaseAddress, which only
					 * works with relative paths that do not begin with a slash. We want to
					 * fix this even if we're not manipulating the base URL. The generated
					 * code looks like this:
					 * 
					 * urlBuilder_.Append(BaseUrl != null ? BaseUrl.TrimEnd('/') : "").Append("/methodName");
					 * 
					 * Change it to look like this:
					 * 
					 * urlBuilder_.Append(BaseUrl != null ? BaseUrl.EndsWith("/") ? BaseUrl : BaseUrl + "/" : "").Append("methodName");
					 *
					 * See https://github.com/RSuter/NSwag/issues/1850.
					 */
					if (settings.UseBaseUrl)
					{
						code = code.Replace("BaseUrl.TrimEnd('/')",
							"BaseUrl.EndsWith(\"/\") ? BaseUrl : BaseUrl + \"/\"");
						foreach (var path in doc.Paths.Keys)
						{
							code = code.Replace('"' + path + '"', '"' + path.TrimStart('/') + '"');
						}
					}

					/* NSwag uses the longest common prefix of the method URLs as the
					 * base URL, and the relative URLs are only the parts that differ.
					 * We want to control where the URLs are split between base and
					 * relative path. */
					if (!string.IsNullOrEmpty(api.BaseUrl) && doc.BaseUrl.StartsWith(api.BaseUrl))
					{
						var pathSegment = doc.BaseUrl.Substring(api.BaseUrl.Length).Trim('/');
						if (pathSegment.Length > 0)
						{
							/* Change all relative paths to include the path segment. */
							foreach (var path in doc.Paths.Keys.Select(o => o.TrimStart('/')))
							{
								code = code.Replace('"' + path + '"', '"' + pathSegment + '/' + path + '"');
							}

							/* Remove the path segment from the base URL. */
							if (settings.UseBaseUrl)
							{
								if (api.KeepBaseUrl)
								{
									code = code.Replace(doc.BaseUrl, api.BaseUrl);
								}
								else
								{
									code = code.Replace("'" + doc.BaseUrl + '"', "null");
								}
							}
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
			return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj, SerializerSettings), SerializerSettings);
		}
	}
}
