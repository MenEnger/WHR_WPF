using System.Collections.Generic;
using whr_wpf.Model;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 都市VM
	/// </summary>
	public class TownViewModel
	{
		public string TownName { get; set; }
		public string Population { get; set; }
		public List<Line> Lines { get; set; }

		public TownViewModel(Station station)
		{
			TownName = station.Name;
			Population = $"人口 {station.Population}万人";
			Lines = station.BelongingLines;
		}
	}
}
