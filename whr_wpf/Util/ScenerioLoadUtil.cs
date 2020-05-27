using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using whr_wpf.Model;
using static whr_wpf.Model.GameInfo;

namespace whr_wpf.Util
{
	public class ScenerioLoadUtil
	{
		/// <summary>
		/// ファイル読み込み
		/// </summary>
		public static GameInfo LoadFile(string baseDir)
		{
			var gameInfo = new GameInfo();

			//mod読み込み
			List<string> modLines = ApplicationUtil.LoadFileLines(Path.Combine(baseDir, "index.mod"));
			gameInfo.ScenerioVersion = int.Parse(ExtractModProperty(modLines, "version"));
			gameInfo.BasicYear = int.Parse(ExtractModProperty(modLines, "basicyear"));
			gameInfo.Season = (SeasonEnum)int.Parse(ExtractModProperty(modLines, "season"));
			gameInfo.SteamYear = int.Parse(ExtractModProperty(modLines, "steamyear"));
			gameInfo.Kamotu = (KamotsuEnum)int.Parse(ExtractModProperty(modLines, "kamotu"));
			gameInfo.FarePerKm = int.Parse(ExtractModProperty(modLines, "km"));
			gameInfo.Rpm = int.Parse(ExtractModProperty(modLines, "rpm"));
			gameInfo.LineMakeCost = int.Parse(ExtractModProperty(modLines, "linemc"));
			gameInfo.TechCost = int.Parse(ExtractModProperty(modLines, "tecc"));
			//mapは省略
			gameInfo.InfoPosi = (InfoPosiEnum)int.Parse(ExtractModProperty(modLines, "infoh"));

			var hojo = ExtractModProperty(modLines, "hojo").Split(",");
			gameInfo.HojoStartYear = int.Parse(hojo[0]);
			gameInfo.HojoEndYear = int.Parse(hojo[1]);
			gameInfo.HojoAmount = int.Parse(hojo[2]);

			gameInfo.warModeList = CreateWarModeList(ExtractModProperties(modLines, "warmode"));

			//バリデーションor値補正
			if (gameInfo.ScenerioVersion < 1)
			{
				_ = MessageBox.Show("シナリオバージョンの数値が異常です");
				ApplicationUtil.ForceExit();
			}
			if (gameInfo.BasicYear < 0)
			{
				_ = MessageBox.Show("基礎とする年の値が異常です");
				ApplicationUtil.ForceExit();
			}
			//kamotuはenum定義なので一旦見送り。他のenumもチェックを一旦見送り
			if (gameInfo.Rpm < 1)
			{
				gameInfo.Rpm = 100;
			}
			if (gameInfo.SteamYear < 1)
			{
				gameInfo.SteamYear = int.MaxValue;
			}
			if (gameInfo.LineMakeCost < 1)
			{
				gameInfo.LineMakeCost = 100;
			}
			if (gameInfo.TechCost < 1)
			{
				gameInfo.TechCost = 100;
			}

			//画像
			var bgImage = new BitmapImage(new Uri(Path.Combine(baseDir, "map.bmp"), UriKind.Relative));
			gameInfo.map = bgImage;

			//初期化

			//駅
			gameInfo.stations = CreateStationFromFile(baseDir);

			//路線
			gameInfo.lines = CreateLineFromFile(baseDir, gameInfo);
			BindLinesToStations(gameInfo);

			//乗り継ぎ
			gameInfo.longwayList = CreateLongwayFromFile(baseDir, gameInfo);

			//運転系統
			gameInfo.diagrams = CreateDiagramFromFile(baseDir, gameInfo);
			BindDiagramsToLines(gameInfo);

			//モード読み込み
			gameInfo.modes = LoadModes(modLines);
			//FIXME 仮で最初のモードの車輌などを注入しているので、ここを動的に
			gameInfo.compositions.AddRange(gameInfo.modes[0].DefautltCompositions);
			gameInfo.Money = gameInfo.modes[0].Money;
			gameInfo.Year = gameInfo.modes[0].Year;
			gameInfo.MYear = gameInfo.modes[0].MYear;

			Console.WriteLine("ファイル読み込み完了");
			return gameInfo;
		}

		/// <summary>
		/// index.modのモード部読み込み
		/// </summary>
		/// <param name="modLines"></param>
		/// <returns></returns>
		private static List<Mode> LoadModes(List<string> modLines) => CreateModeList(modLines);

