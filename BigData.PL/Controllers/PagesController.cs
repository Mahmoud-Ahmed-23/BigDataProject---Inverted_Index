using BigData.BLL.Services.CrawlerService;
using BigData.BLL.Services.Search;
using BigData.DAL.Data;
using Microsoft.AspNetCore.Mvc;

public class PagesController : Controller
{
	private readonly ApplicationDbContext _context;
	private readonly SearchService _searchService;
	private readonly WebCrawlerService _crawlerService;

	public PagesController(ApplicationDbContext context, SearchService searchService, WebCrawlerService crawlerService)
	{
		_context = context;
		_searchService = searchService;
		_crawlerService = crawlerService;
	}

	public IActionResult Index()
	{
		return View();
	}

	public IActionResult Search(string query)
	{
		var results = _searchService.Search(query);
		ViewData["Query"] = query;
		ViewData["Results"] = results;
		return View("Index");
	}

	public IActionResult ShowPageContent(string url, string word)
	{
		var page = _context.Pages.FirstOrDefault(p => p.Url == url);
		if (page != null)
		{
			if (!string.IsNullOrEmpty(word))
			{
				// تمييز الكلمة في المحتوى
				page.Content = HighlightWordInContent(page.Content, word);
			}
			return View("Page", page);
		}

		return NotFound();
	}

	private string HighlightWordInContent(string content, string word)
	{
		if (string.IsNullOrEmpty(word))
			return content;

		// إضافة العلامات لتحديد الكلمة
		string highlightedWord = $"<span style='background-color: yellow; font-weight: bold;'>{word}</span>";
		return content.Replace(word, highlightedWord, StringComparison.OrdinalIgnoreCase);
	}
}
