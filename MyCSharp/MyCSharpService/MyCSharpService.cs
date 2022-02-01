﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Management;
using System.Security.Principal;
using Microsoft.Win32;
using MyCSharpService;

namespace MyCSharp.Service
{
    public class Win32API
    {
        public static bool CreateStreamFile(string FileName)
        {
            bool bStreamFile = false;
            IntPtr FileHandle;
            string StreamName = FileName + ":MiniFlt";

            FileHandle = NativeAPI.CreateFile(StreamName, NativeAPI.GENERIC_READ | NativeAPI.GENERIC_WRITE,
                0, IntPtr.Zero, FileMode.CreateNew, 0, IntPtr.Zero);

            if (FileHandle.ToInt32() != NativeAPI.INVALID_HANDLE_VALUE)
            {
            NativeAPI.CloseHandle(FileHandle);
                bStreamFile = true;
            }
            else if (NativeAPI.ERROR_FILE_EXISTS == NativeAPI.GetLastError())
            {
                bStreamFile = true;
            }

            return bStreamFile;
        }

        public static bool CheckStreamFile(string FileName)
        {
            bool bStreamFile = false;
            IntPtr FileHandle;
            NativeAPI.IO_STATUS_BLOCK IoStatusBlock = new NativeAPI.IO_STATUS_BLOCK();
            uint BufSize = 0x10000;   //initial buffer size of 65536 bytes
            IntPtr pBuffer = Marshal.AllocHGlobal((int)BufSize);

            FileHandle = NativeAPI.CreateFile(FileName, NativeAPI.GENERIC_READ | NativeAPI.GENERIC_WRITE,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (FileHandle.ToInt32() != NativeAPI.INVALID_HANDLE_VALUE)
            {
                NativeAPI.NTSTATUS Status = NativeAPI.NtQueryInformationFile(FileHandle, ref IoStatusBlock, pBuffer, BufSize,
                    NativeAPI.FILE_INFORMATION_CLASS.FileStreamInformation);

            NativeAPI.CloseHandle(FileHandle);

                if (Status == NativeAPI.NTSTATUS.STATUS_SUCCESS)
                {
                    int StructSize = Marshal.SizeOf(typeof(NativeAPI.FILE_STREAM_INFORMATION));
                    NativeAPI.FILE_STREAM_INFORMATION FileStreamInfo;
                    string StreamName;
                    IntPtr DataPtr = pBuffer;

                    do
                    {
                        FileStreamInfo = (NativeAPI.FILE_STREAM_INFORMATION)Marshal.PtrToStructure(DataPtr, typeof(NativeAPI.FILE_STREAM_INFORMATION));

                        if (FileStreamInfo.StreamNameLen == 0) break;

                        StreamName = Marshal.PtrToStringUni(DataPtr + StructSize - 2, (int)FileStreamInfo.StreamNameLen / 2);
                        if (NativeAPI.CheckStreamName.Equals(StreamName) == true)
                        {
                            bStreamFile = true;
                            break;
                        }

                        DataPtr += (int)FileStreamInfo.NextEntryOffset;
                    } while (FileStreamInfo.NextEntryOffset != 0);
                }
            }

            Marshal.FreeHGlobal(pBuffer);

            return bStreamFile;
        }

        public static bool CheckTargetFile(string FileName, string StreamName)
        {
            bool bCheckFile = false;
            IntPtr FileHandle;

            string streamName = FileName + ":" + StreamName;
            FileHandle = NativeAPI.CreateFile(streamName, NativeAPI.GENERIC_READ | NativeAPI.GENERIC_WRITE,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (FileHandle.ToInt32() != NativeAPI.INVALID_HANDLE_VALUE) // skip file target
            {
                bCheckFile = true;

                NativeAPI.CloseHandle(FileHandle);
            }

            return bCheckFile;
        }

        public bool CheckUseFile(string fileName)
        {
            bool bUse = false;

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Close();
                    bUse = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ErrMsg[{0}]", e.Message.ToString());
            }

            return bUse;
        }

        public static bool CheckIPType(string path)
        {
            Regex regex = new Regex(@"^\\\\(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])");

            return regex.IsMatch(path);
        }

        public static bool CheckLocalDrive(string path)
        {
            var drvs = DriveInfo.GetDrives().Where(e => e.IsReady && (e.DriveType == DriveType.Fixed));

            foreach (DriveInfo drv in drvs)
            {
                if (char.ToLower(drv.Name[0]) == char.ToLower(path[0]))
                {
                    return true;
                }
            }

            return false;
        }

        public static string executeCMD(string textCMD)
        {
            ProcessStartInfo pri = new ProcessStartInfo();
            Process pro = new Process();

            pri.FileName = @"cmd.exe";
            pri.CreateNoWindow = false;
            pri.UseShellExecute = false;

            pri.RedirectStandardInput = true;
            pri.RedirectStandardOutput = true;
            pri.RedirectStandardError = true;

            pro.StartInfo = pri;
            pro.Start();

            pro.StandardInput.Write(textCMD + Environment.NewLine);
            pro.StandardInput.Close();

            System.IO.StreamReader sr = pro.StandardError;

            string resultValue = sr.ReadToEnd();
            pro.WaitForExit();
            pro.Close();

            return resultValue == "" ? "" : resultValue;
        }

        public bool checkDiskSize(string fileName, long fileSize)
        {
            var drvs = DriveInfo.GetDrives().Where(e => e.IsReady && (e.DriveType == DriveType.Fixed));

            foreach (DriveInfo drv in drvs)
            {
                if (char.ToLower(drv.Name[0]) == char.ToLower(fileName[0]))
                {
                    if (fileSize >= drv.AvailableFreeSpace) return false;
                }
            }

            return true;
        }

        public int ConnectRemoteServerStart(string server, string netUserId, string netPwd)
        {
            int index, retValue = 0;
            string tempName = server.Substring(2);
            string[] hostName = tempName.Split(new char[] { '\\' });

            string Msg = executeCMD(string.Format("net use {0} /user:{1}\\{2} {3} /PERSISTENT:YES", server, hostName[0], netUserId, netPwd));

            if (string.IsNullOrEmpty(Msg) == false)
            {
                index = Msg.IndexOf("86");
                if (index != -1) retValue = 86; //account error
                else
                {
                    index = Msg.IndexOf("1326");
                    if (index != -1) retValue = 86; //account error
                    else retValue = 53; //network error
                }
            }
            return retValue;
        }

        public void CloseRemoteServerEnd(string server)
        {
            executeCMD(string.Format("net use /delete {0}", server));
        }

        public bool CheckDirectoryInfo(string path)
        {
            bool bSetProperties = false;

            DirectoryInfo checkDir = new DirectoryInfo(path);
            if ((checkDir.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
            {
                bSetProperties = true;
            }
            else if ((checkDir.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted)
            {
                bSetProperties = true;
            }

            return bSetProperties;
        }

        public bool CheckDiskSize(string fileName, long fileSize)
        {
            var drvs = DriveInfo.GetDrives().Where(e => e.IsReady && (e.DriveType == DriveType.Fixed));

            foreach (DriveInfo drv in drvs)
            {
                if (char.ToLower(drv.Name[0]) == char.ToLower(fileName[0]))
                {
                    if (fileSize >= drv.AvailableFreeSpace) return false;
                }
            }

            return true;
        }

        public static string GetUNCPathFromHostName(string path)
        {
            string hostName = "";
            Regex regex = new Regex(@"^\\\\(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])");

            if (regex.IsMatch(path))
            {
                string ipName = path.Substring(2);
                string[] name = ipName.Split(new char[] { '\\' });

                try
                {
                    IPHostEntry IpHost = Dns.GetHostEntry(name[0]);
                    hostName = @"\\" + IpHost.HostName + path.Substring(2 + name[0].Length);

                }
                catch (System.Net.Sockets.SocketException e)
                {

                }
            }

            return hostName;
        }


        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WNetGetConnection([MarshalAs(UnmanagedType.LPTStr)] string localName,
                                                [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName,
                                                ref int length);
        public static string GetUNCPath(string originalPath)
        {
            StringBuilder sb = new StringBuilder(512);
            int size = sb.Capacity;

            if (originalPath.Length > 2 && originalPath[1] == ':')
            {
                char c = originalPath[0];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    int error = WNetGetConnection(originalPath.Substring(0, 2), sb, ref size);
                    if (error == 0)
                    {
                        DirectoryInfo dir = new DirectoryInfo(originalPath);
                        string path = System.IO.Path.GetFullPath(originalPath).Substring(System.IO.Path.GetPathRoot(originalPath).Length);
                        return System.IO.Path.Combine(sb.ToString().TrimEnd(), path);
                    }
                }
            }

            return string.Empty;
        }
        public static string GetIPInfo()
        {
            string IPString = "";

            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList.FirstOrDefault(
                                        a => a.AddressFamily == AddressFamily.InterNetwork);

                IPString = ipAddress != null ? ipAddress.ToString() : "";
            }
            catch (Exception ex)
            {
            }

            return IPString;
        }

        public static List<string> GetProcessInfo()
        {
            ManagementClass management = new ManagementClass("Win32_Process");
            ManagementObjectCollection mCollection = management.GetInstances();

            List<string> processList = new List<string>();

            foreach (ManagementObject ps in mCollection)
            {
                if ((string)ps["ExecutablePath"] != null)
                {
                    processList.Add((string)ps["ExecutablePath"]);
                }
            }

            return processList;
        }

        public static bool IsLocalDrive(string path)
        {
            var drvs = DriveInfo.GetDrives().Where(e => e.IsReady && (e.DriveType == DriveType.Fixed));

            foreach (DriveInfo drv in drvs)
            {
                if (char.ToLower(drv.Name[0]) == char.ToLower(path[0]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CreateDir(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                return false;
            }

            Directory.CreateDirectory(dirPath);

            DirectoryInfo dInfo = new DirectoryInfo(dirPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                                            FileSystemRights.FullControl,
                                                            InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                            PropagationFlags.NoPropagateInherit,
                                                            AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);

            return true;
        }

        public string MyReadFile(string fileName, int length)
        {
            byte[] buffer = new byte[length];

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileSystemRights.Read, FileShare.Read, (length - 1), FileOptions.Asynchronous))
            {
                fs.Read(buffer, 0, length);
            }

            return Encoding.Default.GetString(buffer);
        }

        public void MyWriteFile(string fileName, string data, int length)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(data);
                }
            }
        }

        public void CreateRegistrySubKeyValueString(string subKeyName, string keyName, object data)
        {
            using (RegistryKey regKey = Registry.LocalMachine.CreateSubKey(subKeyName))
            {
                regKey.SetValue(keyName, (string)data, RegistryValueKind.String);
            }
        }

        public void CreateRegistrySubKeyValueType(RegistryValueKind regType, string subKeyName, string keyName, object data)
        {
            using (RegistryKey regKey = Registry.LocalMachine.CreateSubKey(subKeyName))
            {
            }
            if (data != null)
            {
                if (regType == RegistryValueKind.DWord)
                {
                    SetRegistryKeyValueDWORD(subKeyName, keyName, data);
                }
                else if (regType == RegistryValueKind.Binary)
                {
                    SetRegistryKeyValueBinary(subKeyName, keyName, data);
                }
            }
        }

        public void SetRegistryKeyValueString(string regPath, string keyName, object data)
        {
            if (data != null)
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regPath, true))
                {
                    regKey.SetValue(keyName, (string)data, RegistryValueKind.String);
                }
            }
        }

        public void SetRegistryKeyValueDWORD(string regPath, string keyName, object data)
        {
            if (data != null)
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regPath, true))
                {
                    regKey.SetValue(keyName, (int)data, RegistryValueKind.DWord);
                }
            }
        }

        public void SetRegistryKeyValueBinary(string regPath, string keyName, object data)
        {
            if (data != null)
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regPath, true))
                {
                    regKey.SetValue(keyName, (byte)data, RegistryValueKind.Binary);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }
        const int RESOURCETYPE_DISK = 0x00000001;
        const int CONNECT_TEMPORARY = 0x00000004;
        const int ERROR_NO_ADMIN_INFO = 13120;
        const int ERROR_NO_SUCH_LOGON_SESSION = 1312;
        const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;

        private const int RESOURCETYPE_ANY = 0x0;
        private const int CONNECT_INTERACTIVE = 0x00000008;
        private const int CONNECT_PROMPT = 0x00000010;
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;

        // API 함수 선언
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern int WNetAddConnection2(ref NETRESOURCE netResource,
           string password, string username, uint flags);

        // API 함수 선언 (공유해제)
        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2", CharSet = CharSet.Auto)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, int fForce);

        public static int NetWorkDriveConnect(string serverName, string userName, string userPwd)
        {
            NETRESOURCE netResource = new NETRESOURCE();
            netResource.dwType = RESOURCETYPE_DISK;
            netResource.lpRemoteName = serverName;
            netResource.lpProvider = "";

            int returnCode = WNetAddConnection2(ref netResource, userPwd, userName, 0);


            return returnCode;
        }

        public static int NetWorkDriveDisConnect(string serverName)
        {
            int returnCode = WNetCancelConnection2(serverName, CONNECT_UPDATE_PROFILE, 1);

            return returnCode;
        }

    }

    public enum OBJ_TYPE : int
    {
        OBJ_FILE = 0,
        OBJ_DIR,
        OBJ_UNKNOWN
    }

    public class ACL_link
    {
        IList<ACL_data> aclData = new List<ACL_data>();
    }

    public class ACLSubject
    {
        public IList<ACL_Subject> aclSubject = new List<ACL_Subject>();

        public ACL_Subject SearchSubject(string subjectName)
        {
            ACL_Subject aclSub = null;

            foreach(var sub in aclSubject)
            {
                if (sub.subjectName == subjectName)
                {
                    aclSub = sub;
                    break;
                }
            }

            return aclSub;
        }
        public void ACLSubjectAdd(string subjectName, UInt32 permissions)
        {
            ACL_Subject aclSub;

            aclSub = SearchSubject(subjectName);
            if (aclSub != null) return;
            else aclSub = new ACL_Subject();

            aclSub.subjectName = subjectName;
            aclSub.permissions = permissions;

            aclSubject.Add(aclSub);
        }

        public void ACLSubjectRemove(string subjectName)
        {
            Int32 index = -1;

            foreach (var aclSub in aclSubject)
            {
                if (aclSub.subjectName == subjectName)
                {
                    index = aclSubject.IndexOf(aclSub);
                    break;
                }
            }

            if (index != -1) aclSubject.RemoveAt(index);
        }

        public void ACLSubjectList()
        {
            foreach (var aclSub in aclSubject)
            {
                Console.WriteLine("SubjectName={0}, permissions={1}", aclSub.subjectName, aclSub.permissions);
            }
        }
    }

    public class ACLObject : ACLSubject
    {
        public IList<ACL_Object> aclObject = new List<ACL_Object>();

        public ACL_Object SearchObject(string objectName)
        {
            ACL_Object aclObj = null;

            foreach(var obj in aclObject)
            {
                if (obj.objectName == objectName)
                {
                    aclObj = obj;
                    break;
                }
            }

            return aclObj;
        }

        public void ACLObjectAdd(string objectName, UInt32 permissions)
        {
            ACL_Object aclObj;

            aclObj = SearchObject(objectName);
            if (aclObj != null) return;
            else aclObj = new ACL_Object();

            aclObj.objectName = objectName;
            aclObj.permissions = permissions;

            aclObject.Add(aclObj);
        }

        public void ACLObjectRemove(string objectName)
        {
            Int32 index = -1;

            foreach (var aclObj in aclObject)
            {
                if (aclObj.objectName == objectName)
                {
                    index = aclObject.IndexOf(aclObj);
                    break;
                }
            }

            if (index != -1) aclObject.RemoveAt(index);
        }

        public void ACLObjectList()
        {
            foreach (var aclObj in aclObject)
            {
                Console.WriteLine("ObjectName={0}, permissions={1}", aclObj.objectName, aclObj.permissions);
            }
        }
    }

    public class ACLEntries : ACLObject
    {
        public void ACLEntriesAdd(string objectName, string subjectName)
        {
            ACL_Object aclObject = null;
            ACL_Subject aclSubject = null;

            aclObject = SearchObject(objectName);
            if (aclObject == null) return;

            aclSubject = SearchSubject(subjectName);
            if (aclSubject == null) return;

            aclObject.aclSubject.Add(aclSubject);
        }

        public void ACLEntriesRemove(string objectName, string subjectName)
        {
            ACL_Object aclObject = null;
            ACL_Subject aclSubject = null;

            aclObject = SearchObject(objectName);
            if (aclObject == null) return;

            aclSubject = SearchSubject(subjectName);
            if (aclSubject == null) return;

            aclObject.aclSubject.Remove(aclSubject);
        }
    }

    [ServiceContract()]
    public interface IMyCSharpService
    {
        [OperationContract()]
        string testFunc();
    }
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MyCSharpService : IMyCSharpService
    {
        public string testFunc()
        {
            return "testFunc";
        }
    }
}
