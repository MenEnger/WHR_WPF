using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
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
			=> LoadFile(baseDir, null);

		/// <summary>
		/// 設定検証後・CSV解析前に、利用側で参照資源を確保して読み込む。
		/// </summary>
		/// <param name="prepareMap">地図参照先の通知。画像を使わない利用側では省略する。</param>
		public static GameInfo LoadFile(string baseDir, Action<string> prepareMap)
		{
			var gameInfo = new GameInfo();

			//mod読み込み
			string modPath = Path.Combine(baseDir, "index.mod");
			var context = new ReadContext(modPath, ScenarioReadStage.Settings);
			List<string> modLines = context.Read(() => File.ReadAllLines(modPath, GetScenarioEncoding(modPath)).ToList());
			gameInfo.ScenerioVersion = context.Number(context.Property(modLines, "version", true), "version");
			gameInfo.BasicYear = context.Number(context.Property(modLines, "basicyear", true), "basicyear");
			gameInfo.Season = (SeasonEnum)context.Number(context.Property(modLines, "season", true), "season");
			gameInfo.SteamYear = context.Number(context.Property(modLines, "steamyear", true), "steamyear");
			gameInfo.Kamotu = (KamotsuEnum)context.Number(context.Property(modLines, "kamotu", true), "kamotu");
			gameInfo.FarePerKm = context.Number(context.Property(modLines, "km", true), "km");
			gameInfo.Rpm = context.Number(context.Property(modLines, "rpm", true), "rpm");
			gameInfo.LineMakeCost = context.Number(context.Property(modLines, "linemc", true), "linemc");
			gameInfo.TechCost = context.Number(context.Property(modLines, "tecc", true), "tecc");
			//mapは省略
			gameInfo.InfoPosi = (InfoPosiEnum)context.Number(context.Property(modLines, "infoh", true), "infoh");

			var hojo = context.Property(modLines, "hojo", true).Split(",");
			gameInfo.HojoStartYear = context.Number(context.Field(hojo, 0, "hojo"), "hojo");
			gameInfo.HojoEndYear = context.Number(context.Field(hojo, 1, "hojo"), "hojo");
			gameInfo.HojoAmount = context.Number(context.Field(hojo, 2, "hojo"), "hojo");

			gameInfo.warModeList = CreateWarModeList(context.Properties(modLines, "warmode"), context);

			//バリデーションor値補正
			if (gameInfo.ScenerioVersion < 1)
			{
				throw new ScenarioValidationException(ScenarioValidationError.InvalidVersion, modPath, "version", gameInfo.ScenerioVersion);
			}
			if (gameInfo.BasicYear < 0)
			{
				throw new ScenarioValidationException(ScenarioValidationError.InvalidBasicYear, modPath, "basicyear", gameInfo.BasicYear);
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

			// 地図は参照先のみ記録し、画像の読込は表示側へ任せる。
			gameInfo.MapImagePath = Path.GetFullPath(Path.Combine(baseDir, "map.bmp"));
			prepareMap?.Invoke(gameInfo.MapImagePath);

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
			gameInfo.Modes = CreateModeList(modLines, new ReadContext(modPath, ScenarioReadStage.Modes));

			// モードの人口補正・技術設定が適用された後でApを初期化する。

			return gameInfo;
		}

		// 元シナリオのShift_JISと、新規シナリオのUTF-8を読み込む。
		private static Encoding GetScenarioEncoding(string path)
		{
			var utf8 = new UTF8Encoding(false, true);
			try
			{
				utf8.GetString(File.ReadAllBytes(path));
				return utf8;
			}
			catch (DecoderFallbackException)
			{
				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
				return Encoding.GetEncoding(932);
			}
		}

		//town.csv読み込み
		private static List<Station> CreateStationFromFile(string basePath)
		{
			using var csv = new CsvInput(Path.Combine(basePath, "town.csv"), ScenarioReadStage.Stations);
			var stationList = new List<Station>();
			while (csv.HasRows)
			{
				var cols = csv.ReadFields();
				var context = csv.Context;
				stationList.Add(new Station
				{
					Name = Field(cols, 0, context),
					Size = (StationSize)Number(Field(cols, 2, context), context),
					X = Number(Field(cols, 3, context), context),
					Y = Number(Field(cols, 4, context), context),
					Population = Number(Field(cols, 5, context), context),
					KamotsuKibo = Number(Field(cols, 6, context), context)
				});
			}
			return stationList;
		}

		//line.csv読み込み
		private static List<Line> CreateLineFromFile(string basePath, GameInfo gameInfo)
		{
			using var csv = new CsvInput(Path.Combine(basePath, "line.csv"), ScenarioReadStage.Lines);
			var lineList = new List<Line>();
			while (csv.HasRows)
			{
				var cols = csv.ReadFields();
				var context = csv.Context;
				lineList.Add(new Line
				{
					Name = Field(cols, 0, context),
					Start = Reference(gameInfo.stations, Number(Field(cols, 1, context), context), context),
					End = Reference(gameInfo.stations, Number(Field(cols, 2, context), context), context),
					grade = (LineGrade)Number(Field(cols, 3, context), context),
					Distance = Number(Field(cols, 4, context), context),
					propertyType = (LinePropertyType)Number(Field(cols, 5, context), context)
				});
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
			using var csv = new CsvInput(Path.Combine(basePath, "longway.csv"), ScenarioReadStage.Longways);
			var longwayList = new List<Longway>();
			while (csv.HasRows)
			{
				var cols = csv.ReadFields();
				var context = csv.Context;
				var longway = new Longway
				{
					route = new List<Line>(),
					start = Reference(gameInfo.stations, Number(Field(cols, 0, context), context), context),
					end = Reference(gameInfo.stations, Number(Field(cols, 1, context), context), context),
					isKamotuOperated = true
				};
				for (int idx = 2; ; idx++)
				{
					var j = Number(Field(cols, idx, context, route: true), context);
					if (j == -1) break;
					if (j == -2) { longway.isKamotuOperated = false; break; }
					longway.route.Add(Reference(gameInfo.lines, j - 1, context));
				}
				longwayList.Add(longway);
			}
			return longwayList;
		}

		//diagram.csv読み込み
		private static List<KeitoDiagram> CreateDiagramFromFile(string basePath, GameInfo gameInfo)
		{
			using var csv = new CsvInput(Path.Combine(basePath, "diagram.csv"), ScenarioReadStage.Diagrams);
			var diagramList = new List<KeitoDiagram>();
			while (csv.HasRows)
			{
				var cols = csv.ReadFields();
				var context = csv.Context;
				var diagram = new KeitoDiagram
				{
					route = new List<Line>(),
					Name = Field(cols, 0, context),
					start = Reference(gameInfo.stations, Number(Field(cols, 1, context), context), context),
					end = Reference(gameInfo.stations, Number(Field(cols, 2, context), context), context)
				};
				for (int idx = 3; ; idx++)
				{
					var j = Number(Field(cols, idx, context, route: true), context);
					if (j == -1) break;
					diagram.route.Add(Reference(gameInfo.lines, j - 1, context));
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
		public static List<WarMode> CreateWarModeList(List<string> warModeStringList) => CreateWarModeList(warModeStringList, null);
		private static List<WarMode> CreateWarModeList(List<string> warModeStringList, ReadContext context)
		{
			return warModeStringList.Select(warmodeStr =>
			{
				var arr = warmodeStr.Split(',');
				return new WarMode
				{
					StartYear = Number(Field(arr, 0, context, "warmode"), context, "warmode"),
					EndYear = Number(Field(arr, 1, context, "warmode"), context, "warmode"),
					kamotsuIndex = Number(Field(arr, 2, context, "warmode"), context, "warmode"),
				};
			}).ToList();
		}

		/// <summary>
		/// モード設定をブロックごとに抽出してリストで返却
		/// </summary>
		/// <param name="modesLines"></param>
		/// <returns></returns>
		public static List<Mode> CreateModeList(List<string> modesLines) => CreateModeList(modesLines, null);
		private static List<Mode> CreateModeList(List<string> modesLines, ReadContext context)
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
					modeObjList.Add(CreateMode(modeList, context?.ForMode(modeObjList.Count + 1)));
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
		private static Mode CreateMode(List<string> modeLines, ReadContext context)
		{
			Mode mode = new Mode();

			//ゲーム設定
			mode.Name = Property(modeLines, context, "#mode");
			mode.Year = Number(Property(modeLines, context, "year", true), context, "year");
			mode.Money = LongNumber(Property(modeLines, context, "money", true), context, "money");
			mode.Message = Property(modeLines, context, "message", true).Replace(',', '\n');
			mode.MYear = Number(Property(modeLines, context, "myear", true), context, "myear");
			mode.genkaiJoki = OptionalNumber(Property(modeLines, context, "steam"), context, "steam") ?? 40;
			mode.genkaiDenki = OptionalNumber(Property(modeLines, context, "elect"), context, "elect");
			mode.genkaiKidosha = OptionalNumber(Property(modeLines, context, "diesel"), context, "diesel");
			mode.genkaiLinear = OptionalNumber(Property(modeLines, context, "linear"), context, "linear");
			var tecno = OptionalNumber(Property(modeLines, context, "tecno"), context, "tecno");
			if (tecno.HasValue)
			{
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
			int[] people = Property(modeLines, context, "people")?.Split(",").Select(it => Number(it, context, "people")).Take(2).ToArray() ?? new int[] { 1, 1 };
			if (people.Count() == 2)
			{
				mode.peopleNume = people[0];
				mode.peopleDenom = people[1];
			}


			//デフォルト編成
			mode.DefautltCompositions = Properties(modeLines, context, "car").Select(value =>
			{
				string[] arr = value.Split(",");
				int ck = Number(Field(arr, 2, context, "car"), context, "car"); //編成規格
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
					throw InvalidSetting(context, "car", new ArgumentException("軌間が未定義です"));

				PowerEnum powerSource =
					isLinear ? PowerEnum.LinearMotor :
					isElectric ? PowerEnum.Electricity :
					isDiesel ? PowerEnum.Diesel :
					isSteam ? PowerEnum.Steam :
					throw InvalidSetting(context, "car", new ArgumentException("動力源が未指定です"));

				return new DefautltComposition
				{
					Name = Field(arr, 0, context, "car"),
					BestSpeed = Number(Field(arr, 1, context, "car"), context, "car"),
					CarCount = Number(Field(arr, 3, context, "car"), context, "car"),
					HeldUnits = Number(Field(arr, 4, context, "car"), context, "car"),
					Price = Number(Field(arr, 5, context, "car"), context, "car"),
					seat = Seat(Number(Field(arr, 6, context, "car"), context, "car"), context),
					Gauge = carGauge,
					Power = powerSource,
					Tilt = isPendulum ? CarTiltEnum.Pendulum : CarTiltEnum.None,
					Type = railType
				};
			}).ToList();

			//路線
			List<LineDefaultSetting> lineDefaultSettings = new List<LineDefaultSetting>();
			bool[] ltn = Property(modeLines, context, "ltn")?.Split(",").Select(flg => Number(flg, context, "ltn") != 0).ToArray() ?? new bool[0];
			int[] lk = Property(modeLines, context, "lk")?.Split(",").Select(value => Number(value, context, "lk")).ToArray() ?? new int[ltn.Length];
			int[] lbs = Property(modeLines, context, "lbs")?.Split(",").Select(value => Number(value, context, "lbs")).ToArray() ?? new int[ltn.Length];
			int[] las = Property(modeLines, context, "las")?.Split(",").Select(value => Number(value, context, "las")).ToArray() ?? new int[ltn.Length];
			int[] lwe = Property(modeLines, context, "lwe")?.Split(",").Select(value => Number(value, context, "lwe")).ToArray() ?? Enumerable.Repeat(GameConstants.RetentionRateDefault, ltn.Length).ToArray();
			int[] lts = Property(modeLines, context, "lts")?.Split(",").Select(value => Number(value, context, "lts")).ToArray() ?? new int[ltn.Length];
			int[] ulc = Property(modeLines, context, "ulc")?.Split(",").Select(value => Number(value, context, "ulc")).ToArray() ?? null;
			int[] ulcr = Property(modeLines, context, "ulcr")?.Split(",").Select(value => Number(value, context, "ulcr")).ToArray() ?? new int[ltn.Length];
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
						setting.useComposition = Reference(mode.DefautltCompositions, Field(ulc, i, context, "ulc") - 1, context, "ulc");
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
			int[] udc = Property(modeLines, context, "udc")?.Split(",").Select(value => Number(value, context, "udc")).ToArray() ?? new int[0];
			int[] udcr = Property(modeLines, context, "udcr")?.Split(",").Select(value => Number(value, context, "udcr")).ToArray() ?? new int[udc.Length];
			for (int i = 0; i < udc.Length; i++)
			{
				KeitoDefaultSetting setting = new KeitoDefaultSetting()
				{
					useComposition = Reference(mode.DefautltCompositions, udc[i] - 1, context, "udc"),
					runningPerDay = Field(udcr, i, context, "udcr")
				};
				keitoDefaultSettings.Add(setting);
			}
			mode.KeitoDefaultSettings = keitoDefaultSettings;

			//目標
			mode.goalLineMake = (LineGoalTargetEnum?)OptionalNumber(Property(modeLines, context, "mmake"), context, "mmake");
			mode.goalTechDevelop = ReadGoals(Properties(modeLines, context, "mtec"), context);

			string[] mlbsValues = Property(modeLines, context, "mlbs")?.Split(",") ?? null;
			if (mlbsValues != null)
			{
				mode.goalLineBestSpeed = ((LineGoalTargetEnum?)OptionalNumber(Field(mlbsValues, 0, context, "mlbs"), context, "mlbs"), Number(Field(mlbsValues, 1, context, "mlbs"), context, "mlbs"));
			}
			string[] mmanegeValues = Property(modeLines, context, "mmanage")?.Split(",") ?? null;
			if (mmanegeValues != null)
			{
				mode.goalLineManage = (LineGoalTargetEnum?)OptionalNumber(Field(mmanegeValues, 0, context, "mmanage"), context, "mmanage");
			}
			mode.goalMoney = OptionalNumber(Property(modeLines, context, "mmoney"), context, "mmoney");
			mode.gameoverYear = Number(Property(modeLines, context, "myear", true), context, "myear");

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

		private sealed class CsvInput : IDisposable
		{
			private readonly TextFieldParser parser;
			private readonly string[] physicalLines;
			public ReadContext Context { get; }
			public bool HasRows => !parser.EndOfData;
			public CsvInput(string path, ScenarioReadStage stage)
			{
				Context = new ReadContext(path, stage);
				var text = Context.Read(() => File.ReadAllText(path, GetScenarioEncoding(path)));
				physicalLines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
				parser = new TextFieldParser(new StringReader(text)) { TextFieldType = FieldType.Delimited };
				parser.SetDelimiters(",");
			}
			public string[] ReadFields()
			{
				// EndOfDataの先読みは空行を消費しないため、同じスナップショットで開始行を補正する。
				long line = parser.LineNumber;
				while (line > 0 && line <= physicalLines.LongLength && string.IsNullOrWhiteSpace(physicalLines[line - 1])) line++;
				Context.Line = line > 0 ? line : null;
				try { return parser.ReadFields(); }
				catch (MalformedLineException ex)
				{
					Context.Line = parser.ErrorLineNumber > 0 ? parser.ErrorLineNumber : Context.Line;
					throw Context.Error(ScenarioReadError.InvalidFormat, ex);
				}
			}
			public void Dispose() => parser.Dispose();
		}

		private sealed class ReadContext
		{
			public string Path { get; }
			public ScenarioReadStage Stage { get; }
			public long? Line { get; set; }
			public int? Mode { get; }
			public ReadContext(string path, ScenarioReadStage stage, int? mode = null)
			{ Path = path; Stage = stage; Mode = mode; }
			public ReadContext ForMode(int mode) => new ReadContext(Path, Stage, mode);
			public ScenarioReadException Error(ScenarioReadError error, Exception inner = null, string property = null)
				=> new ScenarioReadException(error, Stage, Path, inner, Line, property, Mode);
			public T Read<T>(Func<T> read)
			{
				try { return read(); }
				catch (FileNotFoundException ex) { throw Error(ScenarioReadError.FileNotFound, ex); }
				catch (DirectoryNotFoundException ex) { throw Error(ScenarioReadError.FileNotFound, ex); }
				catch (UnauthorizedAccessException ex) { throw Error(ScenarioReadError.AccessDenied, ex); }
				catch (IOException ex) { throw Error(ScenarioReadError.IoFailure, ex); }
			}
			public string Property(List<string> lines, string key, bool required = false)
				=> ScenerioLoadUtil.Property(lines, this, key, required);
			public List<string> Properties(List<string> lines, string key)
				=> ScenerioLoadUtil.Properties(lines, this, key);
			public int Number(string value, string key) => ScenerioLoadUtil.Number(value, this, key);
			public T Field<T>(T[] values, int index, string key) => ScenerioLoadUtil.Field(values, index, this, key);
		}

		private static string Property(List<string> lines, ReadContext context, string key, bool required = false)
		{
			if (context == null) return ExtractModProperty(lines, key);
			string value;
			try { value = ExtractModProperty(lines, key); }
			catch (IndexOutOfRangeException ex) { throw context.Error(ScenarioReadError.MissingField, ex, key); }
			if (required && value == null) throw context.Error(ScenarioReadError.MissingValue, property: key);
			return value;
		}

		private static List<string> Properties(List<string> lines, ReadContext context, string key)
		{
			if (context == null) return ExtractModProperties(lines, key);
			try { return ExtractModProperties(lines, key); }
			catch (IndexOutOfRangeException ex) { throw context.Error(ScenarioReadError.MissingField, ex, key); }
		}

		private static int Number(string value, ReadContext context, string key = null)
		{
			if (context == null) return int.Parse(value);
			try { return int.Parse(value); }
			catch (FormatException ex) { throw context.Error(ScenarioReadError.InvalidNumber, ex, key); }
			catch (OverflowException ex) { throw context.Error(ScenarioReadError.NumberOutOfRange, ex, key); }
		}

		private static long LongNumber(string value, ReadContext context, string key)
		{
			if (context == null) return long.Parse(value);
			try { return long.Parse(value); }
			catch (FormatException ex) { throw context.Error(ScenarioReadError.InvalidNumber, ex, key); }
			catch (OverflowException ex) { throw context.Error(ScenarioReadError.NumberOutOfRange, ex, key); }
		}
		private static int? OptionalNumber(string value, ReadContext context, string key)
			=> value == null ? null : Number(value, context, key);

		private static T Field<T>(T[] values, int index, ReadContext context, string key = null, bool route = false)
		{
			try { return values[index]; }
			catch (IndexOutOfRangeException ex) when (context != null)
			{ throw context.Error(route ? ScenarioReadError.InvalidFormat : ScenarioReadError.MissingField, ex, key); }
		}

		private static T Reference<T>(List<T> values, int index, ReadContext context, string key = null)
		{
			try { return values[index]; }
			catch (ArgumentOutOfRangeException ex) when (context != null)
			{ throw context.Error(ScenarioReadError.InvalidReference, ex, key); }
		}

		private static Exception InvalidSetting(ReadContext context, string key, ArgumentException ex)
			=> context == null ? ex : context.Error(ScenarioReadError.InvalidSetting, ex, key);

		private static SeatEnum Seat(int value, ReadContext context)
		{
			try { return LogicUtil.ConvertSeatModToInternalId(value); }
			catch (ArgumentException ex) when (context != null)
			{ throw context.Error(ScenarioReadError.InvalidSetting, ex, "car"); }
		}

		private static Dictionary<PowerEnum, int> ReadGoals(List<string> values, ReadContext context)
		{
			// キーと値の消費順は従来のToDictionaryと同じに保つ。
			var goals = new Dictionary<PowerEnum, int>();
			foreach (var value in values)
			{
				var key = (PowerEnum)(Number(Field(value.Split(","), 0, context, "mtec"), context, "mtec") - 1);
				var amount = Number(Field(value.Split(","), 1, context, "mtec"), context, "mtec");
				try { goals.Add(key, amount); }
				catch (ArgumentException ex) when (context != null)
				{ throw context.Error(ScenarioReadError.InvalidSetting, ex, "mtec"); }
			}
			return goals;
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
