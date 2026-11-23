using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WizFileUltra
{
    public static class ShellContextMenuHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ILCreateFromPath(string pszPath);

        [DllImport("shell32.dll")]
        public static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[] apidl, uint dwFlags);

        // Win32 IContextMenu P/Invoke
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214e4-0000-0000-c000-000000000046")]
        public interface IContextMenu
        {
            [PreserveSig]
            int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
            [PreserveSig]
            int InvokeCommand(ref CMINVOKECOMMANDINFOEX pici);
            [PreserveSig]
            int GetCommandString(UIntPtr idcmd, uint uflags, IntPtr reserved, [MarshalAs(UnmanagedType.LPArray)] byte[] commandstring, uint cch);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CMINVOKECOMMANDINFOEX
        {
            public int cbSize;
            public int fMask;
            public IntPtr hwnd;
            public IntPtr lpVerb;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public int dwHotKey;
            public IntPtr hIcon;
            public string lpTitle;
            public IntPtr lpVerbW;
            public string lpParametersW;
            public string lpDirectoryW;
            public string lpTitleW;
            public POINT ptInvoke;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214E6-0000-0000-C000-000000000046")]
        public interface IShellFolder
        {
            void ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
            void EnumObjects(IntPtr hwnd, int grfFlags, out IntPtr ppenumIDList);
            void BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IntPtr ppv);
            void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IntPtr ppv);
            [PreserveSig]
            int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
            void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);
            void GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref uint rgfInOut);
            [PreserveSig]
            int GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl, [In] ref Guid riid, ref uint rgfReserved, out IntPtr ppv);
            void GetDisplayNameOf(IntPtr pidl, uint uFlags, IntPtr lpName);
            void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHGetDesktopFolder(out IShellFolder ppshf);

        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        public const uint CMF_NORMAL = 0x00000000;
        public const uint CMF_EXPLORE = 0x00000004;
        public const uint TPM_RETURNCMD = 0x0100;
        public const uint TPM_RIGHTBUTTON = 0x0002;
        public const int SW_SHOWNORMAL = 1;
        public const int CMIC_MASK_UNICODE = 0x00004000;

        public static bool ShowShellContextMenu(string filePath, System.Drawing.Point pt, IntPtr hwndOwner)
        {
            if (string.IsNullOrEmpty(filePath) || (!File.Exists(filePath) && !Directory.Exists(filePath)))
                return false;

            try
            {
                IShellFolder desktopFolder;
                if (SHGetDesktopFolder(out desktopFolder) != 0 || desktopFolder == null)
                    return false;

                string parentDir = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileName(filePath);

                IntPtr parentPidl;
                uint eaten, attr = 0;
                desktopFolder.ParseDisplayName(hwndOwner, IntPtr.Zero, string.IsNullOrEmpty(parentDir) ? filePath : parentDir, out eaten, out parentPidl, ref attr);

                IShellFolder targetFolder = null;
                Guid iidShellFolder = typeof(IShellFolder).GUID;
                if (string.IsNullOrEmpty(parentDir))
                {
                    targetFolder = desktopFolder;
                }
                else
                {
                    IntPtr pv;
                    desktopFolder.BindToObject(parentPidl, IntPtr.Zero, ref iidShellFolder, out pv);
                    if (pv != IntPtr.Zero)
                    {
                        targetFolder = (IShellFolder)Marshal.GetObjectForIUnknown(pv);
                        Marshal.Release(pv);
                    }
                }

                if (targetFolder == null) targetFolder = desktopFolder;

                IntPtr filePidl;
                targetFolder.ParseDisplayName(hwndOwner, IntPtr.Zero, fileName, out eaten, out filePidl, ref attr);

                IntPtr[] pidls = new IntPtr[] { filePidl };
                Guid iidContextMenu = typeof(IContextMenu).GUID;
                IntPtr pContextMenu;
                uint reserved = 0;
                int hr = targetFolder.GetUIObjectOf(hwndOwner, 1, pidls, ref iidContextMenu, ref reserved, out pContextMenu);

                if (hr == 0 && pContextMenu != IntPtr.Zero)
                {
                    IContextMenu contextMenu = (IContextMenu)Marshal.GetObjectForIUnknown(pContextMenu);
                    IntPtr hMenu = CreatePopupMenu();

                    uint idCmdFirst = 1;
                    uint idCmdLast = 0x7FFF;
                    contextMenu.QueryContextMenu(hMenu, 0, idCmdFirst, idCmdLast, CMF_NORMAL | CMF_EXPLORE);

                    int selectedCmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, hwndOwner, IntPtr.Zero);

                    if (selectedCmd > 0)
                    {
                        CMINVOKECOMMANDINFOEX invokeInfo = new CMINVOKECOMMANDINFOEX();
                        invokeInfo.cbSize = Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX));
                        invokeInfo.fMask = CMIC_MASK_UNICODE;
                        invokeInfo.hwnd = hwndOwner;
                        invokeInfo.lpVerb = (IntPtr)(selectedCmd - idCmdFirst);
                        invokeInfo.lpVerbW = (IntPtr)(selectedCmd - idCmdFirst);
                        invokeInfo.nShow = SW_SHOWNORMAL;
                        contextMenu.InvokeCommand(ref invokeInfo);
                    }

                    DestroyMenu(hMenu);
                    Marshal.ReleaseComObject(contextMenu);
                    Marshal.Release(pContextMenu);
                    ILFree(filePidl);
                    if (parentPidl != IntPtr.Zero) ILFree(parentPidl);
                    return true;
                }

                if (filePidl != IntPtr.Zero) ILFree(filePidl);
                if (parentPidl != IntPtr.Zero) ILFree(parentPidl);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ShowShellContextMenu failed", ex);
            }

            return false;
        }
    }
}
