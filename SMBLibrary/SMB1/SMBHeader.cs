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
    public class SMBHeader
    {
        public const int Length = 32;
        public static readonly byte[] ProtocolSignature = new byte[] { 0xFF, 0x53, 0x4D, 0x42 };

        public byte[] Protocol; // byte[4], 0xFF followed by "SMB"
        public CommandName Command;
        public NTStatus Status;
        public HeaderFlags Flags;
        public HeaderFlags2 Flags2;
        //ushort PIDHigh
        public ulong SecurityFeatures;
        // public ushort Reserved;
        public ushort TID; // Tree ID
        //ushort PIDLow;
        public ushort UID; // User ID
        public ushort MID; // Multiplex ID

        public uint PID; // Process ID

        public SMBHeader()
        {
            Protocol = ProtocolSignature;
        }

        public SMBHeader(byte[] buffer)
        {
            Protocol = ByteReader.ReadBytes(buffer, 0, 4);
            //stucture size and credit charge 2 bytes each
            //ChannelSequence/Reserved is 4 bytes
            Command = (CommandName)ByteReader.ReadByte(buffer, 12);
            Status = (NTStatus)LittleEndianConverter.ToUInt32(buffer, 13);
            Flags = (HeaderFlags)ByteReader.ReadByte(buffer, 17);
            Flags2 = (HeaderFlags2)LittleEndianConverter.ToUInt16(buffer, 18);
            ushort PIDHigh = LittleEndianConverter.ToUInt16(buffer, 20);
            SecurityFeatures = LittleEndianConverter.ToUInt64(buffer, 22);
            TID = LittleEndianConverter.ToUInt16(buffer, 32);
            ushort PIDLow = LittleEndianConverter.ToUInt16(buffer, 34);
            UID = LittleEndianConverter.ToUInt16(buffer, 36);
            MID = LittleEndianConverter.ToUInt16(buffer, 38);

            PID = (uint)((PIDHigh << 16) | PIDLow);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            ushort PIDHigh = (ushort)(PID >> 16);
            ushort PIDLow = (ushort)(PID & 0xFFFF);

            ByteWriter.WriteBytes(buffer, offset + 0, Protocol);
            ByteWriter.WriteByte(buffer, offset + 4, (byte)Command);
            LittleEndianWriter.WriteUInt32(buffer, offset + 5, (uint)Status);
            ByteWriter.WriteByte(buffer, offset + 9, (byte)Flags);
            LittleEndianWriter.WriteUInt16(buffer, offset + 10, (ushort)Flags2);
            LittleEndianWriter.WriteUInt16(buffer, offset + 12, PIDHigh);
            LittleEndianWriter.WriteUInt64(buffer, offset + 14, SecurityFeatures);
            LittleEndianWriter.WriteUInt16(buffer, offset + 24, TID);
            LittleEndianWriter.WriteUInt16(buffer, offset + 26, PIDLow);
            LittleEndianWriter.WriteUInt16(buffer, offset + 28, UID);
            LittleEndianWriter.WriteUInt16(buffer, offset + 30, MID);
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[Length];
            WriteBytes(buffer, 0);
            return buffer;
        }

        public bool ReplyFlag
        {
            get
            {
                return (Flags & HeaderFlags.Reply) > 0;
            }
        }

        /// <summary>
        /// SMB_FLAGS2_EXTENDED_SECURITY
        /// </summary>
        public bool ExtendedSecurityFlag
        {
            get
            {
                return (this.Flags2 & HeaderFlags2.ExtendedSecurity) > 0;
            }
            set
            {
                if (value)
                {
                    this.Flags2 |= HeaderFlags2.ExtendedSecurity;
                }
                else
                {
                    this.Flags2 &= ~HeaderFlags2.ExtendedSecurity;
                }
            }
        }

        public bool UnicodeFlag
        {
            get
            {
                return (Flags2 & HeaderFlags2.Unicode) > 0;
            }
        }
    }
}
