using BigData.DAL.Data;
using BigData.DAL.Entities;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web; // إضافة الـ namespace لتشفير وفك تشفير الروابط

namespace BigData.BLL.Services.CrawlerService
{
	public class WebCrawlerService
	{
		private readonly ApplicationDbContext _context;
		private readonly int _maxLinks;
		private readonly string _saveDirectory;
		private readonly HashSet<string> _visited;
		private readonly Queue<string> _queue;
		private readonly List<HashSet<int>> _neighbors;

		private readonly List<string> _startingUrls;

		public WebCrawlerService(ApplicationDbContext context, int maxLinks = 1000)
		{
			_context = context;
			_maxLinks = maxLinks;
			_saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files", "Input");
			_visited = new HashSet<string>();
			_queue = new Queue<string>();
			_neighbors = new List<HashSet<int>>();

			_startingUrls = new List<string>
			{
				"https://www.dailynewsegypt.com/",
				"https://www.geeksforgeeks.org/"
			};

			Directory.CreateDirectory(_saveDirectory);
		}

		public async Task CrawlAndSavePages()
		{
			if (_context.Pages.Any())
			{
				Console.WriteLine("Pages already exist. Skipping crawling.");
				return;
			}

			foreach (var url in _startingUrls)
			{
				_queue.Enqueue(url);
			}

			await CrawlPages();
			await ParsePages();

			Console.WriteLine($"Crawling complete. Total pages saved: {_visited.Count}");
		}

		private async Task CrawlPages()
		{
			while (_queue.Count > 0 && _visited.Count < _maxLinks)
			{
				var currentUrl = _queue.Dequeue();
				if (_visited.Contains(currentUrl)) continue;

				try
				{
					var web = new HtmlWeb();
					var doc = await web.LoadFromWebAsync(currentUrl);
					_visited.Add(currentUrl);
					Console.WriteLine($"Visited: {currentUrl}");

					// Extract and enqueue new links
					var links = ExtractLinks(doc, currentUrl);
					foreach (var link in links)
					{
						var decodedLink = HttpUtility.UrlDecode(link); // فك التشفير قبل إضافته
						if (!_visited.Contains(decodedLink) && !_queue.Contains(link))
						{
							_queue.Enqueue(link);
						}
					}

					// Delay to avoid overloading the server
					await Task.Delay(2000); // Delay 2 seconds between requests
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error processing {currentUrl}: {ex.Message}");
				}
			}
		}

		private async Task ParsePages()
		{
			var visitedList = _visited.ToList();

			foreach (var url in visitedList)
			{
				try
				{
					var web = new HtmlWeb();
					var doc = await web.LoadFromWebAsync(url);

					// Extract main content
					// Extract main content (more generic)
					var mainContent = doc.DocumentNode.SelectSingleNode("//article")
								   ?? doc.DocumentNode.SelectSingleNode("//main")
								   ?? doc.DocumentNode.SelectSingleNode("//body");

					var rawText = mainContent?.InnerText ?? string.Empty;


					// Clean text
					string cleanedText = Regex.Replace(rawText, @"[^a-zA-Z\s]", " ");
					cleanedText = Regex.Replace(cleanedText, @"(?<=[.!?])\s+", "\n").Trim();

					// Save to database
					var fileName = Guid.NewGuid().ToString() + ".txt";
					var filePath = Path.Combine(_saveDirectory, fileName);
					await File.WriteAllTextAsync(filePath, cleanedText);

					var page = new Page
					{
						Url = url,
						Content = cleanedText,
						FileName = fileName
					};

					_context.Pages.Add(page);

					// 🔁 Save to file with GUID
					await File.WriteAllTextAsync(filePath, cleanedText);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error parsing {url}: {ex.Message}");
				}
			}

			await _context.SaveChangesAsync();
		}

		private List<string> ExtractLinks(HtmlDocument doc, string baseUrl)
		{
			var links = new List<string>();
			var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");

			if (linkNodes != null)
			{
				foreach (var linkNode in linkNodes)
				{
					var href = linkNode.GetAttributeValue("href", string.Empty);
					if (!string.IsNullOrWhiteSpace(href))
					{
						var fullUrl = BuildFullUrl(href, baseUrl);
						if (!string.IsNullOrEmpty(fullUrl))
						{
							links.Add(fullUrl);
						}
					}
				}
			}

			return links.Distinct().ToList();
		}

		private string BuildFullUrl(string href, string baseUrl)
		{
			// إذا كان الرابط يبدأ بـ http أو https نتركه كما هو
			if (href.StartsWith("https://") || href.StartsWith("http://"))
			{
				return HttpUtility.UrlDecode(href); // فك التشفير هنا
			}
			else if (href.StartsWith("www."))
			{
				var fullUrl = "https://" + href;
				return HttpUtility.UrlDecode(fullUrl); // فك التشفير هنا
			}
			else if (href.StartsWith("/"))
			{
				var fullUrl = baseUrl.TrimEnd('/') + href;
				return HttpUtility.UrlDecode(fullUrl); // فك التشفير هنا
			}
			return string.Empty;
		}
	}
}
