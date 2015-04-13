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
    /// SMB_QUERY_FS_SIZE_INFO
    /// </summary>
    public class QueryFSSizeInfo : QueryFSInformation
    {
        public const int Length = 24;

        public ulong TotalAllocationUnits;
        public ulong TotalFreeAllocationUnits;
        public uint SectorsPerAllocationUnit;
        public uint BytesPerSector;

        public QueryFSSizeInfo()
        {
        }

        public QueryFSSizeInfo(byte[] buffer, int offset)
        {
            TotalAllocationUnits = LittleEndianConverter.ToUInt64(buffer, 0);
            TotalFreeAllocationUnits = LittleEndianConverter.ToUInt64(buffer, 8);
            SectorsPerAllocationUnit = LittleEndianConverter.ToUInt32(buffer, 16);
            BytesPerSector = LittleEndianConverter.ToUInt32(buffer, 20);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            byte[] buffer = new byte[Length];
            LittleEndianWriter.WriteUInt64(buffer, 0, TotalAllocationUnits);
            LittleEndianWriter.WriteUInt64(buffer, 8, TotalFreeAllocationUnits);
            LittleEndianWriter.WriteUInt32(buffer, 16, SectorsPerAllocationUnit);
            LittleEndianWriter.WriteUInt32(buffer, 20, BytesPerSector);
            return buffer;
        }
    }
}
