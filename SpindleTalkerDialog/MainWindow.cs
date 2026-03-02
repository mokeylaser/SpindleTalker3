// Icons from http://www.veryicon.com/icons/system/led/
// MODBUS CRC code from http://www.codeproject.com/Articles/19214/CRC-Calculation
// Analogue meter from http://www.codeproject.com/Articles/24945/Analog-Meter
//

using System;
using System.Drawing;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SpindleTalker2.Properties;
using System.IO;
using System.Diagnostics;
using VfdControl;

namespace SpindleTalker2
{
    public partial class MainWindow : Form
    {

        #region Initialisation

        private int howLongToWait = 5000;
        private MeterControl _meterControl;
        public MotorControl _hyMotorControl;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void HYmodbus_OnWriteLog(string message, bool error = false)
        {
            Debug.Print(message);
        }

        private Stopwatch stopWatchInitialPoll = new Stopwatch();

        private void SpindleTalker_Load(object sender, EventArgs e)
        {
            ChangeGtrackbarColours(false);

            if (SerialPort.GetPortNames().Length == 0)
            {
                this.Show();
                MessageBox.Show(this, "There are no COM Ports detected on this computer.\nPlease install a COM Port and restart this app.", "No COM Ports Installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                COMPortStatus(false);
                return;
            }

            _hyMotorControl = new MotorControl(
                portName: VFDsettings.PortName,
                baudRate: VFDsettings.BaudRate,
                dataBits: VFDsettings.DataBits,
                parity: (int)VFDsettings.Parity,
                stopBits: (int)VFDsettings.StopBits,
                modBusID: VFDsettings.VFD_ModBusID);
            _hyMotorControl._hyModbus.VFDData.OnSerialPortConnected += COMPortStatus;
            _hyMotorControl._hyModbus.VFDData.OnChanged += VFDdata_OnChanged;
            _hyMotorControl._hyModbus.OnWriteLog += HYmodbus_OnWriteLog;

            var settingsForm = new SettingsControl(this);
            var terminalForm = new TerminalControl(this, settingsForm.csvSeperator);
            _meterControl = new MeterControl(this);
            panelMain.Controls.AddRange(new Control[] { _meterControl, terminalForm, settingsForm });
            foreach (Control ctrl in panelMain.Controls)
            {
                ctrl.Dock = DockStyle.Fill;
                if (ctrl.Tag.ToString() == VFDsettings.LastMDIChild)
                {
                    ctrl.BringToFront();
                    foreach (Button button in panelMDI.Controls)
                        if (button.Tag == ctrl.Tag)
                            button.BackColor = SystemColors.GradientActiveCaption;
                        else button.BackColor = SystemColors.Control;
                }


            }

            settingsForm.InitializeControlValues();
            this.Width = 1500;
            this.Height = 700;

            if (VFDsettings.AutoConnectAtStartup)
            {
                // Ensure HYmodbus has the latest settings before auto-connecting
                _hyMotorControl._hyModbus.PortName = VFDsettings.PortName;
                _hyMotorControl._hyModbus.BaudRate = VFDsettings.BaudRate;
                _hyMotorControl._hyModbus.DataBits = VFDsettings.DataBits;
                _hyMotorControl._hyModbus.Parity = (int)VFDsettings.Parity;
                _hyMotorControl._hyModbus.StopBits = (int)VFDsettings.StopBits;
                _hyMotorControl._hyModbus.ModBusID = VFDsettings.VFD_ModBusID;

                _hyMotorControl._hyModbus.Connect();
                timerInitialPoll.Start();
                stopWatchInitialPoll.Start();
            }

            foreach (var file in Directory.GetFiles("font", "*.ttf"))
                InstallFont.RegisterFont(file);

        }

        public void PopulateQuickSets()
        {
            while (flowLayoutPanelQuickSets.Controls.Count > 0) flowLayoutPanelQuickSets.Controls[0].Dispose();

            string[] quickSetSpeeds = VFDsettings.QuickSets.Split(';');

            foreach (string quickSetSpeed in quickSetSpeeds)
            {
                int targetRPM = Convert.ToInt32(quickSetSpeed);
                Button buttonToAdd = new Button();
                buttonToAdd.Text = Convert.ToInt32(quickSetSpeed).ToString("N0");
                buttonToAdd.Click += buttonSpeed_Click;
                flowLayoutPanelQuickSets.Controls.Add(buttonToAdd);
            }
        }

        #endregion

        #region Button Events

        private void MDI_Select_Click(object sender, EventArgs e)
        {
            Button senderItem = (Button)sender;

            foreach (Button button in panelMDI.Controls)
            {
                if (button == senderItem) button.BackColor = SystemColors.GradientActiveCaption;
                else button.BackColor = SystemColors.Control;

            }

            foreach (Control ctrl in panelMain.Controls)
            {
                if (ctrl.Tag == senderItem.Tag)
                {
                    ctrl.BringToFront();
                    break;
                }
            }

            VFDsettings.LastMDIChild = senderItem.Tag.ToString();

        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (_hyMotorControl._hyModbus.VFDData.SerialConnected)
            {
                if (_meterControl.MeterRPM.Value > 0)
                    if (MessageBox.Show("Spindle appears to still be running, are you sure you wish to disconnect?",
                        "Spindle still running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1)
                            != System.Windows.Forms.DialogResult.Yes) return;

                _hyMotorControl._hyModbus.Disconnect();
                Thread.Sleep(500);
                _meterControl.ZeroAll();
                groupBoxSpindleControl.Enabled = false;
                groupBoxSpindleControl.Enabled = false;
                groupBoxQuickSets.Enabled = false;
                ChangeGtrackbarColours(false);
                _hyMotorControl._hyModbus.VFDData.Clear();
                toolStripStatusRPM.Text = "Current RPM Unknown (Not Connected)";
                toolStripStatusRPM.Image = Resources.orangeLED;
            }
            else
            {
                // Apply current settings from VFDsettings before connecting
                _hyMotorControl._hyModbus.PortName = VFDsettings.PortName;
                _hyMotorControl._hyModbus.BaudRate = VFDsettings.BaudRate;
                _hyMotorControl._hyModbus.DataBits = VFDsettings.DataBits;
                _hyMotorControl._hyModbus.Parity = (int)VFDsettings.Parity;
                _hyMotorControl._hyModbus.StopBits = (int)VFDsettings.StopBits;
                _hyMotorControl._hyModbus.ModBusID = VFDsettings.VFD_ModBusID;

                stopWatchInitialPoll.Reset();
                _hyMotorControl._hyModbus.Connect();
                stopWatchInitialPoll.Start();
                timerInitialPoll.Start();
            }

        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            _hyMotorControl.Start((checkBoxReverse.Checked ? SpindleDirection.Backwards : SpindleDirection.Forward));
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            groupBoxSpindleSpeed.Enabled = true;
            ChangeGtrackbarColours(true);
            groupBoxQuickSets.Enabled = true;
            //gTrackBarSpindleSpeed.Value = Settings.VFD_MinFreq;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            _hyMotorControl.Stop();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            groupBoxSpindleSpeed.Enabled = false;
            gTrackBarSpindleSpeed.Value = 0;
            ChangeGtrackbarColours(false);
            groupBoxQuickSets.Enabled = false;
        }

        private void gTrackBarSpindleSpeed_DoubleClick(object sender, EventArgs e)
        {
            SetSpeedPopup setSpeedPopup = new SetSpeedPopup();
            if (setSpeedPopup.ShowDialog() == DialogResult.OK)
            {
                Console.WriteLine("OK was clicked");
                Console.WriteLine("Checkbox was " + (setSpeedPopup.checkBoxQuickSets.Checked ? "" : "not ") + "checked");
                Console.WriteLine(setSpeedPopup.textBoxRPM.Text);
            }
            else Console.WriteLine("Cancel was clicked");

        }

        private void buttonSpeed_Click(object sender, EventArgs e)
        {
            string senderText = (sender as Button).Text;
            string targetRPM = Regex.Replace(senderText, "[^0-9]", "");
            int rpm = Math.Min(Convert.ToInt32(targetRPM), gTrackBarSpindleSpeed.Maximum);
            gTrackBarSpindleSpeed.Value = rpm;
        }

        #endregion

        #region Various methods

        private void SpindleTalker_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_meterControl?.MeterOutF.Value > 0)
            {
                if (MessageBox.Show("Spindle appears to still be running, are you sure you wish to exit?", "Spindle still running", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) != System.Windows.Forms.DialogResult.OK)
                {
                    e.Cancel = true;
                    return;
                }
            }
            _hyMotorControl?._hyModbus.Disconnect();

            if (_hyMotorControl != null)
                VFDsettings.Save();
        }

