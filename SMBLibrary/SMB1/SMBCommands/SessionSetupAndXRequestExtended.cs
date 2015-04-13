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
    /// SMB_COM_SESSION_SETUP_ANDX Extended Request
    /// </summary>
    public class SessionSetupAndXRequestExtended : SMBAndXCommand
    {
        public const int ParametersLength = 24;
        // Parameters:
        public ushort MaxBufferSize;
        public ushort MaxMpxCount;
        public ushort VcNumber;
        public uint SessionKey;
        //ushort SecurityBlobLength;
        public uint Reserved;
        public ServerCapabilities Capabilities;
        // Data:
        public byte[] SecurityBlob;
        public string NativeOS;     // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)
        public string NativeLanMan; // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)

        public SessionSetupAndXRequestExtended(byte[] buffer, int offset, bool isUnicode) : base(buffer, offset, isUnicode)
        {
            MaxBufferSize = LittleEndianConverter.ToUInt16(this.SMBParameters, 4);
            MaxMpxCount = LittleEndianConverter.ToUInt16(this.SMBParameters, 6);
            VcNumber = LittleEndianConverter.ToUInt16(this.SMBParameters, 8);
            SessionKey = LittleEndianConverter.ToUInt32(this.SMBParameters, 10);
            ushort securityBlobLength = LittleEndianConverter.ToUInt16(this.SMBParameters, 14);
            Reserved = LittleEndianConverter.ToUInt32(this.SMBParameters, 16);
            Capabilities = (ServerCapabilities)LittleEndianConverter.ToUInt32(this.SMBParameters, 20);

            SecurityBlob = ByteReader.ReadBytes(this.SMBData, 0, securityBlobLength);

            int dataOffset = SecurityBlob.Length;
            if (isUnicode)
            {
                int padding = securityBlobLength % 2;
                dataOffset += padding;
            }
            NativeOS = SMBHelper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
            NativeLanMan = SMBHelper.ReadSMBString(this.SMBData, ref dataOffset, isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_SESSION_SETUP_ANDX;
            }
        }
    }
}
