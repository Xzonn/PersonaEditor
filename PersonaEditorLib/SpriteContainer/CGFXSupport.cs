﻿using Kontract;
using Kontract.IO;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace PersonaEditorLib.SpriteContainer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CGFXHeader
{
    public Magic magic;
    public ushort byteOrder;
    public ushort headerSize;
    public uint revision;
    public uint fileSize;
    public uint entryCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DataHeader
{
    public Magic magic;
    public uint dataSize;
}

public class DataEntry
{
    public DataEntry(Stream input)
    {
        using (var br = new BinaryReaderX(input, true))
        {
            entryCount = br.ReadUInt32();
            var relOffset = br.ReadUInt32();
            offset = (relOffset > 0) ? relOffset + (uint)br.BaseStream.Position - 4 : 0;
        }
    }
    public uint entryCount;
    public uint offset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DictHeader
{
    public Magic magic;
    public uint dictSize;
    public uint entryCount;
    public uint rootNodeReference;
    public ushort rootNodeLeft;
    public ushort rootNodeRight;
    public uint rootNodeNameOffset;
    public uint rootNodeDataOffset;
}

public class DictEntry
{
    public DictEntry(Stream input)
    {
        using (var br = new BinaryReaderX(input, true))
        {
            refBit = br.ReadInt32();
            leftNode = br.ReadUInt16();
            rightNode = br.ReadUInt16();
            nameOffset = br.ReadUInt32() + (uint)br.BaseStream.Position - 4;
            dataOffset = br.ReadUInt32() + (uint)br.BaseStream.Position - 4;
        }
    }
    public int refBit; //Radix tree?
    public ushort leftNode;
    public ushort rightNode;
    public uint nameOffset;
    public uint dataOffset;
}

public class TXOBEntry
{
    public TXOBEntry(Stream input)
    {
        using (var br = new BinaryReaderX(input, true))
        {
            type = br.ReadUInt32();
            magic = br.ReadStruct<Magic>();
            revision = br.ReadUInt32();
            nameOffset = br.ReadUInt32() + (uint)br.BaseStream.Position - 4;
            userDataEntries = br.ReadUInt32();

            var tmp = br.ReadUInt32();
            userDataOffset = (tmp > 0) ? tmp + (uint)br.BaseStream.Position - 4 : 0;

            height = br.ReadUInt32();
            width = br.ReadUInt32();
            openGLFormat = br.ReadUInt32();
            openGLType = br.ReadUInt32();
            mipmapLvls = br.ReadUInt32();
            texObject = br.ReadUInt32();
            locationFlags = br.ReadUInt32();
            format = br.ReadUInt32();
            userDataSize = br.ReadUInt32();

            var userDataStart = br.BaseStream.Position - 4;
            if (userDataOffset != 0 && userDataSize > 4)
            {
                userDataHeader = br.ReadStruct<DictHeader>();
                for (int i = 0; i < userDataHeader.entryCount; i++)
                {
                    dictEntries.Add(new DictEntry(br.BaseStream));
                }

                //this has to be hacky for now
                br.BaseStream.Position = userDataStart + userDataSize;
            }

            height2 = br.ReadUInt32();
            width2 = br.ReadUInt32();
            texDataSize = br.ReadUInt32();
            texDataOffset = br.ReadUInt32() + (uint)br.BaseStream.Position - 4;
            dynamicAllocator = br.ReadUInt32();
            bitDepth = br.ReadUInt32();
            locAddress = br.ReadUInt32();
            memAddress = br.ReadUInt32();
        }
    }
    public uint type;
    public Magic magic;
    public uint revision;
    public uint nameOffset;
    public uint userDataEntries;
    public uint userDataOffset;
    public uint height;
    public uint width;
    public uint openGLFormat;
    public uint openGLType;
    public uint mipmapLvls;
    public uint texObject;
    public uint locationFlags;
    public uint format;
    public uint userDataSize;
    public uint width2;
    public uint height2;
    public uint texDataSize;
    public uint texDataOffset;
    public uint dynamicAllocator;
    public uint bitDepth;
    public uint locAddress;
    public uint memAddress;

    public DictHeader userDataHeader;
    List<DictEntry> dictEntries = new List<DictEntry>();
}