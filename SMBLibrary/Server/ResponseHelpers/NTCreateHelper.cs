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
using SMBLibrary.Services;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server
{
    public class NTCreateHelper
    {
        internal static SMBCommand GetNTCreateResponse(SMBHeader header, NTCreateAndXRequest request, object share, StateObject state)
        {
            bool isExtended = (request.Flags & NTCreateFlags.NT_CREATE_REQUEST_EXTENDED_RESPONSE) > 0;
            string path = request.FileName;
            if (share is NamedPipeShare)
            {
                RemoteService service = ((NamedPipeShare)share).GetService(path);
                if (service != null)
                {
                    ushort fileID = state.AddOpenedFile(path);
                    if (isExtended)
                    {
                        return CreateResponseExtendedForNamedPipe(fileID);
                    }
                    else
                    {
                        return CreateResponseForNamedPipe(fileID);
                    }
                }

                header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
            }
            else // FileSystemShare
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;
                IFileSystem fileSystem = fileSystemShare.FileSystem;
                bool forceDirectory = (request.CreateOptions & CreateOptions.FILE_DIRECTORY_FILE) > 0;
                bool forceFile = (request.CreateOptions & CreateOptions.FILE_NON_DIRECTORY_FILE) > 0;

                if (forceDirectory & (request.CreateDisposition != CreateDisposition.FILE_CREATE &&
                                      request.CreateDisposition != CreateDisposition.FILE_OPEN &&
                                      request.CreateDisposition != CreateDisposition.FILE_OPEN_IF))
                {
                    header.Status = NTStatus.STATUS_INVALID_PARAMETER;
                    return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                }

                // Windows will try to access named streams (alternate data streams) regardless of the FILE_NAMED_STREAMS flag, we need to prevent this behaviour.
                if (path.Contains(":"))
                {
                    // Windows Server 2003 will return STATUS_OBJECT_NAME_NOT_FOUND
                    header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                    return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                }

                FileSystemEntry entry = fileSystem.GetEntry(path);
                if (request.CreateDisposition == CreateDisposition.FILE_OPEN)
                {
                    if (entry == null)
                    {
                        header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                        return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                    }

                    if (entry.IsDirectory && forceFile)
                    {
                        header.Status = NTStatus.STATUS_FILE_IS_A_DIRECTORY;
                        return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                    }

                    if (!entry.IsDirectory && forceDirectory)
                    {
                        // Not sure if that's the correct response
                        header.Status = NTStatus.STATUS_OBJECT_NAME_COLLISION;
                        return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                    }
                }
                else if (request.CreateDisposition == CreateDisposition.FILE_CREATE)
                {
                    if (entry != null)
                    {
                        // File already exists, fail the request
                        header.Status = NTStatus.STATUS_OBJECT_NAME_COLLISION;
                        return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                    }

                    string userName = state.GetConnectedUserName(header.UID);
                    if (!fileSystemShare.HasWriteAccess(userName))
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                    }

                    try
                    {
                        if (forceDirectory)
                        {
                            entry = fileSystem.CreateDirectory(path);
                        }
                        else
                        {
                            entry = fileSystem.CreateFile(path);
                        }
                    }
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                            return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                        }
                        else
                        {
                            header.Status = NTStatus.STATUS_DATA_ERROR;
                            return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                    }
                }
                else if (request.CreateDisposition == CreateDisposition.FILE_OPEN_IF ||
                         request.CreateDisposition == CreateDisposition.FILE_OVERWRITE ||
                         request.CreateDisposition == CreateDisposition.FILE_OVERWRITE_IF ||
                         request.CreateDisposition == CreateDisposition.FILE_SUPERSEDE)
                {
                    entry = fileSystem.GetEntry(path);
                    if (entry == null)
                    {
                        if (request.CreateDisposition == CreateDisposition.FILE_OVERWRITE)
                        {
                            header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                            return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                        }

                        string userName = state.GetConnectedUserName(header.UID);
                        if (!fileSystemShare.HasWriteAccess(userName))
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                        }

                        try
                        {
                            if (forceDirectory)
                            {
                                entry = fileSystem.CreateDirectory(path);
                            }
                            else
                            {
                                entry = fileSystem.CreateFile(path);
                            }
                        }
                        catch (IOException ex)
                        {
                            ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                            if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                            {
                                header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                                return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                            }
                            else
                            {
                                header.Status = NTStatus.STATUS_DATA_ERROR;
                                return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                        }
                    }
                    else
                    {
                        if (request.CreateDisposition == CreateDisposition.FILE_OVERWRITE ||
                            request.CreateDisposition == CreateDisposition.FILE_OVERWRITE_IF ||
                            request.CreateDisposition == CreateDisposition.FILE_SUPERSEDE)
                        {
                            string userName = state.GetConnectedUserName(header.UID);
                            if (!fileSystemShare.HasWriteAccess(userName))
                            {
                                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                                return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                            }

                            // Truncate the file
                            try
                            {
                                Stream stream = fileSystem.OpenFile(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                                stream.Close();
                            }
                            catch (IOException ex)
                            {
                                ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                                if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                                {
                                    header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                                    return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                                }
                                else
                                {
                                    header.Status = NTStatus.STATUS_DATA_ERROR;
                                    return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                                return new ErrorResponse(CommandName.SMB_COM_NT_CREATE_ANDX);
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidRequestException();
                }
                bool isSequentialAccess = (request.CreateOptions & CreateOptions.FILE_SEQUENTIAL_ONLY) > 0;
                ushort fileID = state.AddOpenedFile(path, isSequentialAccess);
                if (isExtended)
                {
                    NTCreateAndXResponseExtended response = CreateResponseExtendedFromFileSystemEntry(entry, fileID);
                    if ((request.Flags & NTCreateFlags.NT_CREATE_REQUEST_OPBATCH) > 0)
                    {
                        response.OpLockLevel = OpLockLevel.BatchOpLockGranted;
                    }
                    return response;
                }
                else
                {
                    NTCreateAndXResponse response = CreateResponseFromFileSystemEntry(entry, fileID);
                    if ((request.Flags & NTCreateFlags.NT_CREATE_REQUEST_OPBATCH) > 0)
                    {
                        response.OpLockLevel = OpLockLevel.BatchOpLockGranted;
                    }
                    return response;
                }
            }
        }

        private static NTCreateAndXResponse CreateResponseForNamedPipe(ushort fileID)
        {
            NTCreateAndXResponse response = new NTCreateAndXResponse();
            response.FID = fileID;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            return response;
        }

        private static NTCreateAndXResponseExtended CreateResponseExtendedForNamedPipe(ushort fileID)
        {
            NTCreateAndXResponseExtended response = new NTCreateAndXResponseExtended();
            response.FID = fileID;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            NamedPipeStatus status = new NamedPipeStatus();
            status.ICount = 255;
            status.ReadMode = ReadMode.MessageMode;
            status.NamedPipeType = NamedPipeType.MessageNodePipe;
            response.NMPipeStatus = status;
            response.MaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                FileAccessMask.FILE_EXECUTE |
                                                FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                    FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                    FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                    FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            return response;
        }

        private static NTCreateAndXResponse CreateResponseFromFileSystemEntry(FileSystemEntry entry, ushort fileID)
        {
            NTCreateAndXResponse response = new NTCreateAndXResponse();
            if (entry.IsDirectory)
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Directory;
                response.Directory = true;
            }
            else
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            }
            response.FID = fileID;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.AllocationSize = InfoHelper.GetAllocationSize(entry.Size);
            response.EndOfFile = entry.Size;
            response.CreateTime = entry.CreationTime;
            response.LastAccessTime = entry.LastAccessTime;
            response.LastWriteTime = entry.LastWriteTime;
            response.LastChangeTime = entry.LastWriteTime;
            response.ResourceType = ResourceType.FileTypeDisk;
            return response;
        }

        private static NTCreateAndXResponseExtended CreateResponseExtendedFromFileSystemEntry(FileSystemEntry entry, ushort fileID)
        {
            NTCreateAndXResponseExtended response = new NTCreateAndXResponseExtended();
            if (entry.IsDirectory)
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Directory;
                response.Directory = true;
            }
            else
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            }
            response.FID = fileID;
            response.CreateTime = entry.CreationTime;
            response.LastAccessTime = entry.LastAccessTime;
            response.LastWriteTime = entry.LastWriteTime;
            response.LastChangeTime = entry.LastWriteTime;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.AllocationSize = InfoHelper.GetAllocationSize(entry.Size);
            response.EndOfFile = entry.Size;
            response.ResourceType = ResourceType.FileTypeDisk;
            response.FileStatus = FileStatus.NO_EAS | FileStatus.NO_SUBSTREAMS | FileStatus.NO_REPARSETAG;
            response.MaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                FileAccessMask.FILE_EXECUTE |
                                                FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                    FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                    FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                    FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            return response;
        }
    }
}
