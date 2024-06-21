using Kontract.Image;
using Kontract.Image.Swizzle;
using Kontract.IO;
using PersonaEditorLib.Sprite;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PersonaEditorLib.SpriteContainer;

public class CTPK : IGameData
{
    public readonly List<CTPKEntry> entries = [];
    public readonly List<Bitmap> bmps = [];

    private CTPKHeader header;
    private readonly List<ImageSettings> settings = [];

    public List<GameFile> SubFiles { get; } = [];
    public FormatEnum Type => FormatEnum.Unknown;

    private readonly string fileName;

    public CTPK(string path)
    {
        fileName = Path.GetFileName(path);
        using var fs = File.OpenRead(path);
        LoadImage(fs);
        LoadSubFiles();
    }

    public CTPK(string fileName, Stream stream)
    {
        this.fileName = Path.GetFileName(fileName);
        LoadImage(stream);
        LoadSubFiles();
    }

    public CTPK(string fileName, byte[] bytes)
    {
        this.fileName = Path.GetFileName(fileName);
        using var ms = new MemoryStream(bytes);
        LoadImage(ms);
        LoadSubFiles();
    }

    private void LoadImage(Stream stream)
    {
        using BinaryReaderX br = new(stream);

        //Header
        header = br.ReadStruct<CTPKHeader>();
        for (int i = 0; i < header.texCount; i++) entries.Add(new CTPKEntry());

        //TexEntry List
        br.BaseStream.Position = 0x20;
        foreach (var entry in entries) entry.texEntry = br.ReadStruct<TexEntry>();

        //DataSize List
        foreach (var entry in entries) for (int i = 0; i < entry.texEntry.mipLvl; i++) entry.dataSizes.Add(br.ReadInt32());

        //Name List
        foreach (var entry in entries) entry.name = br.ReadCStringA();

        //Hash List
        br.BaseStream.Position = header.crc32SecOffset;
        List<HashEntry> hashList = [.. br.ReadMultiple<HashEntry>(header.texCount).OrderBy(e => e.id)];
        var count = 0;
        foreach (var entry in entries) entry.hash = hashList[count++];

        //MipMapInfo List
        br.BaseStream.Position = header.texInfoOffset;
        foreach (var entry in entries) entry.mipmapEntry = br.ReadStruct<MipmapEntry>();

        //Add bmps
        br.BaseStream.Position = header.texSecOffset;
        for (int i = 0; i < entries.Count; i++)
        {
            //Main texture
            br.BaseStream.Position = entries[i].texEntry.texOffset + header.texSecOffset;
            var settings = new ImageSettings
            {
                Width = entries[i].texEntry.width,
                Height = entries[i].texEntry.height,
                Format = Support.CTRFormat[entries[i].texEntry.imageFormat],
                Swizzle = new CTRSwizzle(entries[i].texEntry.width, entries[i].texEntry.height)
            };

            this.settings.Add(settings);
            bmps.Add(Common.Load(br.ReadBytes((entries[i].dataSizes[0] == 0) ? entries[i].texEntry.texDataSize : entries[i].dataSizes[0]), settings));

            //Mipmaps
            if (entries[i].texEntry.mipLvl > 1)
            {
                for (int j = 1; j < entries[i].texEntry.mipLvl; j++)
                {
                    settings = new ImageSettings
                    {
                        Width = settings.Width >> 1,
                        Height = settings.Height >> 1,
                        Format = Support.CTRFormat[entries[i].mipmapEntry.mipmapFormat],
                        Swizzle = new CTRSwizzle(settings.Width >> 1, settings.Height >> 1)
                    };

                    this.settings.Add(settings);
                    bmps.Add(Common.Load(br.ReadBytes(entries[i].dataSizes[j]), settings));
                }
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

        //Write CTPK Header
        bw.WriteStruct(header);
        bw.BaseStream.Position = 0x20;

        //Write TexEntries
        foreach (var entry in entries) bw.WriteStruct(entry.texEntry);

        //Write dataSizes
        foreach (var entry in entries)
            foreach (var size in entry.dataSizes) bw.Write(size);

        //Write names
        foreach (var entry in entries) bw.WriteASCII(entry.name + "\0");

        //Write hashes
        bw.BaseStream.Position = (bw.BaseStream.Position + 0x3) & ~0x3;
        List<HashEntry> hash = [.. entries.Select(c => c.hash).OrderBy(c => c.crc32)];
        foreach (var entry in hash) bw.WriteStruct(entry);

        //Write mipmapInfo
        foreach (var entry in entries) bw.WriteStruct(entry.mipmapEntry);

        //Write bitmaps
        bw.BaseStream.Position = header.texSecOffset;
        var index = 0;
        foreach (var entry in entries)
        {
            var settings = new ImageSettings
            {
                Width = bmps[index].Width,
                Height = bmps[index].Height,
                Format = Support.CTRFormat[entry.texEntry.imageFormat],
                Swizzle = new CTRSwizzle(bmps[index].Width, bmps[index].Height)
            };
            bw.Write(Common.Save(bmps[index++], settings));

            if (entry.texEntry.mipLvl > 1)
            {
                for (int i = 1; i < entry.texEntry.mipLvl; i++)
                {
                    settings = new ImageSettings
                    {
                        Width = bmps[index].Width << i,
                        Height = bmps[index].Height << i,
                        Format = Support.CTRFormat[entry.mipmapEntry.mipmapFormat],
                        Swizzle = new CTRSwizzle(bmps[index].Width << i, bmps[index].Height << i)
                    };
                    bw.Write(Common.Save(bmps[index++], settings));
                }
            }
        }
    }
}
