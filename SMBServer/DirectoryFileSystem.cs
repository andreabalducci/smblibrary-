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
using Utilities;

namespace SMBServer
{
    public class DirectoryFileSystem : FileSystem
    {
        private DirectoryInfo m_directory;

        public DirectoryFileSystem(string path) : this(new DirectoryInfo(path))
        {
        }

        public DirectoryFileSystem(DirectoryInfo directory)
        {
            m_directory = directory;
        }

        public override FileSystemEntry GetEntry(string path)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            if (File.Exists(fullPath))
            {
                FileInfo file = new FileInfo(fullPath);
                bool isHidden = (file.Attributes & FileAttributes.Hidden) > 0;
                bool isReadonly = (file.Attributes & FileAttributes.ReadOnly) > 0;
                bool isArchived = (file.Attributes & FileAttributes.Archive) > 0;
                return new FileSystemEntry(path, file.Name, false, (ulong)file.Length, file.CreationTime, file.LastWriteTime, file.LastAccessTime, isHidden, isReadonly, isArchived);
            }
            else if (Directory.Exists(fullPath))
            {
                DirectoryInfo directory = new DirectoryInfo(fullPath);
                string fullName = FileSystem.GetDirectoryPath(path);
                bool isHidden = (directory.Attributes & FileAttributes.Hidden) > 0;
                bool isReadonly = (directory.Attributes & FileAttributes.ReadOnly) > 0;
                bool isArchived = (directory.Attributes & FileAttributes.Archive) > 0;
                return new FileSystemEntry(fullName, directory.Name, true, 0, directory.CreationTime, directory.LastWriteTime, directory.LastAccessTime, isHidden, isReadonly, isArchived);
            }
            else
            {
                return null;
            }
        }

        public override FileSystemEntry CreateFile(string path)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            FileStream stream = File.Create(fullPath);
            stream.Close();
            return GetEntry(path);
        }

        public override FileSystemEntry CreateDirectory(string path)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            Directory.CreateDirectory(fullPath);

            return GetEntry(path);
        }

        public override void Move(string source, string destination)
        {
            ValidatePath(source);
            ValidatePath(destination);
            string sourcePath = m_directory.FullName + source;
            string destinationPath = m_directory.FullName + destination;
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, destinationPath);
            }
            else // Entry is a directory
            {
                Directory.Move(sourcePath, destinationPath);
            }
        }

        public override void Delete(string path)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public override List<FileSystemEntry> ListEntriesInDirectory(string path)
        {
            ValidatePath(path);
            if (path == String.Empty)
            {
                path = @"\";
            }
            string fullPath = m_directory.FullName + path;
            DirectoryInfo directory = new DirectoryInfo(fullPath);
            List<FileSystemEntry> result = new List<FileSystemEntry>();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                string fullName = GetRelativeDirectoryPath(subDirectory.FullName);
                bool isHidden = (subDirectory.Attributes & FileAttributes.Hidden) > 0;
                bool isReadonly = (subDirectory.Attributes & FileAttributes.ReadOnly) > 0;
                bool isArchived = (subDirectory.Attributes & FileAttributes.Archive) > 0;
                result.Add(new FileSystemEntry(fullName, subDirectory.Name, true, 0, subDirectory.CreationTime, subDirectory.LastWriteTime, subDirectory.LastAccessTime, isHidden, isReadonly, isArchived));
            }
            foreach (FileInfo file in directory.GetFiles())
            {
                string fullName = GetRelativePath(file.FullName);
                bool isHidden = (file.Attributes & FileAttributes.Hidden) > 0;
                bool isReadonly = (file.Attributes & FileAttributes.ReadOnly) > 0;
                bool isArchived = (file.Attributes & FileAttributes.Archive) > 0;
                result.Add(new FileSystemEntry(fullName, file.Name, false, (ulong)file.Length, file.CreationTime, file.LastWriteTime, file.LastAccessTime, isHidden, isReadonly, isArchived));
            }
            return result;
        }

        public override Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            return new FileInfo(fullPath).Open(mode, access, share);
        }

        public override void SetAttributes(string path, bool? isHidden, bool? isReadonly, bool? isArchived)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                FileAttributes attributes = File.GetAttributes(fullPath);
                if (isHidden.HasValue)
                {
                    if (isHidden.Value)
                    {
                        attributes |= FileAttributes.Hidden;
                    }
                    else
                    {
                        attributes &= ~FileAttributes.Hidden;
                    }
                }

                if (isReadonly.HasValue)
                {
                    if (isReadonly.Value)
                    {
                        attributes |= FileAttributes.ReadOnly;
                    }
                    else
                    {
                        attributes &= ~FileAttributes.ReadOnly;
                    }
                }

                if (isArchived.HasValue)
                {
                    if (isArchived.Value)
                    {
                        attributes |= FileAttributes.Archive;
                    }
                    else
                    {
                        attributes &= ~FileAttributes.Archive;
                    }
                }

                File.SetAttributes(fullPath, attributes);
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public override void SetDates(string path, DateTime? creationDT, DateTime? lastWriteDT, DateTime? lastAccessDT)
        {
            ValidatePath(path);
            string fullPath = m_directory.FullName + path;
            if (File.Exists(fullPath))
            {
                if (creationDT.HasValue)
                {
                    File.SetCreationTime(fullPath, creationDT.Value);
                }

                if (lastWriteDT.HasValue)
                {
                    File.SetLastWriteTime(fullPath, lastWriteDT.Value);
                }

                if (lastAccessDT.HasValue)
                {
                    File.SetLastAccessTime(fullPath, lastAccessDT.Value);
                }
            }
            else if (Directory.Exists(fullPath))
            {
                if (creationDT.HasValue)
                {
                    Directory.SetCreationTime(fullPath, creationDT.Value);
                }

                if (lastWriteDT.HasValue)
                {
                    Directory.SetLastWriteTime(fullPath, lastWriteDT.Value);
                }

                if (lastAccessDT.HasValue)
                {
                    Directory.SetLastAccessTime(fullPath, lastAccessDT.Value);
                }
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        private void ValidatePath(string path)
        {
            if (path.StartsWith(@"..\") || path.Contains(@"\..\"))
            {
                throw new UnauthorizedAccessException("Given path is not allowed");
            }
        }

        public override string Name
        {
            get
            {
                return "DirFS";
            }
        }

        public override long Size
        {
            get
            {
                DriveInfo drive = new DriveInfo(m_directory.FullName.Substring(0, 2));
                return drive.TotalSize;
            }
        }

        public override long FreeSpace
        {
            get
            {
                DriveInfo drive = new DriveInfo(m_directory.FullName.Substring(0, 2));
                return drive.AvailableFreeSpace;
            }
        }

        private string GetRelativePath(string fullPath)
        {
            return fullPath.Substring(m_directory.FullName.Length);
        }

        private string GetRelativeDirectoryPath(string fullPath)
        {
            return GetRelativePath(GetDirectoryPath(fullPath));
        }
    }
}
