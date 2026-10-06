using System;
using System.Windows;
using whr_wpf.Model;

namespace whr_wpf
{
	/// <summary>
	/// 終了確認と保存・読込の利用不可案内を扱うUIユーティリティー
	/// </summary>
	public class ApplicationUtil
	{
		/// <summary>
		/// セーブ確認して終了
		/// </summary>
		/// <param name="info"></param>
		public static void Exit(GameInfo info)
		{
			MessageBoxResult x = MessageBox.Show("現在セーブ機能は利用できません。保存せずに終了しますか？", "終了確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (x != MessageBoxResult.Yes) { return; }

			Application.Current.Shutdown();
		}

		/// <summary>
		/// セーブせず終了
		/// </summary>
		public static void ForceExit()
		{
			Application.Current.Shutdown();
		}

		/// <summary>
		/// 読込機能が利用できないことを案内する
		/// </summary>
		/// <returns>読込は行わずnullを返す</returns>
		public static object LoadData()
		{
			MessageBox.Show("ロード機能は現在利用できません。新規ゲームから開始してください。", "ロード", MessageBoxButton.OK, MessageBoxImage.Information);
			return null;
		}

		/// <summary>
		/// 保存機能が利用できないことを案内する
		/// </summary>
		/// <param name="info">保存するオブジェクト</param>
		public static void SaveData(GameInfo info)
		{
			MessageBox.Show("セーブ機能は現在利用できません。", "セーブ", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
