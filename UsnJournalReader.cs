using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WizFileUltra
{
    public class UsnJournalReader
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FSCTL_QUERY_USN_JOURNAL = 0x000900f4;
        private const uint FSCTL_ENUM_USN_DATA = 0x000900b3;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct USN_JOURNAL_DATA
        {
            public ulong UsnJournalID;
            public long FirstUsn;
            public long NextUsn;
            public long LowestValidUsn;
            public long MaxUsn;
            public ulong MaximumSize;
            public ulong AllocationDelta;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MFT_ENUM_DATA
        {
            public ulong StartFileReferenceNumber;
            public long LowUsn;
            public long HighUsn;
        }

        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public static List<FileRecord> ScanDriveMFT(DriveInfo drive, Action<int> progressCallback = null)
        {
            var records = new List<FileRecord>(200000);
            string driveLetter = drive.Name.TrimEnd('\\');
            string volumePath = @"\\.\" + driveLetter;

            using (SafeFileHandle handle = CreateFile(
                volumePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    return FallbackEnumerateDrive(drive, progressCallback);
                }

                USN_JOURNAL_DATA ujd = new USN_JOURNAL_DATA();
                int ujdSize = Marshal.SizeOf(ujd);
                IntPtr ujdBuffer = Marshal.AllocHGlobal(ujdSize);

                try
                {
                    uint bytesReturned = 0;
                    bool success = DeviceIoControl(
                        handle,
                        FSCTL_QUERY_USN_JOURNAL,
                        IntPtr.Zero,
                        0,
                        ujdBuffer,
                        ujdSize,
                        out bytesReturned,
                        IntPtr.Zero);

                    if (!success)
                    {
                        return FallbackEnumerateDrive(drive, progressCallback);
                    }

                    ujd = (USN_JOURNAL_DATA)Marshal.PtrToStructure(ujdBuffer, typeof(USN_JOURNAL_DATA));

                    MFT_ENUM_DATA med = new MFT_ENUM_DATA
                    {
                        StartFileReferenceNumber = 0,
                        LowUsn = 0,
                        HighUsn = ujd.NextUsn
                    };

                    int medSize = Marshal.SizeOf(med);
                    IntPtr medBuffer = Marshal.AllocHGlobal(medSize);
                    Marshal.StructureToPtr(med, medBuffer, true);

                    int bufferSize = 64 * 1024;
                    IntPtr outputBuffer = Marshal.AllocHGlobal(bufferSize);
                    var idToRecord = new Dictionary<ulong, FileRecord>(200000);

                    try
                    {
                        int count = 0;
                        while (DeviceIoControl(handle, FSCTL_ENUM_USN_DATA, medBuffer, medSize, outputBuffer, bufferSize, out bytesReturned, IntPtr.Zero))
                        {
                            if (bytesReturned <= 8) break;

                            IntPtr currentPtr = new IntPtr(outputBuffer.ToInt64() + 8);
                            int remainingBytes = (int)bytesReturned - 8;

                            while (remainingBytes > 0)
                            {
                                int recordLength = Marshal.ReadInt32(currentPtr, 0);
                                if (recordLength <= 0 || recordLength > remainingBytes) break;

                                int majorVersion = Marshal.ReadInt16(currentPtr, 4);
                                if (majorVersion == 2 || majorVersion == 3)
                                {
                                    ulong fileRef = (ulong)Marshal.ReadInt64(currentPtr, 8);
                                    ulong parentRef = (ulong)Marshal.ReadInt64(currentPtr, 16);
                                    long usn = Marshal.ReadInt64(currentPtr, 24);
                                    long timestamp = Marshal.ReadInt64(currentPtr, 32);
                                    uint fileAttrs = (uint)Marshal.ReadInt32(currentPtr, 52);
                                    short fileNameLen = Marshal.ReadInt16(currentPtr, 56);
                                    short fileNameOffset = Marshal.ReadInt16(currentPtr, 58);

                                    string fileName = Marshal.PtrToStringUni(new IntPtr(currentPtr.ToInt64() + fileNameOffset), fileNameLen / 2);
                                    bool isDir = (fileAttrs & FILE_ATTRIBUTE_DIRECTORY) != 0;
                                    DateTime dt = DateTime.FromFileTimeUtc(timestamp).ToLocalTime();

                                    var rec = new FileRecord
                                    {
                                        FileReferenceNumber = fileRef,
                                        ParentFileReferenceNumber = parentRef,
                                        Name = fileName,
                                        IsDirectory = isDir,
                                        ModifiedDate = dt,
                                        Size = 0
                                    };

                                    ulong cleanRef = fileRef & 0x0000FFFFFFFFFFFFUL;
                                    idToRecord[cleanRef] = rec;
                                    records.Add(rec);
                                    count++;

                                    if (count % 25000 == 0)
                                    {
                                        progressCallback?.Invoke(count);
                                    }
                                }

                                currentPtr = new IntPtr(currentPtr.ToInt64() + recordLength);
                                remainingBytes -= recordLength;
                            }

                            ulong nextStart = (ulong)Marshal.ReadInt64(outputBuffer, 0);
                            Marshal.WriteInt64(medBuffer, 0, (long)nextStart);
                        }

                        string rootPrefix = drive.Name.TrimEnd('\\');
                        foreach (var rec in records)
                        {
                            rec.FullPath = BuildFullPath(rec, idToRecord, rootPrefix);
                        }

                        return records;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(medBuffer);
                        Marshal.FreeHGlobal(outputBuffer);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ujdBuffer);
                }
            }
        }

        private static string BuildFullPath(FileRecord rec, Dictionary<ulong, FileRecord> dict, string rootPrefix)
        {
            var parts = new List<string>();
            var current = rec;
            int depth = 0;

            while (current != null && depth < 100)
            {
                parts.Add(current.Name);
                ulong parentRef = current.ParentFileReferenceNumber & 0x0000FFFFFFFFFFFFUL;
                if (parentRef == (current.FileReferenceNumber & 0x0000FFFFFFFFFFFFUL)) break;
                if (!dict.TryGetValue(parentRef, out current))
                {
                    break;
                }
                depth++;
            }

            parts.Reverse();
            return rootPrefix + "\\" + string.Join("\\", parts);
        }

        public static List<FileRecord> FallbackEnumerateDrive(DriveInfo drive, Action<int> progressCallback = null)
        {
            var list = new List<FileRecord>(10000);
            int count = 0;
            var queue = new Queue<string>();
            queue.Enqueue(drive.RootDirectory.FullName);

            while (queue.Count > 0)
            {
                string dir = queue.Dequeue();
                try
                {
                    DirectoryInfo di = new DirectoryInfo(dir);
                    foreach (var d in di.EnumerateDirectories())
                    {
                        var rec = new FileRecord
                        {
                            Name = d.Name,
                            FullPath = d.FullName,
                            IsDirectory = true,
                            ModifiedDate = d.LastWriteTime,
                            Size = 0
                        };
                        list.Add(rec);
                        queue.Enqueue(d.FullName);
                        count++;
                        if (count % 5000 == 0) progressCallback?.Invoke(count);
                    }

                    foreach (var f in di.EnumerateFiles())
                    {
                        var rec = new FileRecord
                        {
                            Name = f.Name,
                            FullPath = f.FullName,
                            IsDirectory = false,
                            ModifiedDate = f.LastWriteTime,
                            Size = f.Length
                        };
                        list.Add(rec);
                        count++;
                        if (count % 5000 == 0) progressCallback?.Invoke(count);
                    }
                }
                catch { }
            }

            return list;
        }
    }
}
