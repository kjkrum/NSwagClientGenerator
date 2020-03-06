# NSwagClientGenerator
Inject NSwag REST clients directly into your .NET project build. Code is generated into the target project's intermediate build path so it normally won't be checked into source control.

As suggested by the 0.x version number, this project is still in development. However, I've been using 0.3.x in production and it's working well. I'm releasing it for public comment before calling it 1.0 and publishing it to NuGet.org. Meanwhile, you'll need to build and publish it to [your own NuGet feed](https://docs.microsoft.com/en-us/nuget/hosting-packages/local-feeds).

This generator is designed for ease of use and rapid iteration. When you add the NuGet package to your project, it creates a default config file. Edit the `Apis` section according to your needs. It will look something like this:

```json
{
	"ServiceDoc": "https://yourdomain/live/api/swagger/v1/methods/{0}",
	"Services": [ "Erp.BO.SalesOrderSvc" ],
	"Namespace": "Epicor",
	"BasePath": "/live",
	"KeepBaseUrl": false,
	"UserName": "user",
	"Password": "p@ss",
	"IgnoreInvalidCert": true
}
```
## API Config Quick Reference
* `ServiceDoc`: The URL of the API's Swagger documents. `{0}` will be replaced by each of the clients named in `Services`.
* `Services`: A list of services you want to generate clients for.
* `Namespace`: The client namespace prefix. Each client is generated into its own namespace to avoid collisions between generated types.
* `Basepath`: The part of the service method path that you do *not* want embedded in the client. This project includes a workaround for [NSwag #1837](https://github.com/RSuter/NSwag/issues/1837).
* `KeepBaseUrl`: Whether the base URL has a hard-coded default value (derived from the service URL) when using the NSwag `UseBaseURL` option.
* `UserName`: The user name used to access `ServiceDoc`. Does not appear in generated code.
* `Password`: The password used to access `ServiceDoc`. Does not appear in generated code.
* `BearerToken`: Alternative to `UserName` and `Password`. Does not appear in generated code.
* `IgnoreInvalidCert`: Ignore invalid SSL certificate on the server hosting `ServiceDoc`.
* `ConvertNumbersToDecimal`: Use `decimal` for numeric properties and parameters. See [NSwag #1814](https://github.com/RSuter/NSwag/issues/1814).
* `IgnoreRequired`: Clears the "required" section of the API spec. See below for an explanation.

The remainder of the config file gives you complete control over the NSwag generator settings. Clients are only regenerated if you modify the config file or do a full rebuild.

I've used this to generate clients for the Epicor ERP and Magento REST APIs. Please let me know if it works for you!

## Swagger 2.0 and Nullability

Swagger 2.0 has no way to express nullability. Strictly speaking, this means that properties cannot be null. This is obviously a major limitation, and implementors have worked around it in various ways.

NSwag treats Swagger 2.0 properties as nullable if and only if they are not declared as required. But some APIs just ignore the nullability issue and send null values for required properties. NSwag will happily generate clients for these APIs, but the clients will explode at runtime when they encounter null values. The `IgnoreRequired` option is a hack to force these APIs to conform to NSwag's hack. The only solution that isn't a hack is for these APIs to upgrade their specs to OpenAPI 3.0.
 