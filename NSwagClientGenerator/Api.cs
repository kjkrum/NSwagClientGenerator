using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NSwagClientGenerator
{
	public class Api
	{
		public string ServiceDoc { get; set; } = "http://example.com/api/{0}";
		public List<string> Services { get; set; } = new List<string>();
		public string Namespace { get; set; }
		public string BasePath { get; set; }
		public bool KeepBaseUrl { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string BearerToken { get; set; }
		public bool IgnoreInvalidCert { get; set; }
		public bool ConvertNumbersToDecimal { get; set; }
        public bool IgnoreRequired { get; set; }

		public HttpClient NewClient()
		{
			var handler = new HttpClientHandler();
			if(!string.IsNullOrEmpty(UserName) && Password != null)
			{
				handler.Credentials = new NetworkCredential(UserName, Password);
			}
			if(IgnoreInvalidCert)
			{
				handler.ServerCertificateCustomValidationCallback = (senderC, cert, chain, sslPolicyErrors) => true;
			}
			var client = new HttpClient(handler, true);
			if(!string.IsNullOrEmpty(BearerToken))
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
			}
			return client;
		}
	}
}
