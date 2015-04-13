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
    public class SessionRetargetResponsePacket : SessionPacket
    {
        uint IPAddress;
        ushort Port;

        public SessionRetargetResponsePacket() : base()
        {
            this.Type = SessionPacketTypeName.RetargetSessionResponse;
        }

        public SessionRetargetResponsePacket(byte[] buffer) : base(buffer)
        {
            IPAddress = BigEndianConverter.ToUInt32(this.Trailer, 0);
            Port = BigEndianConverter.ToUInt16(this.Trailer, 4);
        }

        public override byte[] GetBytes()
        {
            this.Trailer = new byte[6];
            BigEndianWriter.WriteUInt32(this.Trailer, 0, IPAddress);
            BigEndianWriter.WriteUInt16(this.Trailer, 4, Port);
            return base.GetBytes();
        }
    }
}
