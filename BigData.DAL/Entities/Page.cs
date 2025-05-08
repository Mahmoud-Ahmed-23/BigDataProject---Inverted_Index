using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigData.DAL.Entities
{
	public class Page
	{
		public int Id { get; set; }
		public string Url { get; set; }
		public string Content { get; set; }
		public string FileName { get; set; }
	}

}
