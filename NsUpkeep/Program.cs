using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NsUpkeep
{
	class Program
	{
		// DLL imports.
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();
		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		// Private static variables.
		private static string exepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private static string ComputerName = Environment.GetEnvironmentVariable("computername");
		private static string UserName = Environment.GetEnvironmentVariable("username");
		private static string XMLfile = Environment.GetEnvironmentVariable("temp") + @"\NsUpkeepXML.xml";
		private static string logfile = "NsUpkeep.log";
		private static ManagementClass MC;
		private static ManagementObjectCollection MO;
		private static ManagementBaseObject NewDNS;
		private static int SW_HIDE = 0;
		private static Process p;
		//private static int SW_SHOW = 5;

		static void Main(string[] args)
		{
			if (args.Length == 3 && args[0] == "--run") {
				
				// Gjem vinduet.
				IntPtr ConsoleWindow = GetConsoleWindow();
				ShowWindow(ConsoleWindow, SW_HIDE);
				PerformUpdate(args[1] +","+ args[2]);

			} else if (args.Length > 0 && args[0] == "--version") {
	
				Console.WriteLine(
					"This version is "+ 
					Assembly.GetExecutingAssembly().GetName().Version.Major.ToString() +"."+
					Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString() +"."+
					Assembly.GetExecutingAssembly().GetName().Version.Build.ToString()
				);
				return;

			} else if (args.Length > 0 && args[0] == "--help") {
	
				Console.WriteLine(Environment.NewLine +
					"  _______          ____ ___         __                         "+ Environment.NewLine+
					"  \\      \\   _____|    |   \\______ |  | __ ____   ____ ______  "+ Environment.NewLine+
					"  /   |   \\ /  ___/    |   /\\____ \\|  |/ // __ \\_/ __ \\\\____ \\ "+ Environment.NewLine+
					" /    |    \\\\___ \\|    |  / |  |_> >    <\\  ___/\\  ___/|  |_> >"+ Environment.NewLine+
					" \\____|__  /____  >______/  |   __/|__|_ \\\\___  >\\___  >   __/ "+ Environment.NewLine+
					"         \\/     \\/          |__|        \\/    \\/     \\/|__|    "+ Environment.NewLine + 
					" https://thronic.com/Software/NsUpkeep/"+ Environment.NewLine +
					" (C)2017-2018 Dag J Nedrelid <dj@thronic.com>"+ Environment.NewLine + 
					" Specified DNS server upkeep assistant. " + Environment.NewLine + Environment.NewLine +
					" Options:" + Environment.NewLine +
					"  --version" + Environment.NewLine + 
					"  --start-on-boot 8.8.8.8 8.8.4.4" +Environment.NewLine +
					"  --remove-from-boot" + Environment.NewLine + Environment.NewLine +
					"  NOTE: --start-on-boot will use the current location, "+ Environment.NewLine +
					"        so put this file in appdata or something first. " + Environment.NewLine +
					"        Do NOT use hostnames -- USE IP ADDRESSES!" + Environment.NewLine
				);
				return;

			} else if (args.Length == 3 && args[0] == "--start-on-boot") {
	
				try {
				p = new Process();
				p.StartInfo.UseShellExecute = true;
				p.StartInfo.FileName = "ICACLS";
				p.StartInfo.Arguments = "\"c:\\Windows\\Tasks\" /GRANT \"Administratorer\":(F) >nul 2>&1";
				p.StartInfo.RedirectStandardOutput = false;
				p.StartInfo.RedirectStandardError = false;
				p.Start();

				p = new Process();
				p.StartInfo.UseShellExecute = true;
				p.StartInfo.FileName = "ICACLS";
				p.StartInfo.Arguments = "\"c:\\Windows\\System32\\Tasks\" /GRANT \"Administratorer\":(F) >nul 2>&1";
				p.StartInfo.RedirectStandardOutput = false;
				p.StartInfo.RedirectStandardError = false;
				p.Start();

				p = new Process();
				p.StartInfo.UseShellExecute = true;
				p.StartInfo.FileName = "SCHTASKS";
				//p.StartInfo.Arguments = "/CREATE /RU \""+ ComputerName +"\\"+ UserName +"\" /TN NsUpkeep /IT /F /XML "+ XMLfile;
				p.StartInfo.Arguments = "/CREATE /TN NsUpkeep /F /XML "+ XMLfile;
				p.StartInfo.RedirectStandardOutput = false;
				p.StartInfo.RedirectStandardError = false;
				File.WriteAllText(XMLfile, CreateTaskXML(exepath + @"\NsUpkeep.exe", args[1], args[2]), UnicodeEncoding.Unicode);
				p.Start();

				Console.WriteLine("NsUpkeep should now update every 5 minute after next reboot.");

				} catch (Exception ee) {
					Console.WriteLine("Startup option error: "+ ee.Message);
				}

			} else if (args.Length > 0 && args[0] == "--remove-from-boot") {
				
				try {
				// Remove any startup task.
				p = new Process();
				p.StartInfo.UseShellExecute = true;
				p.StartInfo.FileName = "SCHTASKS";
				p.StartInfo.Arguments = "/DELETE /TN NsUpkeep /F";
				p.StartInfo.RedirectStandardOutput = false;
				p.StartInfo.RedirectStandardError = false;
				p.Start();

				Console.WriteLine("NsUpkeep should now be removed from startup.");

				} catch (Exception ee) {
					Console.WriteLine("Startup option error: "+ ee.Message);
				}
				
			} else {
				Console.WriteLine("Nothing to do... Use --help for tips.");
			}
		}

		private static void PerformUpdate(string NSINFO)
		{
			try {
			MC = new ManagementClass("Win32_NetworkAdapterConfiguration");
			MO = MC.GetInstances();

			foreach (ManagementObject o in MO) {
				
				// Only updates the NIC where the TCP/IP is bound and enabled.
				if ((bool)o["IPEnabled"]) {

					// Update DNS settings.
					NewDNS = o.GetMethodParameters("SetDNSServerSearchOrder");
					NewDNS["DNSServerSearchOrder"] = NSINFO.Split(',');
					o.InvokeMethod("SetDNSServerSearchOrder", NewDNS, null);
				}

			}} catch (Exception e) {
				File.AppendAllText(exepath +@"\"+ logfile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +":\t"+ e.Message + Environment.NewLine);

				// Rotate logfile if it's getting big.
				double _fsz = new FileInfo(logfile).Length;
				double _fszKiB = Math.Round(_fsz/1024,1);
				if (_fszKiB >= 1047552) { // 1 MB as margin.
					File.Copy(logfile, logfile +".old", true);
					File.WriteAllText(logfile, "");
				}
			}
		}

		private static string CreateTaskXML(string ProgramFilePath, string NS1, string NS2)
		{
			return ""+
			"<?xml version=\"1.0\" encoding=\"UTF-16\"?>"+
			"<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">"+
			"  <Triggers>"+
			"    <LogonTrigger>"+
			"      <Repetition>"+
			"        <Interval>PT5M</Interval>"+
			"        <StopAtDurationEnd>false</StopAtDurationEnd>"+
			"      </Repetition>"+
			"      <Enabled>true</Enabled>"+
			"    </LogonTrigger>"+
			"  </Triggers>"+
			"  <Principals>"+
			"    <Principal id=\"Author\">"+
			"      <UserId>S-1-5-18</UserId>"+
			"      <RunLevel>HighestAvailable</RunLevel>"+
			"    </Principal>"+
			"  </Principals>"+
			"  <Settings>"+
			"    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>"+
			"    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>"+
			"    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>"+
			"    <AllowHardTerminate>false</AllowHardTerminate>"+
			"    <StartWhenAvailable>false</StartWhenAvailable>"+
			"    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>"+
			"    <IdleSettings>"+
			"      <StopOnIdleEnd>true</StopOnIdleEnd>"+
			"      <RestartOnIdle>false</RestartOnIdle>"+
			"    </IdleSettings>"+
			"    <AllowStartOnDemand>true</AllowStartOnDemand>"+
			"    <Enabled>true</Enabled>"+
			"    <Hidden>true</Hidden>"+
			"    <RunOnlyIfIdle>false</RunOnlyIfIdle>"+
			"    <WakeToRun>false</WakeToRun>"+
			"    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>"+
			"    <Priority>7</Priority>"+
			"  </Settings>"+
			"  <Actions Context=\"Author\">"+
			"    <Exec>"+
			"      <Command>\""+ ProgramFilePath +"\"</Command>"+
			"      <Arguments>--run "+ NS1 +" "+ NS2 +"</Arguments>"+
			"    </Exec>"+
			"  </Actions>"+
			"</Task>";
		}
	}
}
