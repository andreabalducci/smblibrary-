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

namespace SMBLibrary.RPC
{
    /// <summary>
    /// rpcconn_response_hdr_t
    /// </summary>
    public class ResponsePDU : RPCPDU
    {
        private uint AllocationHint;
        public ushort ContextID;
        public byte CancelCount;
        public byte Reserved;
        public byte[] Data;
        public byte[] AuthVerifier;

        public ResponsePDU() : base()
        {
            PacketType = PacketTypeName.Response;
            AuthVerifier = new byte[0];
        }

        public ResponsePDU(byte[] buffer) : base(buffer)
        {
            int offset = RPCPDU.CommonFieldsLength;
            AllocationHint = LittleEndianReader.ReadUInt32(buffer, ref offset);
            ContextID = LittleEndianReader.ReadUInt16(buffer, ref offset);
            CancelCount = ByteReader.ReadByte(buffer, ref offset);
            Reserved = ByteReader.ReadByte(buffer, ref offset);
            int dataLength = FragmentLength - AuthLength - offset;
            Data = ByteReader.ReadBytes(buffer, ref offset, dataLength);
            AuthVerifier = ByteReader.ReadBytes(buffer, offset, AuthLength);
        }

        public override byte[] GetBytes()
        {
            AuthLength = (ushort)AuthVerifier.Length;
            FragmentLength = (ushort)(RPCPDU.CommonFieldsLength + 8 + Data.Length + AuthVerifier.Length);
            AllocationHint = (ushort)Data.Length;
            
            byte[] buffer = new byte[FragmentLength];
            WriteCommonFieldsBytes(buffer);
            int offset = RPCPDU.CommonFieldsLength;
            LittleEndianWriter.WriteUInt32(buffer, ref offset, AllocationHint);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, ContextID);
            ByteWriter.WriteByte(buffer, ref offset, CancelCount);
            ByteWriter.WriteByte(buffer, ref offset, Reserved);
            ByteWriter.WriteBytes(buffer, ref offset, Data);
            ByteWriter.WriteBytes(buffer, ref offset, AuthVerifier);
            return buffer;
        }
    }
}
