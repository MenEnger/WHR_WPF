using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using whr_wpf.Model;

namespace whr_wpf
{
	/// <summary>
	/// アプリケーションユーティリティー
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
		/// ファイルを読み込んで行ごとのリストにして返す
		/// </summary>
		/// <param name="path">テキストファイルのパス</param>
		/// <returns></returns>
		public static List<string> LoadFileLines(string path)
		{
			try
			{
				using StreamReader sr = new StreamReader(path);
				List<string> vs = new List<string>();
				while (sr.Peek() != -1)
				{
					vs.Add(sr.ReadLine());
				}
				return vs;
			}
			catch (IOException e)
			{
				Console.WriteLine("The file could not be read:");
				Console.WriteLine(e.Message);
				throw e;
			}
		}



		/// <summary>
		/// オブジェクトの内容をファイルから読み込み復元する
		/// </summary>
		/// <param name="path">読み込むファイル名</param>
		/// <returns>復元されたオブジェクト</returns>
		public static object LoadFromBinaryFile(string path)
		{
			throw new NotSupportedException("旧形式のセーブデータの読み込みは現在利用できません。");
		}

		/// <summary>
		/// オブジェクトの内容をファイルから読み込み復元する
		/// </summary>
		/// <returns>復元されたオブジェクト</returns>
		public static object LoadData()
		{
			MessageBox.Show("ロード機能は現在利用できません。新規ゲームから開始してください。", "ロード", MessageBoxButton.OK, MessageBoxImage.Information);
			return null;
		}

		/// <summary>
		/// オブジェクトの内容をファイルに保存する
		/// </summary>
		/// <param name="obj">保存するオブジェクト</param>
		/// <param name="path">保存先のファイル名</param>
		public static void SaveToBinaryFile(object obj, string path)
		{
			throw new NotSupportedException("セーブ機能は現在利用できません。");
		}

		/// <summary>
		/// オブジェクトの内容をファイルに保存する
		/// </summary>
		/// <param name="info">保存するオブジェクト</param>
		/// <param name="path">保存先のファイル名</param>
		public static void SaveData(GameInfo info)
		{
			MessageBox.Show("セーブ機能は現在利用できません。", "セーブ", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
