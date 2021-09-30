﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Smiter.Classes;

namespace Smiter
{
    public partial class MainForm : Form
    {
        public List<LogEvent> Log = new List<LogEvent>();
        private void InsertToLog(string Event, int EventType, int EventClass)
        {
            Log.Add(new LogEvent(EventType, Event, EventClass));
        }
        #region Global Variables
        //Object to get CPU Usage
        private PerformanceCounter cpuMetric = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        //Timer object to constantly refresh the process list and CPU Usage
        private Timer timer = new Timer();
        //Boolean used to pause or resume the timer
        private bool TimerStatus = true;
        //Multiplier for selecting between seconds or milliseconds for the timer
        private int multiplier = 1;
        //Lists for Log events, classes of events and types of events


        //Objects for calling taskkill
        private System.Diagnostics.Process CMDProcess = new System.Diagnostics.Process();
        private System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        #endregion
        public MainForm()
        {
            InitializeComponent();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            InsertToLog("Program Started, Window Loaded.", 0, 0);
            //Placeholder for the CPU Usage Bar
            CPU_Bar.Value = 50;
            //Get current CPU Usage
            float CPU_Value = cpuMetric.NextValue();
            //Setting default timer Interval and putting number updown on the default position
            timer.Interval = 500;
            RR.Value = 500;
            InsertToLog("Timer Interval Set.", 0, 0);
            //Assigning a method for each timer tick
            timer.Tick += new EventHandler(refreshMetrics);
            //Starting the timer
            timer.Start();
            InsertToLog("Timer Setup and Started.", 0, 0);
            //Hiding the command prompt execution and setting the executable for the command prompt
            string Executable = "cmd.exe";
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = Executable;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            InsertToLog(string.Format("Commands Setup for executable {0}.", Executable), 0, 0);
            InsertToLog("Start-up Finished.", 1, 0);

        }


        //Method to get the Process List. Using a custom class so that it's easier to set-up the datagridview (MainForm.ProcessList)
        private List<ProcessSum> ProcessNames
        {
            get
            {
                Process[] processCollection = Process.GetProcesses();
                List<ProcessSum> ProcessList = new List<ProcessSum>();
                foreach (Process process in processCollection)
                {
                    ProcessList.Add(new ProcessSum(process.Id, process.ProcessName, string.Empty));
                }
                return ProcessList;
            }

        }

        private void Exit()
        {
            NotificationAreaIcon.Visible = false;
            InsertToLog("Notification Area Icon disabled.", 1, 2);
            System.Windows.Forms.Application.Exit();
            InsertToLog("If you're seeing this the program did not close successfully.", 3, 2);
        }

        //The method that closes the program. It first disables the notification icon, so that it doesn't remain zombified and then exits.
        private void CloseBtn_Click(object sender, EventArgs e)
        {
            this.Exit();
        }

        private void refreshMetrics(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(Button))
            {
                Button RefreshButton = (Button)sender;
                if (RefreshButton.Name == "RefreshBtn")
                {
                    InsertToLog("Manual Refresh Issued.", 0, 1);
                }
            }
            ProcessList.DataSource = ProcessNames;
            float CPU_Value = cpuMetric.NextValue();

            //From 0% to 50% the bar will be yellow, from 50% to 75% will be yellow, from 75% to 100% will be red 
            if (CPU_Value < 50)
            {
                CPU_Bar.ForeColor = Color.Green;
            }
            else
            {
                if (CPU_Value >= 50 && CPU_Value < 75)
                {
                    CPU_Bar.ForeColor = Color.Yellow;
                }
                else
                {
                    if (CPU_Value >= 75)
                    {
                        CPU_Bar.ForeColor = Color.Red;
                    }
                }
            }

            //Assigning the Bar Style and percentage filled
            CPU_Bar.Style = ProgressBarStyle.Continuous;
            CPU_Bar.Value = (int)CPU_Value;

        }

        private void PauseBtn_Click(object sender, EventArgs e)
        {
            //Changes the timer status and the button text to match the button action
            if (TimerStatus)
            {
                timer.Stop();
                PauseBtn.Text = "4";
                InsertToLog("Timer Paused.", 1, 1);
            }
            else
            {
                timer.Start();
                PauseBtn.Text = ";";
                InsertToLog("Timer Resumed.", 1, 1);
            }
            TimerStatus = !TimerStatus;
        }

