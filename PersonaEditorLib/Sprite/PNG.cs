using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PersonaEditorLib.Sprite
{
    public class PNG : IGameData
    {
        public List<GameFile> SubFiles { get; } = new List<GameFile>();
        public FormatEnum Type => FormatEnum.Unknown;

        private Bitmap bitmap;
        private byte[] bytes = Array.Empty<byte>();
        public Bitmap Bitmap
        {
            get => bitmap;
            set
            {
                bitmap = value;
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                bytes = ms.ToArray();
            }
        }

        public PNG(byte[] bytes)
        {
            Bitmap = new Bitmap(new MemoryStream(bytes));
        }

        public PNG(Bitmap bitmap)
        {
            Bitmap = bitmap;
        }

        public int GetSize()
        {
            return bytes.Length;
        }

        public byte[] GetData()
        {
            return bytes;
        }
    }
}
