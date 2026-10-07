using System;
using System.Collections.Generic;
using System.Linq;

namespace whr_wpf.Model
{
	public partial class GameInfo
	{
		// オリジナル1.52: include.as の phr、start.as の *s__。
		// Apは技術開発の人口比率を適用する前の総人口。都市人口の合計とは別に保持する。
		private int CalculatePopulationRatio()
		{
			bool[] technologies =
			{
				isDevelopedDynamicSignal, isDevelopedFreeGauge, isDevelopedMachineTilt,
				isDevelopedDualSeat, isDevelopedRetructableLong, isDevelopedRichCross,
				isDevelopedCarTiltPendulum, isDevelopedAutoGate,
				isDevelopedConvertibleCross, isDevelopedBlockingSignal
			};
			int ratio = 50 + technologies.Count(developed => developed) * 2;
			if (genkaiDenki > 0) { ratio += 2; }
			if (genkaiKidosha > 0) { ratio += 2; }
			if (genkaiLinear > 0) { ratio += 3; }
			if (genkaiDenki > 200 || genkaiKidosha > 200) { ratio += 3; }
			if (genkaiLinear >= 990) { ratio += 10; }
			return ratio;
		}

		private void InitializePopulation()
		{
			Ap = checked((int)(stations.Sum(station => (long)station.Population) * 100 / CalculatePopulationRatio()));
		}

		private void UpdatePopulation()
		{
			// main.as 550-555の成長率の切り替えは、モード期限myearから100年後を基準とする。
			if (Difficulty == DifficultyLevelEnum.VeryEasy || Year < (long)MYear + 100)
			{
				if (Difficulty.HasValue) { Ap = checked((int)((long)Ap * 253 / 250)); }
			}
			else if (Difficulty == DifficultyLevelEnum.Easy)
			{
				Ap = checked((int)((long)Ap * 201 / 200));
			}

			var distribution = CalculateStationPopulationDistribution(stations, longwayList, Ap);
			foreach (var station in stations) { station.Population = distribution[station]; }
			// 都市ごとの端数切り捨てや人口比率の変化をApに戻さない。
		}

		/// <summary>
		/// オリジナル1.52の年次人口配分（main.as 581-667）。
		/// allPopulationは技術開発による人口比率を適用する前の総人口。
		/// </summary>
		public Dictionary<Station, int> CalculateStationPopulationDistribution(
			IEnumerable<Station> stations, IEnumerable<Longway> longwayList, int allPopulation)
		{
			if (allPopulation < 0) { throw new ArgumentOutOfRangeException(nameof(allPopulation)); }
			var stationList = stations.ToList();
			var longways = longwayList.ToList();
			var shares = new Dictionary<Station, long>();
			foreach (var station in stationList)
			{
				long share = (long)station.Population * 100;
				// 直接接続の加算は、路線運行に編成を投入している場合だけ（lct > 0）。
				foreach (var line in station.BelongingLines)
				{
					if (!line.IsExist || line.useCompositionNum <= 0) { continue; }
					int minutes = line.CalcAverageRequireMinutes();
					if (minutes <= 0) { continue; }
					var otherStation = line.Start == station ? line.End : line.Start;
					share += CalculateConnectedPopulationShare(minutes, otherStation.Population);
				}
				foreach (var longway in longways.Where(path => path.start == station || path.end == station))
				{
					long minutes = 0;
					bool operated = longway.route.Count > 0;
					foreach (var line in longway.route)
					{
						// 原作のhocは、投入編成のある系統だけを運行本数に加える。
						long trips = line.runningPerDay + line.belongingKeitoDiagrams
							.Where(diagram => diagram.useCompositionNum > 0)
							.Sum(diagram => (long)diagram.runningPerDay);
						if (!line.IsExist || trips <= 0) { operated = false; break; }
						int segmentMinutes = line.CalcAverageRequireMinutes();
						if (segmentMinutes <= 0) { operated = false; break; }
						minutes += segmentMinutes;
					}
					if (!operated) { continue; }
					var otherStation = longway.start == station ? longway.end : longway.start;
					share += CalculateConnectedPopulationShare(minutes, otherStation.Population);
				}
				// 17はオリジナルの縮小表示時の首都。乗換駅の16にはボーナスを付けない。
				if (station.Size == StationSize.Capital || (int)station.Size == 17)
				{
					share = share / 10 * 101 / 10;
				}
				shares.Add(station, share);
			}

			long totalShare = shares.Values.Sum();
			// 合計が100000以上なら、合計と各都市のシェアをそれぞれ整数で半減する。
			while (totalShare >= 100000)
			{
				totalShare /= 2;
				foreach (var station in stationList) { shares[station] /= 2; }
			}
			int ratio = CalculatePopulationRatio();
			return shares.ToDictionary(pair => pair.Key, pair => totalShare == 0
				? 1
				: Math.Max(checked((int)((long)allPopulation * ratio * pair.Value / 100 / totalShare)), 1));
		}

		private static long CalculateConnectedPopulationShare(long minutes, int population)
		{
			// HSP2の整数計算の順序を維持する。乗除算の並べ替えで端数が変わる。
			return (600 / minutes + 10) / 10 * population / 10;
		}
	}
}
