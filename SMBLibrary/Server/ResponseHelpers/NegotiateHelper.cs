/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.Authentication;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server
{
    /// <summary>
    /// Negotiate and Session Setup helper
    /// </summary>
    public class NegotiateHelper
    {
        internal static NegotiateResponseNTLM GetNegotiateResponse(SMBHeader header, NegotiateRequest request, byte[] serverChallenge)
        {
            NegotiateResponseNTLM response = new NegotiateResponseNTLM();

            response.DialectIndex = (ushort)request.Dialects.IndexOf(SMBServer.NTLanManagerDialect);
            response.SecurityMode = SecurityMode.UserSecurityMode | SecurityMode.EncryptPasswords;
            response.MaxMpxCount = 50;
            response.MaxNumberVcs = 1;
            response.MaxBufferSize = 16644;
            response.MaxRawSize = 65536;
            response.Capabilities = ServerCapabilities.Unicode |
                                    ServerCapabilities.LargeFiles |
                                    ServerCapabilities.NTSMB |
                                    ServerCapabilities.NTStatusCode |
                                    ServerCapabilities.NTFind |
                                    ServerCapabilities.LargeRead |
                                    ServerCapabilities.LargeWrite;
            response.SystemTime = DateTime.UtcNow;
            response.ServerTimeZone = (short)-TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            response.Challenge = serverChallenge;
            response.DomainName = String.Empty;
            response.ServerName = String.Empty;

            return response;
        }

        internal static NegotiateResponseNTLMExtended GetNegotiateResponseExtended(NegotiateRequest request, Guid serverGuid)
        {
            NegotiateResponseNTLMExtended response = new NegotiateResponseNTLMExtended();
            response.DialectIndex = (ushort)request.Dialects.IndexOf(SMBServer.NTLanManagerDialect);
            response.SecurityMode = SecurityMode.UserSecurityMode | SecurityMode.EncryptPasswords;
            response.MaxMpxCount = 50;
            response.MaxNumberVcs = 1;
            response.MaxBufferSize = 16644;
            response.MaxRawSize = 65536;
            response.Capabilities = ServerCapabilities.Unicode |
                                    ServerCapabilities.LargeFiles |
                                    ServerCapabilities.NTSMB |
                                    ServerCapabilities.NTStatusCode |
                                    ServerCapabilities.NTFind |
                                    ServerCapabilities.LargeRead |
                                    ServerCapabilities.LargeWrite |
                                    ServerCapabilities.ExtendedSecurity;
            response.SystemTime = DateTime.UtcNow;
            response.ServerTimeZone = (short)-TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            response.ServerGuid = serverGuid;

            return response;
        }

        internal static SMBCommand GetSessionSetupResponse(SMBHeader header, SessionSetupAndXRequest request, INTLMAuthenticationProvider users, StateObject state)
        {
            SessionSetupAndXResponse response = new SessionSetupAndXResponse();
            // The PrimaryDomain field in the request is used to determine with domain controller should authenticate the user credentials,
            // However, the domain controller itself does not use this field.
            // See: http://msdn.microsoft.com/en-us/library/windows/desktop/aa378749%28v=vs.85%29.aspx
            User user;
            try
            {
                user = users.Authenticate(request.AccountName, request.OEMPassword, request.UnicodePassword);
            }
            catch(EmptyPasswordNotAllowedException)
            {
                header.Status = NTStatus.STATUS_ACCOUNT_RESTRICTION;
                return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
            }

            if (user != null)
            {
                response.PrimaryDomain = request.PrimaryDomain;
                header.UID = state.AddConnectedUser(user.AccountName);
            }
            else if (users.EnableGuestLogin)
            {
                response.Action = SessionSetupAction.SetupGuest;
                response.PrimaryDomain = request.PrimaryDomain;
                header.UID = state.AddConnectedUser("Guest");
            }
            else
            {
                header.Status = NTStatus.STATUS_LOGON_FAILURE;
                return new ErrorResponse(CommandName.SMB_COM_SESSION_SETUP_ANDX);
            }
            if ((request.Capabilities & ServerCapabilities.LargeRead) > 0)
            {
                state.LargeRead = true;
            }
            if ((request.Capabilities & ServerCapabilities.LargeWrite) > 0)
            {
                state.LargeWrite = true;
            }
            response.NativeOS = String.Empty; // "Windows Server 2003 3790 Service Pack 2"
            response.NativeLanMan = String.Empty; // "Windows Server 2003 5.2"

            return response;
        }

        internal static SMBCommand GetSessionSetupResponseExtended(SMBHeader header, SessionSetupAndXRequestExtended request, INTLMAuthenticationProvider users, StateObject state)
        {
            SessionSetupAndXResponseExtended response = new SessionSetupAndXResponseExtended();

            response.Action = SessionSetupAction.SetupGuest;
            header.UID = state.AddConnectedUser("Guest");
            response.NativeOS = String.Empty; // "Windows Server 2003 3790 Service Pack 2"
            response.NativeLanMan = String.Empty; // "Windows Server 2003 5.2"

            return response;
        }
    }
}
