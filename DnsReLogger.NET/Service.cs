using System;
using System.Runtime.InteropServices;


namespace DnsReLogger.NET
{
    static class DnsReloggerService
    {

        #region ServiceParameters
        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        const string DisplayName = "DnsReLogger.NET";
        const string ServiceName = "dnsrelogger";
        const Int32 SERVICE_AUTO_START = 0x2;
        const Int32 SERVICE_ERROR_NORMAL = 0x1;
        #endregion

        #region WIN32_ERRORS
        const Int32 ERROR_SERVICE_ALREADY_RUNNING = 0x420;
        const Int32 ERROR_SERVICE_DOES_NOT_EXIST = 0x424;
        const Int32 ERROR_SERVICE_EXISTS = 0x431;
        const Int32 ERROR_DUPLICATE_SERVICE_NAME = 0x436;
        const Int32 ERROR_SERVICE_NOT_FOUND = 0x4DB;
        #endregion

        public static bool IsRunning { get; private set; }

        public enum ServiceState
        {
            Unknown = -1, // The state cannot be (has not been) retrieved.
            NotFound = 0, // The service is not known on the host server.
            Stopped = 1,
            StartPending = 2,
            StopPending = 3,
            Running = 4,
            ContinuePending = 5,
            PausePending = 6,
            Paused = 7
        }

        [Flags]
        public enum ScmAccessRights
        {
            Connect = 0x0001,
            CreateService = 0x0002,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            QueryLockStatus = 0x0010,
            ModifyBootConfig = 0x0020,
            StandardRightsRequired = 0xF0000,
            AllAccess = (StandardRightsRequired | Connect | CreateService |
                         EnumerateService | Lock | QueryLockStatus | ModifyBootConfig)
        }

        [Flags]
        public enum ServiceAccessRights
        {
            QueryConfig = 0x1,
            ChangeConfig = 0x2,
            QueryStatus = 0x4,
            EnumerateDependants = 0x8,
            Start = 0x10,
            Stop = 0x20,
            PauseContinue = 0x40,
            Interrogate = 0x80,
            UserDefinedControl = 0x100,
            Delete = 0x00010000,
            StandardRightsRequired = 0xF0000,
            AllAccess = (StandardRightsRequired | QueryConfig | ChangeConfig |
                         QueryStatus | EnumerateDependants | Start | Stop | PauseContinue |
                         Interrogate | UserDefinedControl)
        }

        public enum ServiceControl
        {
            Stop = 0x00000001,
            Pause = 0x00000002,
            Continue = 0x00000003,
            Interrogate = 0x00000004,
            Shutdown = 0x00000005,
            ParamChange = 0x00000006,
            NetBindAdd = 0x00000007,
            NetBindRemove = 0x00000008,
            NetBindEnable = 0x00000009,
            NetBindDisable = 0x0000000A
        }



        [StructLayout(LayoutKind.Sequential)]
        private class SERVICE_STATUS
        {
            public int dwServiceType = 0;
            public ServiceState dwCurrentState = 0;
            public int dwControlsAccepted = 0;
            public int dwWin32ExitCode = 0;
            public int dwServiceSpecificExitCode = 0;
            public int dwCheckPoint = 0;
            public int dwWaitHint = 0;
        }

        #region OpenSCManager
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr OpenSCManager(string machineName, string databaseName, ScmAccessRights dwDesiredAccess);
        #endregion

        #region OpenService
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceAccessRights dwDesiredAccess);
        #endregion

        #region CreateService
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, ServiceAccessRights dwDesiredAccess, int dwServiceType, Int32 dwStartType, Int32 dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lp, string lpPassword);
        #endregion

        #region CloseServiceHandle
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseServiceHandle(IntPtr hSCObject);
        #endregion

        #region QueryServiceStatus
        [DllImport("advapi32.dll")]
        private static extern int QueryServiceStatus(IntPtr hService, SERVICE_STATUS lpServiceStatus);
        #endregion

        #region DeleteService
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteService(IntPtr hService);
        #endregion

        #region ControlService
        [DllImport("advapi32.dll")]
        private static extern int ControlService(IntPtr hService, ServiceControl dwControl, SERVICE_STATUS lpServiceStatus);
        #endregion

        #region StartService
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);
        #endregion


        public static bool Install()
        {
            int lastError = 0;
            IntPtr hSCManager = OpenSCManager(null, null, ScmAccessRights.AllAccess);
            
            if (hSCManager == IntPtr.Zero)
                throw new ApplicationException($"OpenSCManager failed with error {Marshal.GetLastWin32Error()}");

            IntPtr hService = CreateService(hSCManager, ServiceName, DisplayName, 
                ServiceAccessRights.AllAccess, 
                SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, 
                System.Reflection.Assembly.GetExecutingAssembly().Location, 
                null, IntPtr.Zero, null, null, null);
            
            if (hService == IntPtr.Zero)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError != ERROR_SERVICE_EXISTS && lastError != ERROR_DUPLICATE_SERVICE_NAME)
                    throw new ApplicationException($"CreateService failed with error {lastError}");
                return true;
            }

            if(!StartService(hService, 0, null))
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError != ERROR_SERVICE_ALREADY_RUNNING)
                    throw new ApplicationException($"StartService failed with error {lastError}");
                IsRunning = true;
            }

            CloseServiceHandle(hService);
            CloseServiceHandle(hSCManager);

            return false;

        }
        public static bool Uninstall()
        {
            IntPtr hSCManager = OpenSCManager(null, null, ScmAccessRights.AllAccess);
            int lastError = 0;
            if (hSCManager == IntPtr.Zero)
                throw new ApplicationException($"OpenSCManager failed with error {Marshal.GetLastWin32Error()}");

            IntPtr hService = OpenService(hSCManager, ServiceName, ServiceAccessRights.AllAccess);
            if(hService == IntPtr.Zero)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_SERVICE_DOES_NOT_EXIST)
                    return false;
                else
                    throw new ApplicationException($"OpenService failed with error {lastError}");
            }

            if (!DeleteService(hService))
                throw new ApplicationException($"DeleteService failed with error {Marshal.GetLastWin32Error()}");

            CloseServiceHandle(hService);
            CloseServiceHandle(hSCManager);
            IsRunning = false;
            return true;

        }

        
    }
}
