﻿using Kontract;
using Kontract.Image.Format;
using Kontract.Interface;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PersonaEditorLib.SpriteContainer;

public class Support
{
    public static Dictionary<int, IImageFormat> CTRFormat = new()
    {
        [0] = new RGBA(8, 8, 8, 8),
        [1] = new RGBA(8, 8, 8),
        [2] = new RGBA(5, 5, 5, 1),
        [3] = new RGBA(5, 6, 5),
        [4] = new RGBA(4, 4, 4, 4),
        [5] = new LA(8, 8),
        [6] = new HL(8, 8),
        [7] = new LA(8, 0),
        [8] = new LA(0, 8),
        [9] = new LA(4, 4),
        [10] = new LA(4, 0),
        [11] = new LA(0, 4),
        [12] = new ETC1(),
        [13] = new ETC1(true)
    };
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class CTPKHeader
{
    public Magic magic;
    public short version;
    public short texCount;
    public int texSecOffset;
    public int texSecSize;
    public int crc32SecOffset;
    public int texInfoOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class CTPKEntry
{
    public TexEntry texEntry;
    public List<int> dataSizes = [];
    public string name;
    public HashEntry hash;
    public MipmapEntry mipmapEntry;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class TexEntry
{
    public int nameOffset;
    public int texDataSize;
    public int texOffset;
    public int imageFormat;
    public short width;
    public short height;
    public byte mipLvl;
    public byte type;
    public short zero0;
    public int bitmapSizeOffset;
    public uint timeStamp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class HashEntry
{
    public uint crc32;
    public int id;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class MipmapEntry
{
    public byte mipmapFormat;
    public byte mipLvl;
    //never used compression specifications?
    public byte compression;
    public byte compMethod;
}