using System;
using System.Globalization;
using System.Windows.Data;

namespace whr_wpf.View.Converter
{
	/// <summary>
	/// 路線の属性を基に線の太さを求める
	/// </summary>
	class LineThicknessConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int? laneNum = value as int?;

			if (laneNum <= 0) { return 0; }

			switch (laneNum)
			{
				case 1:
					return 1;
				case 2:
					return 1.5;
				case 3:
					return 2;
				case 4:
					return 2.5;
				case 5:
				case 6:
					return 3;
				default:
					return 4;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
