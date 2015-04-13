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
using SMBLibrary.Authentication;

namespace SMBLibrary.Server
{
    public class IndependentUserCollection : UserCollection, INTLMAuthenticationProvider
    {
        private byte[] m_serverChallenge = new byte[8];

        public IndependentUserCollection()
        {
        }

        public IndependentUserCollection(UserCollection users)
        {
            this.AddRange(users);
        }

        /// <summary>
        /// LM v1 / NTLM v1
        /// </summary>
        public User AuthenticateV1(string accountNameToAuth, byte[] serverChallenge, byte[] lmResponse, byte[] ntlmResponse)
        {
            for (int index = 0; index < this.Count; index++)
            {
                string accountName = this[index].AccountName;
                string password = this[index].Password;

                if (String.Equals(accountName, accountNameToAuth, StringComparison.InvariantCultureIgnoreCase))
                {
                    byte[] expectedLMResponse = NTAuthentication.ComputeLMv1Response(serverChallenge, password);
                    if (ByteUtils.AreByteArraysEqual(expectedLMResponse, lmResponse))
                    {
                        return this[index];
                    }

                    byte[] expectedNTLMResponse = NTAuthentication.ComputeNTLMv1Response(serverChallenge, password);
                    if (ByteUtils.AreByteArraysEqual(expectedNTLMResponse, ntlmResponse))
                    {
                        return this[index];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// LM v1 / NTLM v1 Extended Security
        /// </summary>
        public User AuthenticateV1Extended(string accountNameToAuth, byte[] serverChallenge, byte[] lmResponse, byte[] ntlmResponse)
        {
            for (int index = 0; index < this.Count; index++)
            {
                string accountName = this[index].AccountName;
                string password = this[index].Password;

                if (String.Equals(accountName, accountNameToAuth, StringComparison.InvariantCultureIgnoreCase))
                {
                    byte[] clientChallenge = ByteReader.ReadBytes(lmResponse, 0, 8);
                    byte[] expectedNTLMv1Response = NTAuthentication.ComputeNTLMv1ExtendedSecurityResponse(serverChallenge, clientChallenge, password);

                    if (ByteUtils.AreByteArraysEqual(expectedNTLMv1Response, ntlmResponse))
                    {
                        return this[index];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// LM v2 / NTLM v2
        /// </summary>
        public User AuthenticateV2(string domainNameToAuth, string accountNameToAuth, byte[] serverChallenge, byte[] lmResponse, byte[] ntlmResponse)
        {
            for (int index = 0; index < this.Count; index++)
            {
                string accountName = this[index].AccountName;
                string password = this[index].Password;

                if (String.Equals(accountName, accountNameToAuth, StringComparison.InvariantCultureIgnoreCase))
                {
                    byte[] _LMv2ClientChallenge = ByteReader.ReadBytes(lmResponse, 16, 8);
                    byte[] expectedLMv2Response = NTAuthentication.ComputeLMv2Response(serverChallenge, _LMv2ClientChallenge, password, accountName, domainNameToAuth);
                    if (ByteUtils.AreByteArraysEqual(expectedLMv2Response, lmResponse))
                    {
                        return this[index];
                    }

                    if (ntlmResponse.Length > 24)
                    {
                        NTLMv2ClientChallengeStructure clientChallengeStructure = new NTLMv2ClientChallengeStructure(ntlmResponse, 16);
                        byte[] clientChallengeStructurePadded = clientChallengeStructure.GetBytesPadded();
                        byte[] expectedNTLMv2Response = NTAuthentication.ComputeNTLMv2Response(serverChallenge, clientChallengeStructurePadded, password, accountName, domainNameToAuth);

                        if (ByteUtils.AreByteArraysEqual(expectedNTLMv2Response, ntlmResponse))
                        {
                            return this[index];
                        }
                    }
                }
            }
            return null;
        }

        public byte[] GenerateServerChallenge()
        {
            new Random().NextBytes(m_serverChallenge);
            return m_serverChallenge;
        }

        public ChallengeMessage GetChallengeMessage(byte[] negotiateMessageBytes)
        {
            byte[] serverChallenge = GenerateServerChallenge();

            ChallengeMessage message = new ChallengeMessage();
            message.NegotiateFlags = NegotiateFlags.NegotiateUnicode |
                                     NegotiateFlags.RequestTarget |
                                     NegotiateFlags.NegotiateNTLMKey |
                                     NegotiateFlags.NegotiateExtendedSecurity |
                                     NegotiateFlags.NegotiateTargetInfo |
                                     NegotiateFlags.NegotiateVersion |
                                     NegotiateFlags.Negotiate128 |
                                     NegotiateFlags.Negotiate56;
            message.TargetName = Environment.MachineName;
            message.ServerChallenge = serverChallenge;
            message.TargetInfo = AVPairUtils.GetAVPairSequence(Environment.MachineName, Environment.MachineName);
            message.Version = Authentication.Version.Server2003;
            return message;
        }

        public byte[] GetChallengeMessageBytes(byte[] negotiateMessageBytes)
        {
            ChallengeMessage message = GetChallengeMessage(negotiateMessageBytes);
            return message.GetBytes();
        }

        public User Authenticate(byte[] authenticateMessageBytes)
        {
            AuthenticateMessage message = new AuthenticateMessage(authenticateMessageBytes);
            return Authenticate(message);
        }

        public User Authenticate(AuthenticateMessage message)
        {
            User user;
            if ((message.NegotiateFlags & NegotiateFlags.NegotiateExtendedSecurity) > 0)
            {
                user = AuthenticateV1Extended(message.UserName, m_serverChallenge, message.LmChallengeResponse, message.NtChallengeResponse);
            }
            else
            {
                user = AuthenticateV1(message.UserName, m_serverChallenge, message.LmChallengeResponse, message.NtChallengeResponse);
            }

            if (user == null)
            {
                // NTLM v2
                user = AuthenticateV2(message.DomainName, message.UserName, m_serverChallenge, message.LmChallengeResponse, message.NtChallengeResponse);
            }

            return user;
        }

        public User Authenticate(string accountNameToAuth, byte[] lmResponse, byte[] ntlmResponse)
        {
            return AuthenticateV1(accountNameToAuth, m_serverChallenge, lmResponse, ntlmResponse);
        }

        public bool EnableGuestLogin
        {
            get
            {
                int index = IndexOf("Guest");
                return (index >= 0 && this[index].Password == String.Empty);
            }
        }

        public byte[] ServerChallenge
        {
            get
            {
                return m_serverChallenge;
            }
        }
    }
}
