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
    public struct OpenResults // 2 bytes
    {
        public OpenResult OpenResult;
        public bool OpLockGranted;

        public OpenResults(byte[] buffer, int offset)
        {
            OpenResult = (OpenResult)(buffer[offset] & 0x3);
            OpLockGranted = (buffer[offset + 1] & 0x80) > 0;
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            buffer[0] = (byte)OpenResult;
            if (OpLockGranted)
            {
                buffer[1] = 0x80;
            }
            else
            {
                buffer[1] = 0x00;
            }
        }

        public void WriteBytes(byte[] buffer, ref int offset)
        {
            WriteBytes(buffer, offset);
            offset += 2;
        }

        public static OpenResults Read(byte[] buffer, ref int offset)
        {
            offset += 2;
            return new OpenResults(buffer, offset - 2);
        }
    }
}
