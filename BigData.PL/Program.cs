using BigData.BLL.Services;
using BigData.BLL.Services.CrawlerService;
using BigData.BLL.Services.Search;
using BigData.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BigData.PL
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews();

			builder.Services.AddScoped<WebCrawlerService>();
			builder.Services.AddScoped<InvertedIndexLoaderService>();
			builder.Services.AddScoped<SearchService>();

			builder.Services.AddDbContext<ApplicationDbContext>(optionsBuilder =>
			{
				optionsBuilder.UseSqlServer("Server = .; Database = BigData; Trusted_Connection = True; TrustServerCertificate = True");
			});

			var app = builder.Build();

			using (var scope = app.Services.CreateScope())
			{
				var crawler = scope.ServiceProvider.GetRequiredService<WebCrawlerService>();
				await crawler.CrawlAndSavePages(); // هينفذ الكراول أول ما المشروع يشتغل
			}

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

			app.Run();
		}
	}
}

