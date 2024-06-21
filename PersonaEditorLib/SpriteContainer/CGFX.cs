using Kontract.Image;
using Kontract.Image.Swizzle;
using Kontract.IO;
using PersonaEditorLib.Sprite;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PersonaEditorLib.SpriteContainer;

public class CGFX : IGameData
{
    public readonly List<Bitmap> bmps = [];

    private readonly List<TXOBEntry> txobList = [];
    private readonly List<ImageSettings> settings = [];
    private byte[] headerBytes;

    public List<GameFile> SubFiles { get; } = [];
    public FormatEnum Type => FormatEnum.Unknown;

    private readonly string fileName;

    public CGFX(string path)
    {
        fileName = Path.GetFileName(path);
        using var fs = File.OpenRead(path);
        LoadImage(fs);
        LoadSubFiles();
    }

    public CGFX(string fileName, Stream stream)
    {
        this.fileName = Path.GetFileName(fileName);
        LoadImage(stream);
        LoadSubFiles();
    }

    public CGFX(string fileName, byte[] bytes)
    {
        this.fileName = Path.GetFileName(fileName);
        using var ms = new MemoryStream(bytes);
        LoadImage(ms);
        LoadSubFiles();
    }

    private void LoadImage(Stream stream)
    {
        using BinaryReaderX br = new(stream);

        //CGFX Header
        var cgfxHeader = br.ReadStruct<CGFXHeader>();

        //Data entries
        List<DataEntry> dataEntries = [];
        for (int i = 0; i < cgfxHeader.entryCount; i++)
        {
            var dataHeader = br.ReadStruct<DataHeader>();
            if (dataHeader.magic == "DATA")
            {
                for (int j = 0; j < 16; j++)
                {
                    dataEntries.Add(new DataEntry(br.BaseStream));
                }
            }
        }

        //TextureEntry
        br.BaseStream.Position = dataEntries[1].offset;
        var dictHeader = br.ReadStruct<DictHeader>();
        var dictEntries = new List<DictEntry>();
        for (int i = 0; i < dictHeader.entryCount; i++)
        {
            dictEntries.Add(new DictEntry(br.BaseStream));
        }

        //TextureObjects
        for (int i = 0; i < dictHeader.entryCount; i++)
        {
            br.BaseStream.Position = dictEntries[i].dataOffset;
            txobList.Add(new TXOBEntry(br.BaseStream));
        }

        //save lists to RAM
        br.BaseStream.Position = 0;
        headerBytes = br.ReadBytes((int)txobList[0].texDataOffset);

        //Add images
        for (int i = 0; i < dictHeader.entryCount; i++)
        {
            br.BaseStream.Position = txobList[i].texDataOffset;
            var width = txobList[i].width;
            var height = txobList[i].height;
            for (int j = 0; j < ((txobList[i].mipmapLvls == 0) ? 1 : txobList[i].mipmapLvls); j++)
            {
                var settings = new ImageSettings
                {
                    Width = (int)width,
                    Height = (int)height,
                    Format = Support.CTRFormat[(byte)txobList[i].format],
                    Swizzle = new CTRSwizzle((int)width, (int)height)
                };

                this.settings.Add(settings);
                bmps.Add(Common.Load(br.ReadBytes((int)(Support.CTRFormat[(byte)txobList[i].format].BitDepth * width * height / 8)), settings));

                width /= 2;
                height /= 2;
            }
        }
    }

    private void LoadSubFiles()
    {
        SubFiles.Clear();
        for (var i = 0; i < bmps.Count; i++)
        {
            var bmp = bmps[i];
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
        for (int i = 0; i < bmps.Count; i++)
        {
            bmps[i] = ((PNG)SubFiles[i].GameData).Bitmap;
        }
        using var ms = new MemoryStream();
        Save(ms, true);
        return ms.ToArray();
    }

    public void Save(Stream input, bool leaveOpen)
    {
        using BinaryWriterX bw = new(input, leaveOpen);

        bw.Write(headerBytes);

        int bmpCount = 0;
        for (int i = 0; i < txobList.Count; i++)
        {
            bw.BaseStream.Position = txobList[i].texDataOffset;
            var width = txobList[i].width;
            var height = txobList[i].height;
            for (int j = 0; j < ((txobList[i].mipmapLvls == 0) ? 1 : txobList[i].mipmapLvls); j++)
            {
                var settings = new ImageSettings
                {
                    Width = (int)width,
                    Height = (int)height,
                    Format = Support.CTRFormat[(byte)txobList[i].format],
                    Swizzle = new CTRSwizzle((int)width, (int)height)
                };

                bw.Write(Common.Save(bmps[bmpCount++], settings));

                width /= 2;
                height /= 2;
            }
        }
    }
}
