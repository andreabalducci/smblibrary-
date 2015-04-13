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
    /// SMB_FIND_FILE_NAMES_INFO
    /// </summary>
    public class FindFileNamesInfo : FindInformationEntry
    {
        public const int FixedLength = 12;

        public uint NextEntryOffset;
        public uint FileIndex; // SHOULD be set to zero when sent in a response and SHOULD be ignored when received by the client
        //uint FileNameLength; // In bytes, MUST exclude the null termination.
        public string FileName; // OEM / Unicode character array. MUST be written as SMB_STRING, and read as fixed length string.

        public FindFileNamesInfo() : base(false)
        {
        }

        public FindFileNamesInfo(byte[] buffer, ref int offset, bool isUnicode) : base(false)
        {
            NextEntryOffset = LittleEndianReader.ReadUInt32(buffer, ref offset);
            FileIndex = LittleEndianReader.ReadUInt32(buffer, ref offset);
            uint fileNameLength = LittleEndianReader.ReadUInt32(buffer, ref offset);
            FileName = SMBHelper.ReadFixedLengthString(buffer, ref offset, isUnicode, (int)fileNameLength);
        }

        public override void WriteBytes(byte[] buffer, ref int offset, bool isUnicode)
        {
            uint fileNameLength = (uint)(isUnicode ? FileName.Length * 2 : FileName.Length);

            LittleEndianWriter.WriteUInt32(buffer, ref offset, NextEntryOffset);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, FileIndex);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, fileNameLength);
            SMBHelper.WriteSMBString(buffer, ref offset, isUnicode, FileName);
        }

        public override int GetLength(bool isUnicode)
        {
            int length = FixedLength;

            if (isUnicode)
            {
                length += FileName.Length * 2 + 2;
            }
            else
            {
                length += FileName.Length + 1;
            }
            return length;
        }
    }
}
