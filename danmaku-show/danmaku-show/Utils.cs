using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace danmaku_show
{
    internal static class Utils
    {
        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// See https://stackoverflow.com/questions/5901679/kill-process-tree-programmatically-in-c-sharp
        /// </summary>
        /// <param name="pid">The pid.</param>
        public static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            var searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            catch (Exception)
            {
                // Other Exception.
            }
        }

        /// <summary>
        /// Checks the TCP port whether it is availabled to use.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public static bool CheckTCPPort(int port)
        {
            // Check active port
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnectionInfos = ipGlobalProperties.GetActiveTcpConnections();
            var result = from e in tcpConnectionInfos where e.LocalEndPoint.Port == port select e;
            if(result.Count() > 0)
            {
                return false;
            }

            var tcpListeningInfos = ipGlobalProperties.GetActiveTcpListeners();
            var result2 = from e in tcpListeningInfos where e.Port == port select e;
            if(result.Count() > 0)
            {
                return false;
            }

            // Otherwise
            return true;
        }
    }
}
