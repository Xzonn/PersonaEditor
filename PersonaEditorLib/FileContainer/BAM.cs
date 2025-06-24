using System.Collections.Generic;
using System.IO;

namespace PersonaEditorLib.FileContainer;

public class BAM : IGameData
{
    public List<GameFile> SubFiles { get; } = [];
    private int modelDataOffset;
    private byte[] header;

    public BAM(string path)
    {
        Open(File.ReadAllBytes(path));
    }

    public BAM(byte[] data)
    {
        Open(data);
    }

    private void Open(byte[] data)
    {
        using var br = new BinaryReader(new MemoryStream(data));

        var magic = new string(br.ReadChars(4));
        if (magic != "ATBC")
        {
            throw new InvalidDataException("Invalid BAM file header.");
        }
        var size = br.ReadInt32();

        br.BaseStream.Position = 0x14;
        modelDataOffset = br.ReadInt32();

        br.BaseStream.Position = modelDataOffset;
        var modelMagic = new string(br.ReadChars(8));
        if (modelMagic != "MDLSIGN\0")
        {
            throw new InvalidDataException("Invalid BAM model data header.");
        }
        var modelSize = br.ReadInt32();
        if (size != modelDataOffset + 0x80 + modelSize)
        {
            throw new InvalidDataException("BAM file size does not match expected size based on model data offset and size.");
        }
        br.BaseStream.Position = 0;
        header = br.ReadBytes(modelDataOffset + 0x80);
        var item = GameFormatHelper.OpenFile("model", br.ReadBytes(modelSize));
        item.Tag = 0;
        SubFiles.Add(item);
    }

    #region IGameFile
    public FormatEnum Type => FormatEnum.BAM;

    public int GetSize()
    {
        return header.Length + SubFiles[0].GameData.GetSize();
    }

    public byte[] GetData()
    {
        using var output = new MemoryStream();
        var bw = new BinaryWriter(output);
        bw.Write(header);
        var modelData = SubFiles[0].GameData.GetData();
        bw.Write(modelData);
        bw.BaseStream.Position = 4;
        bw.Write(header.Length + modelData.Length);
        bw.BaseStream.Position = modelDataOffset + 8;
        bw.Write(modelData.Length);
        return output.ToArray();
    }

    #endregion
}
