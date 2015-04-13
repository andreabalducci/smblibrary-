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
    /// NTTransactQuerySecurityDescription Response
    /// </summary>
    public class NTTransactQuerySecurityDescriptorResponse : NTTransactSubcommand
    {
        public const uint ParametersLength = 4;
        // Parameters:
        public uint LengthNeeded; // We might return STATUS_BUFFER_OVERFLOW without the SecurityDescriptor field
        // Data
        public SecurityDescriptor SecurityDescriptor;

        public NTTransactQuerySecurityDescriptorResponse()
        {
        }

        public NTTransactQuerySecurityDescriptorResponse(byte[] parameters, byte[] data)
        {
            LengthNeeded = LittleEndianConverter.ToUInt32(parameters, 0);

            if (data.Length == LengthNeeded)
            {
                SecurityDescriptor = new SecurityDescriptor(data, 0);
            }
        }

        public override NTTransactSubcommandName SubcommandName
        {
            get
            {
                return NTTransactSubcommandName.NT_TRANSACT_QUERY_SECURITY_DESC;
            }
        }
    }
}
