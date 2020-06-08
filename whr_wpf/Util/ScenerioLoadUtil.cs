using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using whr_wpf.Model;
using static whr_wpf.Model.GameInfo;
using static whr_wpf.Model.Mode;

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
			gameInfo.Modes = LoadModes(modLines);

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

				for (int idx = 2; ; idx++) //経由路線
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

		/// <summary>
		/// モード設定文字列からモードオブジェクト作成
		/// </summary>
		/// <param name="modeLines"></param>
		/// <returns></returns>
		private static Mode CreateMode(List<string> modeLines)
		{
			Mode mode = new Mode();

			//ゲーム設定
			mode.Name = ExtractModProperty(modeLines, "#mode");
			mode.Year = int.Parse(ExtractModProperty(modeLines, "year"));
			mode.Money = long.Parse(ExtractModProperty(modeLines, "money"));
			mode.Message = ExtractModProperty(modeLines, "message").Replace(',', '\n');
			mode.MYear = int.Parse(ExtractModProperty(modeLines, "myear"));
			mode.genkaiJoki = ParseIntOrNull(ExtractModProperty(modeLines, "steam")) ?? 40;
			mode.genkaiDenki = ParseIntOrNull(ExtractModProperty(modeLines, "elect"));
			mode.genkaiKidosha = ParseIntOrNull(ExtractModProperty(modeLines, "diesel"));
			mode.genkaiLinear = ParseIntOrNull(ExtractModProperty(modeLines, "linear"));
			var tecno = ParseIntOrNull(ExtractModProperty(modeLines, "tecno"));
			if (tecno.HasValue) {
				int tecnoV = tecno.Value;
				if ((tecnoV & 256) > 0) //動的信号
				{
					mode.isDevelopedDynamicSignal = true;
					mode.isDevelopedFreeGauge = true;
					mode.isDevelopedMachineTilt = true;
					mode.isDevelopedDualSeat = true;
					mode.isDevelopedRetructableLong = true;
					mode.isDevelopedRichCross = true;
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 4) > 0) //フリーゲージトレイン
				{
					mode.isDevelopedFreeGauge = true;
					mode.isDevelopedMachineTilt = true;
					mode.isDevelopedDualSeat = true;
					mode.isDevelopedRetructableLong = true;
					mode.isDevelopedRichCross = true;
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 512) > 0) //機械式車体傾斜装置
				{
					mode.isDevelopedMachineTilt = true;
					mode.isDevelopedDualSeat = true;
					mode.isDevelopedRetructableLong = true;
					mode.isDevelopedRichCross = true;
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 8) > 0) //デュアルシート
				{
					mode.isDevelopedDualSeat = true;
					mode.isDevelopedRetructableLong = true;
					mode.isDevelopedRichCross = true;
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 32) > 0) //収納式ロングシート
				{
					mode.isDevelopedRetructableLong = true;
					mode.isDevelopedRichCross = true;
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 16) > 0) //豪華クロスシート
				{
					mode.isDevelopedRichCross = true;
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 1) > 0) //振り子式車体傾斜装置
				{
					mode.isDevelopedCarTiltPendulum = true;
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 128) > 0) //自動改札機
				{
					mode.isDevelopedAutoGate = true;
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 64) > 0) //転換クロスシート
				{
					mode.isDevelopedConvertibleCross = true;
					mode.isDevelopedBlockingSignal = true;
				}
				else if ((tecnoV & 2) > 0) //閉塞信号
				{
					mode.isDevelopedBlockingSignal = true;
				}
			}
			int[] people = ExtractModProperty(modeLines, "people")?.Split(",").Select(it => int.Parse(it)).Take(2).ToArray() ?? new int[] { 1,1};
			if (people.Count() == 2) {
				mode.peopleNume = people[0];
				mode.peopleDenom = people[1];
			}


			//デフォルト編成
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

			//路線
			List<LineDefaultSetting> lineDefaultSettings = new List<LineDefaultSetting>();
			bool[] ltn = ExtractModProperty(modeLines, "ltn")?.Split(",").Select(flg => int.Parse(flg) != 0).ToArray() ?? new bool[0];
			int[] lk = ExtractModProperty(modeLines, "lk")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[ltn.Length];
			int[] lbs = ExtractModProperty(modeLines, "lbs")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[ltn.Length];
			int[] las = ExtractModProperty(modeLines, "las")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[ltn.Length];
			int[] lwe = ExtractModProperty(modeLines, "lwe")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[ltn.Length];
			int[] lts = ExtractModProperty(modeLines, "lts")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[ltn.Length];
			int[] ulc = ExtractModProperty(modeLines, "ulc")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? null;
			int[] ulcr = ExtractModProperty(modeLines, "ulcr")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[ltn.Length];
			for (int i = 0; i < ltn.Length; i++)
			{
				LineDefaultSetting setting = new LineDefaultSetting();

				setting.IsExist = ltn[i];
				if (setting.IsExist)
				{
					if (i < lk.Length)
					{
						setting.Type = (lk[i] & 4) > 0 ? RailTypeEnum.LinearMotor : RailTypeEnum.Iron;
						setting.gauge = (lk[i] & 1) > 0 ? RailGaugeEnum.Regular : RailGaugeEnum.Narrow;
						setting.IsElectrified = (lk[i] & 2) > 0;
					}
					if (i < lbs.Length)
					{
						setting.bestSpeed = lbs[i];
					}
					if (i < las.Length)
					{
						setting.LaneNum = las[i];
					}
					if (i < lwe.Length)
					{
						setting.retentionRate = lwe[i];
					}
					if (i < lts.Length)
					{
						setting.taihisen = (TaihisenEnum)lts[i];
					}
					if (ulc != null)
					{
						setting.useComposition = mode.DefautltCompositions[ulc[i] - 1];
					}
					if (i < ulcr.Length)
					{
						setting.runningPerDay = ulcr[i];
					}
				}
				lineDefaultSettings.Add(setting);
			}
			mode.LineSettings = lineDefaultSettings;

			//運行系統
			List<KeitoDefaultSetting> keitoDefaultSettings = new List<KeitoDefaultSetting>();
			int[] udc = ExtractModProperty(modeLines, "udc")?.Split(",").Select(value => int.Parse(value)).ToArray() ?? new int[0];
			int[] udcr = ExtractModProperty(modeLines, "udcr")?.Split(",").Select(value => int.Parse(value)).ToArray()?? new int[udc.Length];
			for (int i = 0; i < udc.Length; i++)
			{
				KeitoDefaultSetting setting = new KeitoDefaultSetting()
				{
					useComposition = mode.DefautltCompositions[udc[i] - 1],
					runningPerDay = udcr[i]
				};
				keitoDefaultSettings.Add(setting);
			}
			mode.KeitoDefaultSettings = keitoDefaultSettings;

			//目標
			mode.goalLineMake = (LineGoalTargetEnum?)ParseIntOrNull(ExtractModProperty(modeLines, "mmake"));
			mode.goalTechDevelop = ExtractModProperties(modeLines, "mtec").ToDictionary(
				value => (PowerEnum)(int.Parse(value.Split(",")[0]) + 1),
				value => int.Parse(value.Split(",")[1]));
			string[] mlbsValues = ExtractModProperty(modeLines, "mlbs")?.Split(",") ?? null;
			if (mlbsValues != null) {
				mode.goalLineBestSpeed = ((LineGoalTargetEnum?)ParseIntOrNull(mlbsValues[0]), int.Parse(mlbsValues[1]));
			}
			string[] mmanegeValues = ExtractModProperty(modeLines, "mmanage")?.Split(",") ?? null;
			if (mmanegeValues != null)
			{
				mode.goalLineManage = (LineGoalTargetEnum?)ParseIntOrNull(mmanegeValues[0]);
			}
			mode.goalMoney = ParseIntOrNull(ExtractModProperty(modeLines, "mmoney"));
			mode.gameoverYear = int.Parse(ExtractModProperty(modeLines, "myear"));

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
			return modLines.Find(line => line.StartsWith(property))?.Split(":")[1] ?? null;
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
		/// 数字を数値にパースする nullならnullを返す
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static int? ParseIntOrNull([AllowNull] string s)
		{
			if (s is null)
			{
				return null;
			}
			else
			{
				return int.Parse(s);
			}
		}

	}
}
