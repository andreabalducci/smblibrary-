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
    /// SMB_COM_OPEN_ANDX Request
    /// </summary>
    public class OpenAndXRequest : SMBAndXCommand
    {
        public const int ParametersLength = 30;
        // Parameters:
        //CommandName AndXCommand;
        //byte AndXReserved;
        //ushort AndXOffset;
        public OpenFlags Flags;
        public AccessModeOptions AccessMode;
        public FileAttributes SearchAttrs;
        public FileAttributes FileAttrs;
        public DateTime CreationTime; // UTime
        public OpenMode OpenMode;
        public uint AllocationSize;
        public uint Timeout;
        public uint Reserved;
        // Data:
        public string FileName; // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)

        public OpenAndXRequest() : base()
        {
        }

        public OpenAndXRequest(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            int parametersOffset = 4;
            Flags = (OpenFlags)LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            AccessMode = AccessModeOptions.Read(this.SMBParameters, ref parametersOffset);
            SearchAttrs = (FileAttributes)LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            FileAttrs = (FileAttributes)LittleEndianReader.ReadUInt16(this.SMBParameters, ref parametersOffset);
            CreationTime = SMBHelper.ReadUTime(this.SMBParameters, ref parametersOffset);
            OpenMode = OpenMode.Read(this.SMBParameters, ref parametersOffset);
            AllocationSize = LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);
            Timeout = LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);
            Reserved = LittleEndianReader.ReadUInt32(this.SMBParameters, ref parametersOffset);

            int dataOffset = 0;
            if (isUnicode)
            {
                dataOffset = 1; // 1 byte padding for 2 byte alignment
            }
            FileName = SMBHelper.ReadSMBString(this.SMBData, dataOffset, isUnicode);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            throw new NotImplementedException();
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_OPEN_ANDX;
            }
        }
    }
}
