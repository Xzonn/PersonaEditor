using Kontract.IO;
using PersonaEditorLib.Sprite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PersonaEditorLib.SpriteContainer;


public class SPR3 : IGameData
{
    public SPR3Header header;
    public List<SPR3Entry> sSpr3 = [];

    public CTPK ctpk;

    public List<GameFile> SubFiles { get; } = [];
    public FormatEnum Type => FormatEnum.Unknown;

    private readonly string fileName;

    public SPR3(string path)
    {
        fileName = Path.GetFileName(path);
        using var fs = File.OpenRead(path);
        LoadImage(fs);
        LoadSubFiles();
    }

    public SPR3(string fileName, Stream stream)
    {
        this.fileName = Path.GetFileName(fileName);
        LoadImage(stream);
        LoadSubFiles();
    }

    public SPR3(string fileName, byte[] bytes)
    {
        this.fileName = Path.GetFileName(fileName);
        using var ms = new MemoryStream(bytes);
        LoadImage(ms);
        LoadSubFiles();
    }

    private void LoadImage(Stream stream)
    {
        using BinaryReaderX br = new(stream);

        //SPR3Header
        header = br.ReadStruct<SPR3Header>();

        //dataOffset
        br.ReadUInt32();
        var dataOffset = br.ReadUInt32();

        //Entries
        var entries = br.ReadMultiple<SPR3EntryItem>(header.entryCount);

        //SPR3EntryItem data
        for (int i = 0; i < header.entryCount; i++)
        {
            sSpr3.Add(new SPR3Entry
            {
                entry = entries[i],
                data = br.ReadBytes(0x80)
            });
        }

        //Load CTPK
        br.BaseStream.Position = dataOffset;
        ctpk = new CTPK(fileName, new MemoryStream(br.ReadBytes((int)(br.BaseStream.Length - dataOffset))));
    }

    private void LoadSubFiles()
    {
        SubFiles.Clear();
        for (var i = 0; i < ctpk.bmps.Count; i++)
        {
            var bmp = ctpk.bmps[i];
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
        for (int i = 0; i < ctpk.bmps.Count; i++)
        {
            ctpk.bmps[i] = ((PNG)SubFiles[i].GameData).Bitmap;
        }
        using var ms = new MemoryStream();
        Save(ms, true);
        return ms.ToArray();
    }

    public void Save(Stream input, bool leaveOpen)
    {
        using BinaryWriterX bw = new(input, leaveOpen);

        //SPR3Header
        bw.WriteStruct(header);
        bw.Write(0);
        var dataOffset = 0x28 + sSpr3.Count * 8 + sSpr3.Count * 0x80;
        bw.Write(dataOffset);

        //Entries
        foreach (var spr3 in sSpr3) bw.WriteStruct(spr3.entry);
        foreach (var spr3 in sSpr3) bw.Write(spr3.data);

        //ctpk data
        var save = new MemoryStream();
        ctpk.Save(save, true);
        save.Position = 0;

        save.CopyTo(bw.BaseStream);
    }
}
