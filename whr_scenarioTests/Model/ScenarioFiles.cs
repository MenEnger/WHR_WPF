using System.IO;
using System.Text;

namespace whr_wpf.Model.Tests
{
    internal sealed class ScenarioFiles : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "whr-scenario-" + Guid.NewGuid());

        public ScenarioFiles(bool shiftJis = false)
        {
            Directory.CreateDirectory(DirectoryPath);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = shiftJis ? Encoding.GetEncoding(932) : new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(DirectoryPath, "index.mod"), """
                version:3
                basicyear:1880
                season:0
                steamyear:0
                kamotu:1
                km:10
                rpm:0
                linemc:0
                tecc:0
                infoh:0
                hojo:1880,1900,10
                #mode:検証
                year:1880
                money:5000
                message:開始,案内
                myear:1920
                people:2,1
                #end
                """, encoding);
            File.WriteAllText(Path.Combine(DirectoryPath, "town.csv"), "京都,0,1,10,20,1000,10\n大阪,0,1,30,40,2000,20\n", encoding);
            File.WriteAllText(Path.Combine(DirectoryPath, "line.csv"), "幹線,0,1,1,50,0\n", encoding);
            File.WriteAllText(Path.Combine(DirectoryPath, "longway.csv"), "0,1,1,-1\n", encoding);
            File.WriteAllText(Path.Combine(DirectoryPath, "diagram.csv"), "運転系統,0,1,1,-1\n", encoding);
            // 外部配布物に依存せず、1ピクセルの24ビットBMPを用意する。
            using var writer = new BinaryWriter(File.Create(Path.Combine(DirectoryPath, "map.bmp")));
            writer.Write((ushort)0x4d42);
            writer.Write(58); writer.Write(0); writer.Write(54); writer.Write(40);
            writer.Write(1); writer.Write(1); writer.Write((ushort)1); writer.Write((ushort)24);
            writer.Write(0); writer.Write(4); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
            writer.Write(new byte[] { 0, 0, 255, 0 });
        }

        public void ReplaceProperty(string oldValue, string newValue)
        {
            var path = Path.Combine(DirectoryPath, "index.mod");
            File.WriteAllText(path, File.ReadAllText(path).Replace(oldValue, newValue));
        }

        public void Dispose() => Directory.Delete(DirectoryPath, true);
    }
}
