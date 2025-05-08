using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigData.BLL.Services.Helpers
{
	public static class UrlHelper
	{
		public static string EncodeUrl(string url)
		{
			return url.Replace("https://", "")
					  .Replace("/", "_slash_")
					  .Replace("\\", "_double_back_slash_")
					  .Replace(":", "_colon_")
					  .Replace("*", "_asterisk_")
					  .Replace("?", "_question_")
					  .Replace("\"", "_double_quote_")
					  .Replace("<", "_less_than_")
					  .Replace(">", "_greater_than_")
					  .Replace("|", "_pipe_");
		}

		public static string DecodeUrl(string url)
		{
			return url.Replace("_slash_", "/")
								   .Replace("_double_back_slash_", "\\")
								   .Replace("_colon_", ":")
								   .Replace("_asterisk_", "*")
								   .Replace("_question_", "?")
								   .Replace("_double_quote_", "\"")
								   .Replace("_less_than_", "<")
								   .Replace("_greater_than_", ">")
								   .Replace("_pipe_", "|");
		}
	}

}
