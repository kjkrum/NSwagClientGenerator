﻿using System;
using System.IO;

namespace NSwagClientGenerator
{
	class Program
	{
		static int Main(string[] args)
		{
			try
			{
				if (args.Length != 2)
				{
					var exe = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
					throw new Exception("Usage: " + exe + " <config-file> <output-file>");
				}
				new Generator(args[0], args[1]).Start();
				return 0;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				return 1;
			}
		}
	}
}