		//town.csv読み込み
		private static List<Station> CreateStationFromFile(string basePath)
		{
			string csvfile = Path.Combine(basePath, "town.csv");
			TextFieldParser parser = new TextFieldParser(csvfile);
			parser.TextFieldType = FieldType.Delimited;
			parser.SetDelimiters(","); // 区切り文字はコンマ

			var stationList = new List<Station>();
			while (!parser.EndOfData)
			{
				string[] cols = parser.ReadFields(); // 1行読み込み

				var station = new Station
				{
					Name = cols[0],
					Size = (StationSize)int.Parse(cols[2]),
					X = int.Parse(cols[3]),
					Y = int.Parse(cols[4]),
					Population = int.Parse(cols[5]),
					KamotsuKibo = int.Parse(cols[6]),
				};
				stationList.Add(station);
			}
			return stationList;
		}

		//line.csv読み込み
		private static List<Line> CreateLineFromFile(string basePath, GameInfo gameInfo)
		{
			string csvfile = Path.Combine(basePath, "line.csv");
			TextFieldParser parser = new TextFieldParser(csvfile);
			parser.TextFieldType = FieldType.Delimited;
			parser.SetDelimiters(","); // 区切り文字はコンマ

			var lineList = new List<Line>();

			while (!parser.EndOfData)
			{
				string[] cols = parser.ReadFields(); // 1行読み込み

				var line = new Line()
				{
					Name = cols[0],
					Start = gameInfo.stations[int.Parse(cols[1])],
					End = gameInfo.stations[int.Parse(cols[2])],
					grade = (LineGrade)int.Parse(cols[3]),
					Distance = int.Parse(cols[4]),
					propertyType = (LinePropertyType)int.Parse(cols[5])
				};
				lineList.Add(line);

			}
			return lineList;
		}

		//路線を駅に紐付け
		private static void BindLinesToStations(GameInfo gameInfo)
		{
			foreach (var line in gameInfo.lines)
			{
				line.Start.BelongingLines.Add(line);
				line.End.BelongingLines.Add(line);
			}
		}

		//longway.csv読み込み
		private static List<Longway> CreateLongwayFromFile(string basePath, GameInfo gameInfo)
		{
			string csvfile = Path.Combine(basePath, "longway.csv");
			TextFieldParser parser = new TextFieldParser(csvfile)
			{
				TextFieldType = FieldType.Delimited
			};
			parser.SetDelimiters(","); // 区切り文字はコンマ

			var longwayList = new List<Longway>();

			while (!parser.EndOfData)
			{
				string[] cols = parser.ReadFields(); // 1行読み込み

				var longway = new Longway()
				{
					route = new List<Line>(),
					start = gameInfo.stations[int.Parse(cols[0])],
					end = gameInfo.stations[int.Parse(cols[1])],
					isKamotuOperated = true
				};

				for (int idx = 2; ; idx++) //経由地
				{
					var j = int.Parse(cols[idx]);
					if (j == -1)
					{
						break;
					}
					else if (j == -2)
					{
						longway.isKamotuOperated = false;
						break;
					}
					else
					{
						longway.route.Add(gameInfo.lines[j - 1]);
					}
				}
				longwayList.Add(longway);
			}
			return longwayList;
		}

		//diagram.csv読み込み
		private static List<KeitoDiagram> CreateDiagramFromFile(string basePath, GameInfo gameInfo)
		{
			string csvfile = Path.Combine(basePath, "diagram.csv");
			TextFieldParser parser = new TextFieldParser(csvfile)
			{
				TextFieldType = FieldType.Delimited
			};
			parser.SetDelimiters(","); // 区切り文字はコンマ

			var diagramList = new List<KeitoDiagram>();
			while (!parser.EndOfData)
			{
				string[] cols = parser.ReadFields(); // 1行読み込み

				var diagram = new KeitoDiagram()
				{
					route = new List<Line>(),
					Name = cols[0],
					start = gameInfo.stations[int.Parse(cols[1])],
					end = gameInfo.stations[int.Parse(cols[2])]
				};

				for (int idx = 3; ; idx++) //経由路線
				{
					var j = int.Parse(cols[idx]);
					if (j == -1)
					{
						break;
					}
					else
					{
						diagram.route.Add(gameInfo.lines[j - 1]);
					}
				}
				diagramList.Add(diagram);
			}
			return diagramList;
		}

