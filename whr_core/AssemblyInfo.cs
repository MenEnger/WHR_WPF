using System.Runtime.CompilerServices;

// 既存の内部計算の利用を、UIとシナリオ解析の2アセンブリに限定する。
[assembly: InternalsVisibleTo("世界鉄道網")]
[assembly: InternalsVisibleTo("whr_scenario")]
