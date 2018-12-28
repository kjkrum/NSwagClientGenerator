using NSwag;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NSwagClientGenerator
{
	public static class Extensions
	{
		public static void SetBasePath(this SwaggerDocument doc, String basePath)
		{
			if (basePath == doc.BasePath)
			{
				return;
			}
			if (basePath == null)
			{
				throw new ArgumentNullException();
			}
			var fullPath = doc.BasePath + doc.Paths.Keys.Select(o => o.Substring(0, o.LastIndexOf('/'))).First();
			if (!fullPath.StartsWith(basePath))
			{
				throw new ArgumentException();
			}
			var relPath = fullPath.Substring(basePath.Length);
			if (relPath.Length > 0 && !relPath.StartsWith("/"))
			{
				throw new ArgumentException();
			}
			foreach (var oldPath in new List<string>(doc.Paths.Keys))
			{
				doc.Paths.Add(relPath + oldPath.Substring(oldPath.LastIndexOf('/')), doc.Paths[oldPath]);
				doc.Paths.Remove(oldPath);
			}
			doc.BasePath = basePath;
		}
	}
}
