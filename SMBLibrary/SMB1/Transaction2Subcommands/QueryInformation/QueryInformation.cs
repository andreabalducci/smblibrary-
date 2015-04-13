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
    public abstract class QueryInformation
    {
        public abstract byte[] GetBytes();

        public abstract QueryInformationLevel InformationLevel
        {
            get;
        }

        /// <summary>
        /// SMB_INFO_IS_NAME_VALID will return null
        /// </summary>
        public static QueryInformation GetQueryInformation(byte[] buffer, QueryInformationLevel informationLevel)
        {
            switch (informationLevel)
            {
                case QueryInformationLevel.SMB_INFO_STANDARD:
                    return new QueryInfoStandard(buffer, 0);
                case QueryInformationLevel.SMB_INFO_QUERY_EA_SIZE:
                    return new QueryEASize(buffer, 0);
                case QueryInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST:
                    return new QueryExtendedAttributesFromList(buffer, 0);
                case QueryInformationLevel.SMB_INFO_QUERY_ALL_EAS:
                    return new QueryAllExtendedAttributes(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_BASIC_INFO:
                    return new QueryFileBasicInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_STANDARD_INFO:
                    return new QueryFileStandardInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_EA_INFO:
                    return new QueryFileExtendedAttributeInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_NAME_INFO:
                    return new QueryFileNameInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_ALL_INFO:
                    return new QueryFileAllInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_ALT_NAME_INFO:
                    return new QueryFileAltNameInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_STREAM_INFO:
                    return new QueryFileStreamInfo(buffer, 0);
                case QueryInformationLevel.SMB_QUERY_FILE_COMPRESSION_INFO:
                    return new QueryFileCompressionInfo(buffer, 0);
                default:
                    throw new InvalidRequestException();
            }
        }
    }
}