        #region Functions to change the Refresh Rate of the program, matching what's on the updown object
        private void ChangeRefreshRate(object sender, EventArgs e)
        {
            timer.Interval = (int)RR.Value * multiplier;
        }

        private void ChangeRefreshRate(object sender, ScrollEventArgs e)
        {
            timer.Interval = (int)RR.Value * multiplier;
        }
        #endregion

        //Create a About Form instance and open it
        private void AboutBtn_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.Show();
        }


        private void KillTask(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //TODO: Add message on bottom saying what process was killed, and maybe a log??
                List<string> Output = new List<string>();
                System.Windows.Forms.DataGridViewRow row = ProcessList.Rows[e.RowIndex];
                //if user double clicked a PID
                if (e.ColumnIndex == 0)
                {
                    bool PIDCaught = false;
                    InsertToLog(string.Format("Process termination issued on process with ID {0}{1}.", row.Cells[0].Value, ForceClose.Checked ? " with Force flag enabled" : ""), 0, 3);
                    startInfo.Arguments = string.Format("/C taskkill.exe /PID {0} {1}", row.Cells[0].Value, ForceClose.Checked ? "/F" : "");
                    CMDProcess.StartInfo = startInfo;
                    CMDProcess.Start();
                    while (!CMDProcess.StandardOutput.EndOfStream)
                    {
                        PIDCaught = CMDProcess.StandardOutput.ReadLine().Contains("SUCCESS");
                    }
                    if (PIDCaught)
                        InsertToLog(string.Format("Process with ID {0} was {1}terminated successfully.", row.Cells[0].Value, ForceClose.Checked ? "forcefully " : ""), 1, 3);
                    else
                        throw new Exception(string.Format("No processes were found matching ID {0}", row.Cells[0].Value));
                }
                //if user double clicked a Process Name
                if (e.ColumnIndex == 1)
                {
                    //TODO: Add confirmation message
                    int instanceCount = 0;
                    InsertToLog(string.Format("Process termination issued on all processes with name {0}{1}.", row.Cells[1].Value, ForceClose.Checked ? " with Force flag enabled" : ""), 0, 3);
                    startInfo.Arguments = string.Format("/C taskkill.exe /IM {0}.exe {1}", row.Cells[1].Value, ForceClose.Checked ? "/F" : "");
                    CMDProcess.StartInfo = startInfo;
                    CMDProcess.Start();
                    while (!CMDProcess.StandardOutput.EndOfStream) if (CMDProcess.StandardOutput.ReadLine().Contains("SUCCESS")) instanceCount++;
                    if (instanceCount > 0)
                        InsertToLog(string.Format("{0} instances of process with the name {1} were {2}terminated successfully.", instanceCount, row.Cells[1].Value, ForceClose.Checked ? "forcefully " : ""), 1, 3);
                    else
                        throw new Exception(string.Format("No processes were found matching the name {0}", row.Cells[1].Value));
                }
            }
            catch (Exception ex)
            {
                InsertToLog(string.Format("Exception thrown with error: {0}", ex.Message), 3, 3);
            }

            //If enabled, will exit the program after killing whatever process was selected
            if (HnRCheckBox.Checked) this.Exit();
        }

        #region RRType Radio Buttons
        //Method is called when user clicks the Milliseconds radio button
        private void ChangeRR_to_Milliseconds(object sender, EventArgs e)
        {
            multiplier = 1;
            RR.Minimum = 100;
            RR.Increment = 100;
            timer.Interval = (int)RR.Value * multiplier;
        }

        //Method is called when user clicks the Seconds radio button
        private void ChangeRR_to_Seconds(object sender, EventArgs e)
        {
            multiplier = 1000;
            RR.Minimum = 1;
            RR.Increment = 1;
            timer.Interval = (int)RR.Value * multiplier;
        }
        #endregion

        private void CloseBtnHoverIn(object sender, EventArgs e)
        {
            CloseBtn.BackColor = Color.DarkGray;
            CloseBtn.ForeColor = Color.Red;
        }
        private void CloseBtnHoverOut(object sender, EventArgs e)
        {
            CloseBtn.BackColor = Color.Red;
            CloseBtn.ForeColor = Color.Black;
        }

        private void LogBtn_Click(object sender, EventArgs e)
        {
            LogForm logForm = new LogForm(this, Log);
            logForm.Show();
        }

        private void MinimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
