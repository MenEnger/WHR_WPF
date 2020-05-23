using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
			MessageBoxResult x = MessageBox.Show("セーブしますか？", "", MessageBoxButton.YesNoCancel);
			switch (x)
			{
				case MessageBoxResult.Yes:
					SaveData(info);
					break;
				case MessageBoxResult.Cancel:
					return;
			}

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
		/// index.modから指定したプロパティの文字を抽出
		/// </summary>
		/// <param name="modLines"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public static string ExtractModProperty(List<string> modLines, string property)
		{
			return modLines.Find(line => line.StartsWith(property)).Split(":")[1];
		}

		/// <summary>
		/// index.modから指定したプロパティの文字を抽出し配列で返却
		/// </summary>
		/// <param name="modLines"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public static List<string> ExtractModProperties(List<string> modLines, string property)
		{
			return modLines
				.Where(line => line.StartsWith(property))
				.Select(line => line.Split(":")[1])
				.ToList();
		}

		/// <summary>
		/// オブジェクトの内容をファイルから読み込み復元する
		/// </summary>
		/// <param name="path">読み込むファイル名</param>
		/// <returns>復元されたオブジェクト</returns>
		public static object LoadFromBinaryFile(string path)
		{
			FileStream fs = new FileStream(path,
				FileMode.Open,
				FileAccess.Read);
			BinaryFormatter f = new BinaryFormatter();
			//読み込んで逆シリアル化する
			object obj = f.Deserialize(fs);
			fs.Close();

			return obj;
		}

		/// <summary>
		/// オブジェクトの内容をファイルから読み込み復元する
		/// </summary>
		/// <returns>復元されたオブジェクト</returns>
		public static object LoadData() => LoadFromBinaryFile("save.dat");

		/// <summary>
		/// オブジェクトの内容をファイルに保存する
		/// </summary>
		/// <param name="obj">保存するオブジェクト</param>
		/// <param name="path">保存先のファイル名</param>
		public static void SaveToBinaryFile(object obj, string path)
		{
			FileStream fs = new FileStream(path,
				FileMode.Create,
				FileAccess.Write);
			BinaryFormatter bf = new BinaryFormatter();
			//シリアル化して書き込む
			bf.Serialize(fs, obj);
			fs.Close();
		}

		/// <summary>
		/// オブジェクトの内容をファイルに保存する
		/// </summary>
		/// <param name="info">保存するオブジェクト</param>
		/// <param name="path">保存先のファイル名</param>
		public static void SaveData(GameInfo info) => SaveToBinaryFile(info, "save.dat");
	}
}
