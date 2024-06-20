using PersonaEditorLib.Sprite;
using System.Collections.Generic;
using System.IO;

namespace PersonaEditorLib.SpriteContainer
{
    public class CTPK : IGameData
    {
        public List<GameFile> SubFiles { get; } = new List<GameFile>();
        public FormatEnum Type => FormatEnum.Unknown;

        private readonly image_nintendo.CTPK.CTPK file;
        private readonly string fileName;

        public CTPK(string path)
        {
            fileName = Path.GetFileName(path);
            using var fs = File.OpenRead(path);
            file = new image_nintendo.CTPK.CTPK(fs);
            LoadSubFiles();
        }

        public CTPK(string fileName, Stream stream)
        {
            this.fileName = Path.GetFileName(fileName);
            file = new image_nintendo.CTPK.CTPK(stream);
            LoadSubFiles();
        }

        public CTPK(string fileName, byte[] bytes)
        {
            this.fileName = Path.GetFileName(fileName);
            using var ms = new MemoryStream(bytes);
            file = new image_nintendo.CTPK.CTPK(ms);
            LoadSubFiles();
        }

        private void LoadSubFiles()
        {
            SubFiles.Clear();
            for (var i = 0; i < file.bmps.Count; i++)
            {
                var bmp = file.bmps[i];
                var gameFile = new GameFile($"{fileName}.{i:D2}.png", new PNG(bmp));
                SubFiles.Add(gameFile);
            }
        }

        public int GetSize()
        {
            return GetData().Length;
        }

        public byte[] GetData()
        {
            for (int i = 0; i < file.bmps.Count; i++)
            {
                file.bmps[i] = ((PNG)SubFiles[i].GameData).Bitmap;
            }
            using var ms = new MemoryStream();
            file.Save(ms, true);
            return ms.ToArray();
        }
    }
}
