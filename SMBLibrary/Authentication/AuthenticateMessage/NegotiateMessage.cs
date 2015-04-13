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

namespace SMBLibrary.Authentication
{
    /// <summary>
    /// [MS-NLMP] NEGOTIATE_MESSAGE (Type 1 Message)
    /// </summary>
    public class NegotiateMessage
    {
        public string Signature; // 8 bytes
        public MessageTypeName MessageType;
        public NegotiateFlags NegotiateFlags;
        public string DomainName;
        public string Workstation;
        public Version Version;

        public NegotiateMessage()
        {
            Signature = AuthenticationMessageUtils.ValidSignature;
            MessageType = MessageTypeName.Negotiate;
            DomainName = String.Empty;
            Workstation = String.Empty;
        }

        public NegotiateMessage(byte[] buffer)
        {
            Signature = ByteReader.ReadAnsiString(buffer, 0, 8);
            MessageType = (MessageTypeName)LittleEndianConverter.ToUInt32(buffer, 8);
            NegotiateFlags = (NegotiateFlags)LittleEndianConverter.ToUInt32(buffer, 12);
            DomainName = AuthenticationMessageUtils.ReadAnsiStringBufferPointer(buffer, 16);
            Workstation = AuthenticationMessageUtils.ReadAnsiStringBufferPointer(buffer, 24);
            if ((NegotiateFlags & NegotiateFlags.NegotiateVersion) > 0)
            {
                Version = new Version(buffer, 32);
            }
        }

        public byte[] GetBytes()
        {
            int fixedLength = 32;
            if ((NegotiateFlags & NegotiateFlags.NegotiateVersion) > 0)
            {
                fixedLength += 8;
            }
            int payloadLength = DomainName.Length * 2 + Workstation.Length * 2;
            byte[] buffer = new byte[fixedLength + payloadLength];
            ByteWriter.WriteAnsiString(buffer, 0, AuthenticationMessageUtils.ValidSignature, 8);
            LittleEndianWriter.WriteUInt32(buffer, 8, (uint)MessageType);
            LittleEndianWriter.WriteUInt32(buffer, 12, (uint)NegotiateFlags);

            if ((NegotiateFlags & NegotiateFlags.NegotiateVersion) > 0)
            {
                Version.WriteBytes(buffer, 32);
            }

            int offset = fixedLength;
            AuthenticationMessageUtils.WriteBufferPointer(buffer, 16, (ushort)(DomainName.Length * 2), (uint)offset);
            ByteWriter.WriteUnicodeString(buffer, ref offset, DomainName);
            AuthenticationMessageUtils.WriteBufferPointer(buffer, 16, (ushort)(Workstation.Length * 2), (uint)offset);
            ByteWriter.WriteUnicodeString(buffer, ref offset, Workstation);

            return buffer;
        }
    }
}
