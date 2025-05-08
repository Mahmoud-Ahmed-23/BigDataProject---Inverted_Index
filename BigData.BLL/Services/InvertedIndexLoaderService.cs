using BigData.DAL.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;  

namespace BigData.BLL.Services
{
	public class InvertedIndexLoaderService
	{
		private readonly ApplicationDbContext _context;

		public InvertedIndexLoaderService(ApplicationDbContext context)
		{
			_context = context;
		}

		public Dictionary<string, List<(string Link, int Count)>> LoadIndex(string filePath)
		{
			if (!File.Exists(filePath))
			{
				Console.WriteLine("Inverted index file not found. Generating...");
				
				return new Dictionary<string, List<(string, int)>>();
			}

			var invertedIndex = new Dictionary<string, List<(string Link, int Count)>>();

			foreach (var line in File.ReadLines(filePath))
			{
				if (string.IsNullOrWhiteSpace(line)) continue;

				var parts = line.Split('\t', 2); 
				if (parts.Length != 2) continue;

				var word = parts[0].Trim();
				var entries = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries);

				var linksWithCounts = new List<(string Link, int Count)>();

				foreach (var entry in entries)
				{
					var linkParts = entry.Split(':', 2);
					if (linkParts.Length == 2)
					{
						var link = linkParts[0].Trim();
						if (int.TryParse(linkParts[1], out int count))
						{
							var decodedLink = DecodeUrl(link);
							linksWithCounts.Add((decodedLink, count));
						}
					}
				}

				if (linksWithCounts.Any())
				{
					invertedIndex[word] = linksWithCounts;
				}
			}

			return invertedIndex;
		}

		private string DecodeUrl(string url)
		{
			return HttpUtility.UrlDecode(url); 
		}

		
	}
}
