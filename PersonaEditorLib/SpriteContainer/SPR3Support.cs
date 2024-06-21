using Kontract;
using System.Runtime.InteropServices;

namespace PersonaEditorLib.SpriteContainer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class SPR3Header
{
    public uint const1;
    public uint const2;
    public Magic magic;
    public uint headerSize;
    public uint unk1;
    public ushort unk2;
    public ushort entryCount;
    public uint dataValOffset;
    public uint entryOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class SPR3EntryItem
{
    public uint zero1;
    public uint entryOffset;
}

public class SPR3Entry
{
    public SPR3EntryItem entry;
    public byte[] data;
}