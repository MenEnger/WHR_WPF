using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using whr_wpf.Model;


namespace whr_wpf.ViewModel.Component
{
	/// <summary>
	/// 待避線リスト表示用の部品
	/// </summary>
	public class TaihiViewComponent : AbstractViewComponent, IEquatable<TaihiViewComponent>
	{
		public TaihisenEnum Enum { get; set; }

		public static List<TaihiViewComponent> CreateViewList()
		{
			var taihiList = new List<TaihiViewComponent>();
			foreach (TaihisenEnum taihi in System.Enum.GetValues(typeof(TaihisenEnum)))
			{
				taihiList.Add(new TaihiViewComponent { Caption = taihi.ToName(), Enum = taihi });
			}
			return taihiList;
		}

		public bool Equals([AllowNull] TaihiViewComponent other)
		{
			if (other is null) return false;
			return Caption == other.Caption && Enum == other.Enum;
		}
	}

}
