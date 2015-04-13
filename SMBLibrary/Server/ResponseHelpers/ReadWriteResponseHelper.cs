/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SMBLibrary.RPC;
using SMBLibrary.SMB1;
using SMBLibrary.Services;
using Utilities;

namespace SMBLibrary.Server
{
    public class ReadWriteResponseHelper
    {
        internal static SMBCommand GetReadResponse(SMBHeader header, ReadRequest request, object share, StateObject state)
        {
            byte[] data = PerformRead(header, share, request.FID, request.ReadOffsetInBytes, request.CountOfBytesToRead, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_READ);
            }

            ReadResponse response = new ReadResponse();
            response.Bytes = data;
            response.CountOfBytesReturned = (ushort)data.Length;
            return response;
        }

        internal static SMBCommand GetReadResponse(SMBHeader header, ReadAndXRequest request, object share, StateObject state)
        {
            uint maxCount = request.MaxCount;
            if ((share is FileSystemShare) && state.LargeRead)
            {
                maxCount = request.MaxCountLarge;
            }
            byte[] data = PerformRead(header, share, request.FID, request.Offset, maxCount, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_READ_ANDX);
            }

            ReadAndXResponse response = new ReadAndXResponse();
            if (share is FileSystemShare)
            {
                // If the client reads from a disk file, this field MUST be set to -1 (0xFFFF)
                response.Available = 0xFFFF;
            }
            response.Data = data;
            return response;
        }

        public static byte[] PerformRead(SMBHeader header, object share, ushort FID, ulong offset, uint maxCount, StateObject state)
        {
            if (offset > Int64.MaxValue || maxCount > Int32.MaxValue)
            {
                throw new NotImplementedException("Underlying filesystem does not support unsigned offset / read count");
            }
            return PerformRead(header, share, FID, (long)offset, (int)maxCount, state);
        }

        public static byte[] PerformRead(SMBHeader header, object share, ushort FID, long offset, int maxCount, StateObject state)
        {
            OpenedFileObject openedFile = state.GetOpenedFileObject(FID);
            if (openedFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }
            string openedFilePath = openedFile.Path;

            if (share is NamedPipeShare)
            {
                return state.RetrieveNamedPipeReply(FID);
            }
            else // FileSystemShare
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;
                IFileSystem fileSystem = fileSystemShare.FileSystem;
                if (openedFile.IsSequentialAccess)
                {
                    bool isInCache = (offset >= openedFile.CacheOffset) && (offset + maxCount <= openedFile.CacheOffset + openedFile.Cache.Length);
                    if (!isInCache)
                    {
                        int bytesRead;
                        try
                        {
                            Stream stream = fileSystem.OpenFile(openedFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            stream.Seek(offset, SeekOrigin.Begin);
                            openedFile.CacheOffset = offset;
                            openedFile.Cache = new byte[OpenedFileObject.CacheSize];
                            bytesRead = stream.Read(openedFile.Cache, 0, OpenedFileObject.CacheSize);
                            stream.Close();
                        }
                        catch (IOException ex)
                        {
                            ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                            if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                            {
                                // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                                header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                                return null;
                            }
                            else
                            {
                                header.Status = NTStatus.STATUS_DATA_ERROR;
                                return null;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return null;
                        }

                        if (bytesRead < OpenedFileObject.CacheSize)
                        {
                            // EOF, we must trim the response data array
                            byte[] buffer = new byte[bytesRead];
                            Array.Copy(openedFile.Cache, 0, buffer, 0, bytesRead);
                            openedFile.Cache = buffer;
                        }
                    }

                    int offsetInCache = (int)(offset - openedFile.CacheOffset);
                    int bytesRemained = openedFile.Cache.Length - offsetInCache;
                    int dataLength = Math.Min(maxCount, bytesRemained);
                    byte[] data = new byte[dataLength];
                    Array.Copy(openedFile.Cache, offsetInCache, data, 0, dataLength);
                    return data;
                }
                else
                {
                    int bytesRead;
                    byte[] data;
                    try
                    {
                        Stream stream = fileSystem.OpenFile(openedFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        stream.Seek(offset, SeekOrigin.Begin);
                        data = new byte[maxCount];
                        bytesRead = stream.Read(data, 0, maxCount);
                        stream.Close();
                    }
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                            header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                            return null;
                        }
                        else
                        {
                            header.Status = NTStatus.STATUS_DATA_ERROR;
                            return null;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        header.Status = NTStatus.STATUS_DATA_ERROR;
                        return null;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return null;
                    }

                    if (bytesRead < maxCount)
                    {
                        // EOF, we must trim the response data array
                        byte[] buffer = new byte[bytesRead];
                        Array.Copy(data, 0, buffer, 0, bytesRead);
                        data = buffer;
                    }
                    return data;
                }
            }
        }

        internal static SMBCommand GetWriteResponse(SMBHeader header, WriteRequest request, object share, StateObject state)
        {
            ushort bytesWritten = (ushort)PerformWrite(header, share, request.FID, request.WriteOffsetInBytes, request.Data, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_WRITE_ANDX);
            }
            WriteResponse response = new WriteResponse();
            response.CountOfBytesWritten = bytesWritten;

            return response;
        }

        internal static SMBCommand GetWriteResponse(SMBHeader header, WriteAndXRequest request, object share, StateObject state)
        {
            uint bytesWritten = PerformWrite(header, share, request.FID, request.Offset, request.Data, state);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_WRITE_ANDX);
            }
            WriteAndXResponse response = new WriteAndXResponse();
            response.Count = bytesWritten;

            if (share is FileSystemShare)
            {
                // If the client wrote to a disk file, this field MUST be set to 0xFFFF.
                response.Available = 0xFFFF;
            }
            return response;
        }

        public static uint PerformWrite(SMBHeader header, object share, ushort FID, ulong offset, byte[] data, StateObject state)
        {
            OpenedFileObject openedFile = state.GetOpenedFileObject(FID);
            if (openedFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return 0;
            }
            string openedFilePath = openedFile.Path;

            if (share is NamedPipeShare)
            {
                RemoteService service = ((NamedPipeShare)share).GetService(openedFilePath);
                if (service != null)
                {
                    RPCPDU rpcRequest = RPCPDU.GetPDU(data);
                    RPCPDU rpcReply = RemoteServiceHelper.GetRPCReply(rpcRequest, service);
                    byte[] replyData = rpcReply.GetBytes();
                    state.StoreNamedPipeReply(FID, replyData);
                    return (uint)data.Length;
                }

                // This code should not execute unless the SMB request (sequence) is invalid
                header.Status = NTStatus.STATUS_INVALID_SMB;
                return 0;
            }
            else // FileSystemShare
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;
                IFileSystem fileSystem = fileSystemShare.FileSystem;

                if (openedFile.IsSequentialAccess && openedFile.Cache.Length > 0)
                {
                    openedFile.Cache = new byte[0]; // Empty cache
                }

                try
                {
                    Stream stream = fileSystem.OpenFile(openedFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    stream.Seek((long)offset, SeekOrigin.Begin);
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                    return (uint)data.Length;
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_DISK_FULL)
                    {
                        header.Status = NTStatus.STATUS_DISK_FULL;
                        return 0;
                    }
                    else if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                        header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                        return 0;
                    }
                    else
                    {
                        header.Status = NTStatus.STATUS_DATA_ERROR;
                        return 0;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    header.Status = NTStatus.STATUS_DATA_ERROR;
                    return 0;
                }
                catch (UnauthorizedAccessException)
                {
                    // The user may have tried to write to a readonly file
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return 0;
                }
            }
        }
    }
}
