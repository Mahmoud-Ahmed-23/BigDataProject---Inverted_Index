using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BigData.BLL.Services.Search
{
	public class SearchService
	{
		private readonly InvertedIndexLoaderService _invertedIndexLoaderService;

		public SearchService(
			InvertedIndexLoaderService invertedIndexLoaderService)
		{
			_invertedIndexLoaderService = invertedIndexLoaderService;
		}

		public List<string> Search(string query)
		{
			string indexFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files", "inverted_index.txt");

			// تحميل الـ Inverted Index
			var invertedIndex = _invertedIndexLoaderService.LoadIndex(indexFilePath);

			// تقسيم الاستعلام إلى كلمات فردية
			var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (keywords.Length == 1)
			{
				var word = keywords[0].ToLower();

				return invertedIndex.ContainsKey(word)
					? invertedIndex[word].Select(x => x.Link).ToList()
					: new List<string>();
			}
			else if (keywords.Length == 2)
			{
				var firstKeyword = keywords[0].ToLower();
				var secondKeyword = keywords[1].ToLower();
				return SearchPhrase(invertedIndex, firstKeyword, secondKeyword);
			}

			// لو أكتر من كلمتين، نرجع فاضي مؤقتًا
			return new List<string>();
		}



		private List<string> SearchPhrase(Dictionary<string, List<(string Link, int Count)>> invertedIndex, string word1, string word2)
		{
			if (!invertedIndex.ContainsKey(word1) || !invertedIndex.ContainsKey(word2))
				return new List<string>();

			var word1Links = invertedIndex[word1].Select(x => x.Link).ToHashSet();
			var word2Links = invertedIndex[word2].Select(x => x.Link).ToHashSet();

			// نرجع الصفحات اللي فيها الكلمتين معًا (مش شرط ترتيب معين هنا)
			return word1Links.Intersect(word2Links).ToList();
		}

	}
}
