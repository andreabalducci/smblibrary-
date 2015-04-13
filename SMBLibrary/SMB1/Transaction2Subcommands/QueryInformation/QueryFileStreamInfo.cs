/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_QUERY_FILE_STREAM_INFO
    /// </summary>
    public class QueryFileStreamInfo : QueryInformation
    {
        public const int FixedLength = 24;

        public uint NextEntryOffset;
        //uint StreamNameLength; // In bytes
        public ulong StreamSize;
        public ulong StreamAllocationSize;
        public string StreamName; // Unicode

        public QueryFileStreamInfo()
        {
        }

        public QueryFileStreamInfo(byte[] buffer, int offset)
        {
            NextEntryOffset = LittleEndianReader.ReadUInt32(buffer, ref offset);
            uint streamNameLength = LittleEndianReader.ReadUInt32(buffer, ref offset);
            StreamSize = LittleEndianReader.ReadUInt64(buffer, ref offset);
            StreamAllocationSize = LittleEndianReader.ReadUInt64(buffer, ref offset);
            StreamName = ByteReader.ReadUnicodeString(buffer, ref offset, (int)(streamNameLength / 2));
        }

        public override byte[] GetBytes()
        {
            uint streamNameLength = (uint)(StreamName.Length * 2);
            byte[] buffer = new byte[FixedLength + streamNameLength];
            int offset = 0;
            LittleEndianWriter.WriteUInt32(buffer, ref offset, NextEntryOffset);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, streamNameLength);
            LittleEndianWriter.WriteUInt64(buffer, ref offset, StreamSize);
            LittleEndianWriter.WriteUInt64(buffer, ref offset, StreamAllocationSize);
            ByteWriter.WriteUnicodeString(buffer, ref offset, StreamName);
            return buffer;
        }

        public override QueryInformationLevel InformationLevel
        {
            get
            {
                return QueryInformationLevel.SMB_QUERY_FILE_STREAM_INFO;
            }
        }
    }
}