        public void COMPortStatus(bool connected)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(() => COMPortStatus(connected))); } catch { }
            }
            else
            {
                toolStripStatusLabelComPort.Image = (connected ? Resources.greenLED : Resources.redLED);
                toolStripStatusLabelComPort.Text = _hyMotorControl?._hyModbus.PortName + (connected ? " (connected)" : " (disconnected)");
                toolStripStatusLabelVFDStatus.Image = Resources.orangeLED;
                toolStripStatusLabelVFDStatus.Text = (connected ? "VFD polling" : "VFD Disconnected");
                buttonConnect.Image = (connected ? Resources.connect2 : Resources.disconnect2);
                string status = (connected ? "opened" : "closed");
                Console.WriteLine(string.Format("{0:H:mm:ss.ff} - Port {1} {2}.", DateTime.Now, _hyMotorControl?._hyModbus.PortName, status));
            }
        }

        private void gTrackBarSpindleSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (gTrackBarSpindleSpeed.Value < _hyMotorControl._hyModbus.VFDData.MinRPM) gTrackBarSpindleSpeed.Value = _hyMotorControl._hyModbus.VFDData.MinRPM;
            _hyMotorControl.SetRPM(gTrackBarSpindleSpeed.Value);
        }

        private void VFDdata_OnChanged(VFDdata data)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(() => VFDdata_OnChanged(data))); } catch { }
            }
            else
            {
                if (data.MaxRPM > 0)
                    gTrackBarSpindleSpeed.Maximum = data.MaxRPM;
            }
        }

        private void ChangeGtrackbarColours(bool enabled)
        {
            gTrackBarSpindleSpeed.Enabled = enabled;
        }

        private void checkBoxReverse_CheckedChanged(object sender, EventArgs e)
        {
            buttonStart.Image = (checkBoxReverse.Checked ? Resources.Reverse : Resources.Start);
        }

        private void timerInitialPoll_Tick(object sender, EventArgs e)
        {
            if (_hyMotorControl._hyModbus.VFDData.InitDataOK())
            {
                timerInitialPoll.Stop();
                PopulateQuickSets();
                toolStripStatusLabelVFDStatus.Text = "VFD Settings Downloaded";
                toolStripStatusLabelVFDStatus.Image = Resources.greenLED;
                groupBoxSpindleControl.Enabled = true;
                Thread.Sleep(100);

                if (_meterControl.MeterRPM.Value > 0)
                {
                    //this.SuspendLayout();
                    buttonStart.Enabled = false;
                    buttonStop.Enabled = true;
                    groupBoxSpindleSpeed.Enabled = true;
                    ChangeGtrackbarColours(true);
                    groupBoxQuickSets.Enabled = true;
                    PopulateQuickSets();
                    gTrackBarSpindleSpeed.Value = Math.Min((int)_meterControl.MeterRPM.Value, gTrackBarSpindleSpeed.Maximum);
                    //this.ResumeLayout();
                }
            }
            else
            {
                if (stopWatchInitialPoll.ElapsedMilliseconds > howLongToWait)
                {
                    timerInitialPoll.Stop();

                    if (MessageBox.Show("The VFD does not appear to be responding. Do you wish to continue waiting", "No Response", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                    {
                        howLongToWait += 5000;
                        stopWatchInitialPoll.Reset();
                        stopWatchInitialPoll.Start();
                        timerInitialPoll.Start();
                    }
                    else
                    {
                        _hyMotorControl._hyModbus.Disconnect();
                        int i = 0;
                        while (i++ < 100 && _hyMotorControl._hyModbus.ComOpen)
                        {
                            Thread.Sleep(50);
                        }
                        COMPortStatus(false);
                        toolStripStatusLabelVFDStatus.Text = "VFD did not respond";
                        toolStripStatusLabelVFDStatus.Image = Resources.redLED;
                    }
                }
            }
        }

        #endregion
    }

}
