using BigData.BLL.Services.Helpers;
using BigData.DAL.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;  // إضافة الـ namespace لتشفير وفك تشفير الروابط

namespace BigData.BLL.Services.Search
{
	public class SearchService
	{
		private readonly InvertedIndexLoaderService _invertedIndexLoaderService;

		public SearchService(InvertedIndexLoaderService invertedIndexLoaderService)
		{
			_invertedIndexLoaderService = invertedIndexLoaderService;
		}

		public List<string> Search(string query)
		{
			string indexFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files", "inverted_index.txt");
			var invertedIndex = _invertedIndexLoaderService.LoadIndex(indexFilePath);

			var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var visitedList = LoadVisitedUrls();

			if (keywords.Length == 1)
			{
				var word = keywords[0].ToLower();
				var resultLinks = invertedIndex.ContainsKey(word)
					? invertedIndex[word].Select(x => DecodeUrl(x.Link)).ToList() 
					: new List<string>();
				return resultLinks;
			}
			else
			{
				return SearchPhrase(invertedIndex, keywords);
			}
		}

		private List<string> SearchPhrase(Dictionary<string, List<(string Link, int Count)>> invertedIndex, string[] keywords)
		{
			var resultLinks = new HashSet<string>();

			foreach (var word in keywords)
			{
				var wordLower = word.ToLower();
				if (invertedIndex.ContainsKey(wordLower))
				{
					var wordLinks = invertedIndex[wordLower].Select(x => x.Link).ToHashSet();
					resultLinks = resultLinks.Count == 0 ? wordLinks : resultLinks.Intersect(wordLinks).ToHashSet();
				}
			}

			return resultLinks.Select(DecodeUrl).ToList(); 
		}

		private List<string> LoadVisitedUrls()
		{
			string visitedUrlsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files", "visited_urls.txt");
			var visitedUrls = new List<string>();

			if (File.Exists(visitedUrlsFilePath))
			{
				visitedUrls = File.ReadAllLines(visitedUrlsFilePath).ToList();
			}

			return visitedUrls;
		}


		private string DecodeUrl(string url)
		{
			return UrlHelper.DecodeUrl(url);
		}


	}
}
