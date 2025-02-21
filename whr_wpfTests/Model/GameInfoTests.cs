using Microsoft.VisualStudio.TestTools.UnitTesting;
using whr_wpf.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace whr_wpf.Model.Tests
{
	public class DummyComposition : IComposition
	{
		public DummyComposition() { }

		public int BestSpeed
		{
			get => 60;
			set
			{

			}
		}


		public int CarCount => throw new NotImplementedException();

		public CarTiltEnum Tilt { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public CarGaugeEnum? Gauge { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public bool IsElectrified => throw new NotImplementedException();

		public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public int PassengerCapacity => throw new NotImplementedException();

		public PowerEnum Power { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public int Price => throw new NotImplementedException();

		public RailTypeEnum Type { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public int HeldUnits => 1;

		public SeatEnum? BestComfortSeat => throw new NotImplementedException();

		public void Purchase(GameInfo gameInfo, int quantity)
		{
			throw new NotImplementedException();
		}

		public void Release(int quantity)
		{
			throw new NotImplementedException();
		}

		public void Use(int quantity)
		{
			throw new NotImplementedException();
		}
	}

	[TestClass()]
	public class GameInfoTests
	{

		// テスト用の定数
		private const int ApValue = 100000;

		#region ヘルパーメソッド
		// 簡易な Station 作成ヘルパー
		private Station CreateStation(string name, int population, StationSize size = StationSize.Other)
		{
			return new Station
			{
				Name = name,
				Population = population,
				Size = size,
				X = 0,
				Y = 0
			};
		}

		// ダミーの Line 作成ヘルパー
		// ※CalcRequiredMinutes()が返す値を一定にするため、各プロパティを設定
		// ここでは、DiagramType を LimittedExpressPrior とし、
		// bestSpeed = 60、Distance = 9 とすると
		//    平均速度 = 60*9/10 = 54  で、9*60/54 = 10 分となる
		private Line CreateDummyLine(Station station1, Station station2)
		{
			var line = new Line
			{
				Start = station1,
				End = station2,
				Distance = 9,
				bestSpeed = 60,
				LaneNum = 2,
				diagram = DiagramType.LimittedExpressPrior,
				useComposition = new DummyComposition { BestSpeed = 60 }
			};
			return line;
		}

		// ダミーの Longway 作成ヘルパー
		// 経由ルートにダミーの Line を1本設定して、CalcRequiredMinutes() が 10 分となるようにする
		private Longway CreateDummyLongway(Station station1, Station station2)
		{
			var longway = new Longway
			{
				start = station1,
				end = station2,
				route = new List<Line> { CreateDummyLine(station1, station2) }
			};
			return longway;
		}
		#endregion

		[TestMethod]
		public void Test_SingleStation_NoConnections()
		{
			// Arrange
			var station = CreateStation("StationA", 50); // Population = 50 → baseShare = 5000
			var stations = new List<Station> { station };
			var longways = new List<Longway>(); // 接続なし
			var calculator = new GameInfo { };

			// Act
			var result = calculator.CalculateStationPopulationDistribution(stations, longways, ApValue);

			// Assert
			Assert.IsTrue(result.ContainsKey(station));
			// 単一の場合、比率は 1:1 となるため、新人口は Ap と同じ値となる
			Assert.AreEqual(ApValue, result[station]);
		}

		[TestMethod]
		public void Test_TwoStations_NoConnections()
		{
			// Arrange
			var stationA = CreateStation("StationA", 50);  // baseShare = 5000
			var stationB = CreateStation("StationB", 100); // baseShare = 10000
			var stations = new List<Station> { stationA, stationB };
			var longways = new List<Longway>();
			var calculator = new GameInfo {};

			// Act
			var result = calculator.CalculateStationPopulationDistribution(stations, longways, ApValue);

			// 期待値：
			// 合計シェア = 5000 + 10000 = 15000
			// stationA の新人口 = Ap * 5000 / 15000 = 約 33333
			// stationB の新人口 = Ap * 10000 / 15000 = 約 66666
			Assert.IsTrue(result.ContainsKey(stationA));
			Assert.IsTrue(result.ContainsKey(stationB));
			Assert.AreEqual((int)((long)ApValue * 5000 / 15000), result[stationA]);
			Assert.AreEqual((int)((long)ApValue * 10000 / 15000), result[stationB]);
		}

		[TestMethod]
		public void Test_SingleStation_CapitalBonus()
		{
			// Arrange
			var station = CreateStation("CapitalStation", 50, StationSize.Capital);
			var stations = new List<Station> { station };
			var longways = new List<Longway>();
			var calculator = new GameInfo { };

			// Act
			var result = calculator.CalculateStationPopulationDistribution(stations, longways, ApValue);

			// 首都の場合、基本シェア 5000 に 1% のボーナス → 5000 * 101/100 = 5050
			// 単一の場合、結果は Ap と同じ
			Assert.IsTrue(result.ContainsKey(station));
			Assert.AreEqual(ApValue, result[station]);
		}

		[TestMethod]
		public void Test_TwoStations_WithConnections()
		{
			// Arrange
			// 2駅間に、所属路線と長距離直通（Longway）の両方で接続があるケース
			var stationA = CreateStation("StationA", 50);
			var stationB = CreateStation("StationB", 100);

			// 所属路線（両駅に同じ Line を追加）
			var line = CreateDummyLine(stationA, stationB);
			stationA.BelongingLines.Add(line);
			stationB.BelongingLines.Add(line);

			// Longway 接続
			var longway = CreateDummyLongway(stationA, stationB);
			var stations = new List<Station> { stationA, stationB };
			var longways = new List<Longway> { longway };

			var calculator = new GameInfo { };

			// Act
			var result = calculator.CalculateStationPopulationDistribution(stations, longways,ApValue);

			// 以下、各駅のシェアを手計算（※CalcRequiredMinutes()は上記ダミー設定により 10 分となる）
			// 【stationA】:
			//  - 基本シェア = 50 * 100 = 5000
			//  - 所属路線: 相手駅 stationB の Population=100, 費用計算:
			//       (600/10 + 10)/10 * (100/10) = (60 + 10)/10 * 10 = 70
			//  - Longway: 同様に 70
			//  → 合計シェア = 5000 + 70 + 70 = 5140
			//
			// 【stationB】:
			//  - 基本シェア = 100 * 100 = 10000
			//  - 所属路線: 相手駅 stationA の Population=50 → (600/10+10)/10*(50/10) = 35
			//  - Longway: 同様に 35
			//  → 合計シェア = 10000 + 35 + 35 = 10070
			//
			// 全体合計 = 5140 + 10070 = 15210（15210 < 100000 なのでスケーリングは行われない）
			int totalShare = 5140 + 10070; // 15210
			int expectedA = (int)((long)ApValue * 5140 / totalShare);
			int expectedB = (int)((long)ApValue * 10070 / totalShare);

			// Assert
			Assert.IsTrue(result.ContainsKey(stationA));
			Assert.IsTrue(result.ContainsKey(stationB));
			Assert.AreEqual(expectedA, result[stationA]);
			Assert.AreEqual(expectedB, result[stationB]);
		}
	}
}