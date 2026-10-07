using System;

namespace whr_wpf.View
{
	public static class MoneyDisplayFormatter
	{
		/// <summary>
		/// お金に単位付与
		/// </summary>
		/// <param name="amount">金額(10万円単位)</param>
		/// <returns></returns>
		public static string Format(long amount)
		{
			long trueAmount = Math.Abs(amount * 10_0000);

			return $"{ConvJapaneseNumeral(trueAmount)}円";
		}

		/// <summary>
		/// 日本の命数法に変換
		/// </summary>
		/// <param name="num">数字()</param>
		/// <returns></returns>
		private static string ConvJapaneseNumeral(long num)
		{
			if (num < 0) { throw new ArgumentException("数値は0以上を指定してください"); }

			int ichi = (int)(num % 1_0000),
				man = (int)(num % 1_0000_0000 / 1_0000),
				oku = (int)(num % 1_0000_0000_0000 / 1_0000_0000),
				cho = (int)(num % 1_0000_0000_0000_0000 / 1_0000_0000_0000);

			string result = (cho > 0 ? $"{cho}兆" : "")
				+ (oku > 0 ? $"{oku}億" : "")
				+ (man > 0 ? $"{man}万" : "")
				+ (ichi > 0 ? $"{ichi}" : "");
			if (ichi == 0 && man == 0 && oku == 0 && cho == 0) { result = "0"; }

			return result;
		}

	}
}
