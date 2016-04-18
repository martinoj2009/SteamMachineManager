using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Steam_Machine_Manager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        /*
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        */

        private void Form1_Load(object sender, EventArgs e)
        {
            
            //kick off steam before doing any work
            if (isSteamRunning() == false)
            {
                //Kill the process so that it can be restarted
                killSteam();
                startSteam();
            }


            //Get application's path
            string applicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int idx = applicationPath.LastIndexOf('\\');
            applicationPath = applicationPath.Substring(0, idx);

            //ErrorLog(GetActiveWindowTitle());

            //Make update directory if it doesn't exist
            if (Directory.Exists(applicationPath + "\\update") == false)
            {
                try
                {
                    Directory.CreateDirectory(applicationPath + "\\update");
                }
                catch(Exception ex)
                {
                    ErrorLog(ex.ToString());
                }
                
            }

            //Update video driver
            if(File.Exists(applicationPath + "\\update\\video.exe") && File.Exists(applicationPath + "\\update\\dvideo.txt") == false)
            {
                try
                {
                    updateVideo();
                }
                catch(Exception ex)
                {
                    ErrorLog(ex.ToString());
                }
            }

            //Delete driver update if it exists
            if(File.Exists(applicationPath + "\\update\\dvideo.txt"))
            {
                //Delete the driver and file
                try
                {
                    File.Delete(applicationPath + "\\update\\video.exe");
                    File.Delete(applicationPath + "\\update\\dvideo.txt");
                }
                catch(Exception ex)
                {
                    ErrorLog(ex.ToString());
                }
            }


            //start time service
            startTimeService();
            Thread.Sleep(1000);
            syncTime();


            while (true)
            {
                
                //Check to see if steam is running
                if (isSteamRunning() == false)
                {
                    //Kill the process so that it can be restarted
                    killSteam();
                    startSteam();
                }
                
                
                if(checkForUpdate() == true)
                {
                    updateProgram();
                }

                //Force cleanup
                GC.Collect();
                Thread.Sleep(9000);

            }
            




        }



        private bool isSteamRunning()
        {
            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    if(process.MainWindowTitle == "Steam")
                    {
                        return true;
                    }
                }
            }

            processlist = null;
            //Return false because Steam wasn't found
            return false;
        }


        private void startSteam()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(getSteamLocation());
                //startInfo.WindowStyle = ProcessWindowStyle;
                startInfo.ErrorDialog = false;
                startInfo.Arguments = "-bigpicture";
                Process.Start(startInfo);
            }
            catch(Exception ex)
            {
                ErrorLog(ex.ToString());
            }
            
        }


        private string getSteamLocation()
        {
            RegistryKey regKey;
            try
            {
                regKey = Registry.CurrentUser;
                regKey = regKey.OpenSubKey(@"Software\Valve\Steam");
            }
            catch(Exception ex)
            {
                //Close program if cannot find Steam and write to log
                ErrorLog("Cannot find Steam location in registry!");
                ErrorLog("Cannot find HKEY_LOCAL_MACHINE\\Software\\Valve\\Steam");
                ErrorLog("Will now close program!");
                ErrorLog(ex.ToString());
                Application.Exit();
                return "";
            }

            if (regKey != null)
            {
                string installpath = "";
                try
                {
                    installpath = regKey.GetValue("SteamExe").ToString();
                }
                catch(Exception ex)
                {
                    ErrorLog("Cannot find SteamExe in registry!");
                    ErrorLog(ex.Message.ToString());
                    Application.Exit();
                }
                
                return installpath;
            }

            //Return nothing if it can't be found
            return "";
        }

        private void killSteam()
        {
            try
            {
                Process[] proc = Process.GetProcessesByName("Steam");
                proc[0].Kill();
            }
            catch(Exception)
            {

            }
        }

        private void Form_Shown(object sender, EventArgs e)
        {
            Visible = false;
            Opacity = 100;
        }

        private bool checkForUpdate()
        {
            string applicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int idx = applicationPath.LastIndexOf('\\');
            applicationPath = applicationPath.Substring(0, idx);


            ErrorLog("Checked for update");
            //MessageBox.Show(applicationPath);
            if (File.Exists(applicationPath + "\\update\\update.exe") == true)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(applicationPath + "\\update\\update.exe");
                string version = versionInfo.ProductVersion;

                //Now compare versions
                var version1 = new Version(version);
                var version2 = new Version(Application.ProductVersion);

                var result = version1.CompareTo(version2);

                if (result == 1)
                {
                    //Cleanup
                    versionInfo = null;
                    version = null;
                    version1 = null;
                    version2 = null;

                    //Return true because there's an update
                    return true;
                }
            }
            else
            {

            }


            return false;

        }

        private void updateProgram()
        {
            string applicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int idx = applicationPath.LastIndexOf('\\');
            applicationPath = applicationPath.Substring(0, idx);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(applicationPath + "\\updateSteamMachineManager.exe");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(startInfo);
            }
            catch(Exception ex)
            {
                ErrorLog("Cannot launch updater!");
                ErrorLog(ex.Message.ToString());
            }
            

            //Close the program
            Application.Exit();
        }

        public void ErrorLog(string sErrMsg)
        {
            try
            {
                string sPathName = "error.txt";
                string sErrorTime = DateTime.Now.ToString();
                StreamWriter sw = new StreamWriter(sPathName, true);
                sw.WriteLine(sErrorTime + " --  " + sErrMsg);
                sw.Flush();
                sw.Close();
            }
            catch(Exception)
            {

            }
            
        }

        /*
        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        */


        private void startTimeService()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("net");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "start \"Windows Time\"";
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ErrorLog("Cannot launch updater!");
                ErrorLog(ex.Message.ToString());
            }
        }

        private void syncTime()
        {
            try
            {
                //First register
                ProcessStartInfo startInfo = new ProcessStartInfo("w32tm");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "/register";
                var p = Process.Start(startInfo);
                p.WaitForExit();

                //Now update
                startInfo = new ProcessStartInfo("w32tm");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "/config /update /manualpeerlist:\"pool.ntp.org\"";
                p = Process.Start(startInfo);
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                ErrorLog("Cannot update time!");
                ErrorLog(ex.Message.ToString());
            }
        }

        private void updateVideo()
        {
            string applicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            int idx = applicationPath.LastIndexOf('\\');
            applicationPath = applicationPath.Substring(0, idx);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(applicationPath + "\\update\\video.exe");
                //startInfo.WindowStyle = ProcessWindowStyle;
                startInfo.ErrorDialog = false;
                startInfo.Arguments = "/s";
                Process.Start(startInfo);
                File.Create(applicationPath + "\\update\\dvideo.txt");
            }
            catch (Exception ex)
            {
                ErrorLog(ex.ToString());
            }

            applicationPath = null;
        }
    }
}
