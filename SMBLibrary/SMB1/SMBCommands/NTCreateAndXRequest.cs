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
    /// SMB_COM_NT_CREATE_ANDX Request
    /// </summary>
    public class NTCreateAndXRequest : SMBAndXCommand
    {
        public const int ParametersLength = 48;
        // Parameters:
        //CommandName AndXCommand;
        //byte AndXReserved;
        //ushort AndXOffset;
        public byte Reserved;
        //ushort NameLength; // in bytes
        public NTCreateFlags Flags;
        public uint RootDirectoryFID;
        public DesiredAccess DesiredAccess;
        public ulong AllocationSize;
        public ExtendedFileAttributes ExtFileAttributes;
        public ShareAccess ShareAccess;
        public CreateDisposition CreateDisposition;
        public CreateOptions CreateOptions;
        public ImpersonationLevel ImpersonationLevel;
        public SecurityFlags SecurityFlags;
        // Data:
        public string FileName; // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)

        public NTCreateAndXRequest() : base()
        {
        }

        public NTCreateAndXRequest(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            Reserved = ByteReader.ReadByte(this.SMBParameters, 4);
            ushort nameLength = LittleEndianConverter.ToUInt16(this.SMBParameters, 5);
            Flags = (NTCreateFlags)LittleEndianConverter.ToUInt32(this.SMBParameters, 7);
            RootDirectoryFID = LittleEndianConverter.ToUInt32(this.SMBParameters, 11);
            DesiredAccess = (DesiredAccess)LittleEndianConverter.ToUInt32(this.SMBParameters, 15);
            AllocationSize = LittleEndianConverter.ToUInt64(this.SMBParameters, 19);
            ExtFileAttributes = (ExtendedFileAttributes)LittleEndianConverter.ToUInt32(this.SMBParameters, 27);
            ShareAccess = (ShareAccess)LittleEndianConverter.ToUInt32(this.SMBParameters, 31);
            CreateDisposition = (CreateDisposition)LittleEndianConverter.ToUInt32(this.SMBParameters, 35);
            CreateOptions = (CreateOptions)LittleEndianConverter.ToUInt32(this.SMBParameters, 39);
            ImpersonationLevel = (ImpersonationLevel)LittleEndianConverter.ToUInt32(this.SMBParameters, 43);
            SecurityFlags = (SecurityFlags)ByteReader.ReadByte(this.SMBParameters, 47);

            int dataOffset = 0;
            if (isUnicode)
            {
                dataOffset = 1; // 1 byte padding for 2 byte alignment
            }
            FileName = SMBHelper.ReadSMBString(this.SMBData, dataOffset, isUnicode);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            ushort nameLength = (ushort)FileName.Length;
            this.SMBParameters = new byte[ParametersLength];
            ByteWriter.WriteByte(this.SMBParameters, 0, (byte)AndXCommand);
            ByteWriter.WriteByte(this.SMBParameters, 1, AndXReserved);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 2, AndXOffset);
            ByteWriter.WriteByte(this.SMBParameters, 4, Reserved);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 5, nameLength);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 7, (uint)Flags);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 11, (uint)RootDirectoryFID);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 15, (uint)DesiredAccess);
            LittleEndianWriter.WriteUInt64(this.SMBParameters, 19, (ulong)AllocationSize);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 27, (uint)ExtFileAttributes);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 31, (uint)ShareAccess);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 35, (uint)CreateDisposition);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 39, (uint)CreateOptions);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 43, (uint)ImpersonationLevel);
            ByteWriter.WriteByte(this.SMBParameters, 47, (byte)SecurityFlags);

            if (isUnicode)
            {
                int padding = 1;
                this.SMBData = new byte[padding + FileName.Length * 2 + 2];
                int offset = padding;
                ByteWriter.WriteNullTerminatedUnicodeString(this.SMBData, offset, FileName);
            }
            else
            {
                this.SMBData = new byte[FileName.Length + 1];
                ByteWriter.WriteNullTerminatedUnicodeString(this.SMBData, 0, FileName);
            }

            return base.GetBytes(isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_NT_CREATE_ANDX;
            }
        }
    }
}