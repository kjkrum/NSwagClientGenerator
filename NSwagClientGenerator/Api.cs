using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NSwagClientGenerator
{
	public class Api
	{
		public string Namespace { get; set; }
		public string BaseUrl { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public string BearerToken { get; set; }
		public bool IgnoreInvalidCert { get; set; }
		public string BasePath { get; set; }
		public List<string> Services { get; set; } = new List<string>();

		public HttpClient NewClient()
		{
			var handler = new HttpClientHandler();
			if(!string.IsNullOrEmpty(UserName))
			{
				handler.Credentials = new NetworkCredential(UserName, Password);
			}
			if(IgnoreInvalidCert)
			{
				handler.ServerCertificateCustomValidationCallback = (senderC, cert, chain, sslPolicyErrors) => true;
			}
			var client = new HttpClient(handler, true)
			{
				BaseAddress = new System.Uri(BaseUrl)
			};
			if(!string.IsNullOrEmpty(BearerToken))
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
			}
			return client;
		}
	}
}
