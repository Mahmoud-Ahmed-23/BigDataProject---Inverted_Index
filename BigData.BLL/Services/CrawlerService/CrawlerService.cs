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

		public WebCrawlerService(ApplicationDbContext context, int maxLinks = 1000)
		{
			_context = context;
			_maxLinks = maxLinks;
			_saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Files", "Input");
			_visited = new HashSet<string>();
			_queue = new Queue<string>();
			_neighbors = new List<HashSet<int>>();

			Directory.CreateDirectory(_saveDirectory);
		}

		public async Task CrawlAndSavePages()
		{
			if (_context.Pages.Any())
			{
				Console.WriteLine("Pages already exist. Skipping crawling.");
				return;
			}

			// Initialize with seed URLs
			_queue.Enqueue("https://en.wikipedia.org/wiki/Artificial_intelligence");
			_queue.Enqueue("https://en.wikipedia.org/wiki/Machine_learning");

			// Step 1: Crawl all pages and collect links
			await CrawlPages();

			// Step 2: Parse pages into cleaned text
			await ParsePages();

			// Step 3: Build neighbor relationships
			await BuildNeighborRelationships();

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
					var links = ExtractLinks(doc);
					foreach (var link in links)
					{
						if (!_visited.Contains(link) && !_queue.Contains(link))
						{
							_queue.Enqueue(link);
						}
					}
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
					var mainContent = doc.DocumentNode.SelectSingleNode("//div[@id='mw-content-text']");
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
						FileName = fileName // ⬅️ نحفظ اسم الملف مع الصفحة
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


		private async Task BuildNeighborRelationships()
		{
			var visitedList = _visited.ToList();

			// Initialize neighbors list
			for (int i = 0; i < visitedList.Count; i++)
			{
				_neighbors.Add(new HashSet<int>());
			}

			// Build neighbor relationships
			for (int i = 0; i < visitedList.Count; i++)
			{
				var currentUrl = visitedList[i];

				try
				{
					var web = new HtmlWeb();
					var doc = await web.LoadFromWebAsync(currentUrl);

					var links = ExtractLinks(doc);
					foreach (var link in links)
					{
						if (_visited.Contains(link))
						{
							int neighborIndex = visitedList.IndexOf(link);
							_neighbors[i].Add(neighborIndex);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error processing neighbors for {currentUrl}: {ex.Message}");
				}
			}

			// Save neighbors to file
			await SaveNeighborsToFile(visitedList);
		}

		private List<string> ExtractLinks(HtmlDocument doc)
		{
			var links = new List<string>();
			var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");

			if (linkNodes != null)
			{
				foreach (var linkNode in linkNodes)
				{
					var href = linkNode.GetAttributeValue("href", string.Empty);
					if (!string.IsNullOrWhiteSpace(href)
						&& (href.StartsWith("https://") || href.StartsWith("http://")))

					{
						links.Add(href);
					}
					else if (href.StartsWith("/wiki/") && !href.Contains(":"))
					{
						string fullUrl = "https://en.wikipedia.org" + href;
						links.Add(fullUrl);
					}
				}
			}

			return links.Distinct().ToList();
		}

		private async Task SaveNeighborsToFile(List<string> visitedList)
		{
			var neighborsFilePath = Path.Combine(_saveDirectory, "neighbors.txt");

			using (var writer = new StreamWriter(neighborsFilePath))
			{
				for (int i = 0; i < _neighbors.Count; i++)
				{
					var neighborsStr = string.Join(", ", _neighbors[i]);
					await writer.WriteLineAsync($"{i}: {neighborsStr}");
				}
			}

			Console.WriteLine("Neighbors file saved successfully.");
		}
	}
}