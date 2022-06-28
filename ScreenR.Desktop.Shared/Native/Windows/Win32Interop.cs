using PInvoke;
using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Native.Windows
{
    public static class Win32Interop
    {

        private static User32.SafeDesktopHandle? _lastInputDesktop;

        public static bool TryGetCurrentDesktop(out string desktopName)
        {
            using var safeHandle = GetInputDesktop();
            var desktopPtr = safeHandle.DangerousGetHandle();
            var pvInfoPtr = Marshal.AllocHGlobal(256);
            var lenPtr = Marshal.AllocHGlobal(256);

            try
            {

                if (!User32.GetUserObjectInformation(desktopPtr, User32.ObjectInformationType.UOI_NAME, pvInfoPtr, 256, lenPtr))
                {
                    desktopName = string.Empty;
                    return false;

                }

                desktopName = Marshal.PtrToStringAuto(pvInfoPtr) ?? string.Empty;
                return true;
            }
            finally
            {
                User32.CloseDesktop(desktopPtr);
                Marshal.FreeHGlobal(pvInfoPtr);
                Marshal.FreeHGlobal(lenPtr);
            }
        }
        public static User32.SafeDesktopHandle GetInputDesktop()
        {
            return User32.OpenInputDesktop(User32.DesktopCreationFlags.None, true, Kernel32.ACCESS_MASK.GenericRight.GENERIC_ALL);
        }

        public static bool LaunchProcessInSession(
            string commandLine,
            int targetSessionId,
            bool forceConsoleSession,
            string desktopName,
            bool hiddenWindow,
            out Kernel32.PROCESS_INFORMATION procInfo)
        {
            unsafe
            {
                int winlogonPid = 0;
                var hProcess = IntPtr.Zero;

                procInfo = new Kernel32.PROCESS_INFORMATION();

                var dwSessionId = Kernel32.WTSGetActiveConsoleSessionId();

                if (!forceConsoleSession)
                {
                    var activeSessions = GetActiveSessions();
                    if (activeSessions.Any(x => x.ID == targetSessionId))
                    {
                        dwSessionId = (uint)targetSessionId;
                    }
                    else
                    {
                        dwSessionId = (uint)activeSessions.Last().ID;
                    }
                }

                var processes = Process.GetProcessesByName("winlogon");
                foreach (Process p in processes)
                {
                    if ((uint)p.SessionId == dwSessionId)
                    {
                        winlogonPid = p.Id;
                    }
                }

                var safeHandle = Kernel32.OpenProcess(0x02000000, false, winlogonPid);
                hProcess = safeHandle.DangerousGetHandle();


                if (!AdvApi32.OpenProcessToken(hProcess, 0x0002, out var hPToken))
                {
                    Kernel32.CloseHandle(hProcess);
                    return false;
                }


                // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser.
                var sa = Kernel32.SECURITY_ATTRIBUTES.Create();
                sa.nLength = Marshal.SizeOf(sa);
                var saPtr = Marshal.AllocHGlobal(sa.nLength);
                Marshal.StructureToPtr(sa, saPtr, false);

                // Copy the access token of the winlogon process; the newly created token will be a primary token.
                if (!AdvApi32.DuplicateTokenEx(hPToken, 0x02000000, saPtr, Kernel32.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, AdvApi32.TOKEN_TYPE.TokenPrimary, out var duplicatedToken))
                {
                    Kernel32.CloseHandle(hProcess);
                    return false;
                }

                // By default, CreateProcessAsUser creates a process on a non-interactive window station, meaning
                // the window station has a desktop that is invisible and the process is incapable of receiving
                // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
                // interaction with the new process.

                var desktopPtr = Marshal.StringToHGlobalAnsi($@"winsta0\{desktopName}");
                var si = Kernel32.STARTUPINFO.Create();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop_IntPtr = desktopPtr;


                // Flags that specify the priority and creation method of the process.
                Kernel32.CreateProcessFlags dwCreationFlags;
                if (hiddenWindow)
                {
                    dwCreationFlags =
                        Kernel32.CreateProcessFlags.NORMAL_PRIORITY_CLASS |
                        Kernel32.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT |
                        Kernel32.CreateProcessFlags.CREATE_NO_WINDOW;

                    si.dwFlags = Kernel32.StartupInfoFlags.STARTF_USESHOWWINDOW;
                    si.wShowWindow = 0;
                }
                else
                {
                    dwCreationFlags =
                        Kernel32.CreateProcessFlags.NORMAL_PRIORITY_CLASS |
                        Kernel32.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT |
                        Kernel32.CreateProcessFlags.CREATE_NEW_CONSOLE;
                }

                // Create a new process in the current user's logon session.
                bool result = Kernel32.CreateProcessAsUser(
                    duplicatedToken.DangerousGetHandle(),
                    null,
                    commandLine,
                    saPtr,
                    saPtr,
                    false,
                    dwCreationFlags,
                    IntPtr.Zero,
                    null,
                    ref si,
                    out procInfo);


                // Invalidate the handles.
                Kernel32.CloseHandle(hProcess);
                Kernel32.CloseHandle(desktopPtr);
                Kernel32.CloseHandle(saPtr);
                duplicatedToken.Dispose();
                hPToken.Dispose();

                return result;
            }

        }

        public static bool SwitchToInputDesktop()
        {
            try
            {
                _lastInputDesktop?.Dispose();

                var inputDesktop = User32.OpenInputDesktop(0, true, Kernel32.ACCESS_MASK.GenericRight.GENERIC_ALL);

                if (inputDesktop.IsInvalid)
                {
                    return false;
                }

                var result = User32.SetThreadDesktop(inputDesktop) && User32.SwitchDesktop(inputDesktop);
                _lastInputDesktop = inputDesktop;
                return result;
            }
            catch
            {
                return false;
            }
        }

        public static List<WindowsSession> GetActiveSessions()
        {
            unsafe
            {
                var sessions = new List<WindowsSession>();
                var consoleSessionId = Kernel32.WTSGetActiveConsoleSessionId();
                sessions.Add(new WindowsSession()
                {
                    ID = (int)consoleSessionId,
                    Type = SessionType.Console,
                    Name = "Console",
                    Username = GetUsernameFromSessionId((uint)consoleSessionId)
                });


                var enumSessionResult = WtsApi32.WTSEnumerateSessions(WtsApi32.WTS_CURRENT_SERVER_HANDLE, 0, 1, out IntPtr ppSessionInfo, out var count);
                var dataSize = Marshal.SizeOf(typeof(WtsApi32.WTS_SESSION_INFO));
                var current = ppSessionInfo;

                if (enumSessionResult)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var sessionInfo = Marshal.PtrToStructure<WtsApi32.WTS_SESSION_INFO>(current);
                        current += dataSize;
                        if (sessionInfo.State == WtsApi32.WTS_CONNECTSTATE_CLASS.WTSActive && sessionInfo.SessionID != consoleSessionId)
                        {

                            sessions.Add(new WindowsSession()
                            {
                                ID = sessionInfo.SessionID,
                                Name = sessionInfo.WinStationName,
                                Type = SessionType.RDP,
                                Username = GetUsernameFromSessionId((uint)sessionInfo.SessionID)
                            });
                        }
                    }
                }

                return sessions;
            }
        }
        public static string GetUsernameFromSessionId(uint sessionId)
        {
            var username = string.Empty;
            
            if (WtsApi32Ex.WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsApi32Ex.WTS_INFO_CLASS.WTSUserName, out var buffer, out var strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
                WtsApi32Ex.WTSFreeMemory(buffer);
            }

            return username ?? string.Empty;
        }
    }
}
