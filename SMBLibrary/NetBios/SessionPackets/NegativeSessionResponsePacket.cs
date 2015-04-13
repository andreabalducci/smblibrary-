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

namespace SMBLibrary.NetBios
{
    public class NegativeSessionResponsePacket : SessionPacket
    {
        public byte ErrorCode;

        public NegativeSessionResponsePacket() : base()
        {
            this.Type = SessionPacketTypeName.NegativeSessionResponse;
        }

        public NegativeSessionResponsePacket(byte[] buffer) : base(buffer)
        {
            ErrorCode = ByteReader.ReadByte(this.Trailer, 0);
        }

        public override byte[] GetBytes()
        {
            this.Trailer = new byte[1];
            this.Trailer[0] = ErrorCode;

            return base.GetBytes();
        }
    }
}