		/// <summary>
		/// ダイアを路線に紐付け
		/// </summary>
		private static void BindDiagramsToLines(GameInfo gameInfo)
		{
			foreach (var diagram in gameInfo.diagrams)
			{
				diagram.route.ForEach(line => { line.belongingKeitoDiagrams.Add(diagram); });
			}
		}

		/// <summary>
		/// 戦時モードのリストを生成
		/// </summary>
		/// <param name="warModeStringList">戦時モードの文字列リスト</param>
		/// <returns></returns>
		public static List<WarMode> CreateWarModeList(List<string> warModeStringList)
		{
			return warModeStringList.Select(warmodeStr =>
			{
				var arr = warmodeStr.Split(',');
				return new WarMode
				{
					StartYear = int.Parse(arr[0]),
					EndYear = int.Parse(arr[1]),
					kamotsuIndex = int.Parse(arr[2]),
				};
			}).ToList();
		}

		/// <summary>
		/// モード設定をブロックごとに抽出してリストで返却
		/// </summary>
		/// <param name="modesLines"></param>
		/// <returns></returns>
		public static List<Mode> CreateModeList(List<string> modesLines)
		{
			bool isInMode = false;

			List<Mode> modeObjList = new List<Mode>();
			List<string> modeList = new List<string>();

			foreach (var line in modesLines)
			{
				if (!isInMode && line.StartsWith("#mode:"))
				{
					isInMode = true;
					modeList.Add(line);
					continue;
				}
				if (isInMode) { modeList.Add(line); }
				if (line.EndsWith("#end"))
				{
					isInMode = false;
					modeObjList.Add(CreateMode(modeList));
					modeList.Clear();
				}
			}
			return modeObjList;
		}

		private static Mode CreateMode(List<string> modeLines)
		{
			Mode mode = new Mode();

			mode.Name = ExtractModProperty(modeLines, "#mode");
			mode.Year = int.Parse(ExtractModProperty(modeLines, "year"));
			mode.Money = long.Parse(ExtractModProperty(modeLines, "money"));
			mode.Message = ExtractModProperty(modeLines, "message").Replace(',', '\n');
			mode.MYear = int.Parse(ExtractModProperty(modeLines, "myear"));

			mode.DefautltCompositions = ExtractModProperties(modeLines, "car").Select(value =>
			{
				string[] arr = value.Split(",");
				int ck = int.Parse(arr[2]); //編成規格
				bool isLinear = (ck & 32) > 0;
				bool isDiesel = (ck & 16) > 0;
				bool isElectric = (ck & 8) > 0;
				bool isSteam = (ck & 4) > 0;
				bool isNarrow = (ck & 1) > 0;
				bool isRegular = (ck & 2) > 0;
				bool isPendulum = (ck & 64) > 0;
				bool isFreeGauge = (ck & 128) > 0;

				RailTypeEnum railType = isLinear ? RailTypeEnum.LinearMotor : RailTypeEnum.Iron;

				CarGaugeEnum? carGauge =
					isLinear ? (CarGaugeEnum?)null :
					isNarrow ? CarGaugeEnum.Narrow :
					isRegular ? CarGaugeEnum.Regular :
					isFreeGauge ? CarGaugeEnum.FreeGauge :
					throw new ArgumentException("軌間が未定義です");

				PowerEnum powerSource =
					isLinear ? PowerEnum.LinearMotor :
					isElectric ? PowerEnum.Electricity :
					isDiesel ? PowerEnum.Diesel :
					isSteam ? PowerEnum.Steam :
					throw new ArgumentException("動力源が未指定です");

				return new DefautltComposition
				{
					Name = arr[0],
					BestSpeed = int.Parse(arr[1]),
					CarCount = int.Parse(arr[3]),
					HeldUnits = int.Parse(arr[4]),
					Price = int.Parse(arr[5]),
					seat = LogicUtil.ConvertSeatModToInternalId(int.Parse(arr[6])),
					Gauge = carGauge,
					Power = powerSource,
					Tilt = isPendulum ? CarTiltEnum.Pendulum : CarTiltEnum.None,
					Type = railType
				};
			}).ToList();

			return mode;
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

	}
}
