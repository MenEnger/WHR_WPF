using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using whr_wpf.Model;

namespace whr_wpf.View.Converter
{
	/// <summary>
	/// 路線の属性を基に表示色を求める
	/// </summary>
	class LineColorConverter : IMultiValueConverter
	{

		/// <summary>
		/// 路線を基に表示色を導出
		/// </summary>
		/// <param name="values"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns>
		/// <list type="bullet">
		/// <item>非電化：白</item>
		/// <item>電化：黄色</item>
		/// <item>リニア：赤</item>
		/// <item>その他：透明</item>
		/// </list></returns>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			RailTypeEnum? type = values[0] as RailTypeEnum?;
			bool? isElectrified = values[1] as bool?;


			if (type is null || isElectrified is null) { return Brushes.Transparent; }

			if (type == RailTypeEnum.LinearMotor) { return Brushes.Red; }
			else if (type == RailTypeEnum.Iron)
			{
				if (isElectrified.HasValue)
				{
					return isElectrified.Value ? Brushes.Yellow : Brushes.White;
				}
			}
			return Brushes.Transparent;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
