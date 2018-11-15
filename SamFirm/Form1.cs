﻿namespace SamFirm
{
    using Microsoft.WindowsAPICodePack.Taskbar;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Windows.Forms;

    public class Form1 : Form
    {
        private CheckBox binary_checkbox;
        private Label binary_lbl;
        private CheckBox checkbox_auto;
        private CheckBox checkbox_autodecrypt;
        private CheckBox checkbox_crc;
        private CheckBox checkbox_manual;
        private IContainer components;
        private Label csc_lbl;
        private TextBox csc_textbox;
        private Button decrypt_button;
        private string destinationfile;
        private Button download_button;
        private Label file_lbl;
        private TextBox file_textbox;
        private SamFirm.Command.Firmware FW;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private Label label1;
        public Label lbl_speed;
        public RichTextBox log_textbox;
        private Label model_lbl;
        private TextBox model_textbox;
        public bool PauseDownload;
        private Label pda_lbl;
        private TextBox pda_textbox;
        private Label phone_lbl;
        private TextBox phone_textbox;
        private ProgressBar progressBar;
        private Label region_lbl;
        private TextBox region_textbox;
        private bool SaveFileDialog = true;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private Label size_lbl;
        private TextBox size_textbox;
        private ToolTip tooltip_binary;
        private ToolTip tooltip_binary_box;
        private Button update_button;
        private Label version_lbl;
        private TextBox version_textbox;

        public Form1()
        {
            this.InitializeComponent();
        }

        private void checkbox_auto_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkbox_manual.Checked && !this.checkbox_auto.Checked)
            {
                this.checkbox_auto.Checked = true;
            }
            else
            {
                this.checkbox_manual.Checked = !this.checkbox_auto.Checked;
                this.pda_textbox.Enabled = !this.checkbox_auto.Checked;
                this.csc_textbox.Enabled = !this.checkbox_auto.Checked;
                this.phone_textbox.Enabled = !this.checkbox_auto.Checked;
            }
        }

        private void checkbox_manual_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.checkbox_auto.Checked && !this.checkbox_manual.Checked)
            {
                this.checkbox_manual.Checked = true;
            }
            else
            {
                this.checkbox_auto.Checked = !this.checkbox_manual.Checked;
                this.pda_textbox.Enabled = this.checkbox_manual.Checked;
                this.csc_textbox.Enabled = this.checkbox_manual.Checked;
                this.phone_textbox.Enabled = this.checkbox_manual.Checked;
            }
        }

        private void ControlsEnabled(bool Enabled)
        {
            this.update_button.Invoke(new Action(() => this.update_button.Enabled = Enabled));
            this.download_button.Invoke(new Action(() => this.download_button.Enabled = Enabled));
            this.binary_checkbox.Invoke(new Action(() => this.binary_checkbox.Enabled = Enabled));
            this.model_textbox.Invoke(new Action(() => this.model_textbox.Enabled = Enabled));
            this.region_textbox.Invoke(new Action(() => this.region_textbox.Enabled = Enabled));
            this.checkbox_auto.Invoke(new Action(() => this.checkbox_auto.Enabled = Enabled));
            this.checkbox_manual.Invoke(new Action(() => this.checkbox_manual.Enabled = Enabled));
            this.checkbox_manual.Invoke(new Action(delegate {
                if (this.checkbox_manual.Checked)
                {
                    this.pda_textbox.Enabled = Enabled;
                    this.csc_textbox.Enabled = Enabled;
                    this.phone_textbox.Enabled = Enabled;
                }
            }));
            this.checkbox_autodecrypt.Invoke(new Action(() => this.checkbox_autodecrypt.Enabled = Enabled));
            this.checkbox_crc.Invoke(new Action(() => this.checkbox_crc.Enabled = Enabled));
        }

        private void decrypt_button_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(this.destinationfile))
            {
                Logger.WriteLog("Error: File " + this.destinationfile + " does not exist", false);
            }
            else
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += delegate (object o, DoWorkEventArgs _e) {
                    Thread.Sleep(100);
                    Logger.WriteLog("\nDecrypting firmware...", false);
                    this.ControlsEnabled(false);
                    this.decrypt_button.Invoke(new Action(() => this.decrypt_button.Enabled = false));
                    if (this.destinationfile.EndsWith(".enc2"))
                    {
                        Crypto.SetDecryptKey(this.FW.Region, this.FW.Model, this.FW.Version);
                    }
                    else if (this.destinationfile.EndsWith(".enc4"))
                    {
                        if (this.FW.BinaryNature == 1)
                        {
                            Crypto.SetDecryptKey(this.FW.Version, this.FW.LogicValueFactory);
                        }
                        else
                        {
                            Crypto.SetDecryptKey(this.FW.Version, this.FW.LogicValueHome);
                        }
                    }
                    if (Crypto.Decrypt(this.destinationfile, Path.Combine(Path.GetDirectoryName(this.destinationfile), Path.GetFileNameWithoutExtension(this.destinationfile)), true) == 0)
                    {
                        System.IO.File.Delete(this.destinationfile);
                    }
                    Logger.WriteLog("Decryption finished", false);
                    this.ControlsEnabled(true);
                };
                worker.RunWorkerAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void download_button_Click(object sender, EventArgs e)
        {
            if (this.download_button.Text == "Pause")
            {
                Utility.TaskBarProgressState(true);
                this.PauseDownload = true;
                Utility.ReconnectDownload = false;
                this.download_button.Text = "Download";
            }
            else
            {
                if (((e != null) && (e.GetType() == typeof(DownloadEventArgs))) && ((DownloadEventArgs) e).isReconnect)
                {
                    if (this.download_button.Text == "Pause")
                    {
                        return;
                    }
                    if (!Utility.ReconnectDownload)
                    {
                        return;
                    }
                }
                if (this.PauseDownload)
                {
                    Logger.WriteLog("Download thread is still running. Please wait.", false);
                }
                else if (string.IsNullOrEmpty(this.file_textbox.Text))
                {
                    Logger.WriteLog("No file to download. Please check for update first.", false);
                }
                else
                {
                    if ((e.GetType() != typeof(DownloadEventArgs)) || !((DownloadEventArgs) e).isReconnect)
                    {
                        if (this.SaveFileDialog)
                        {
                            string extension = Path.GetExtension(Path.GetFileNameWithoutExtension(this.FW.Filename));
                            string oldValue = extension + Path.GetExtension(this.FW.Filename);
                            this.saveFileDialog1.SupportMultiDottedExtensions = true;
                            this.saveFileDialog1.OverwritePrompt = false;
                            this.saveFileDialog1.FileName = this.FW.Filename.Replace(oldValue, "");
                            this.saveFileDialog1.Filter = "Firmware|*" + oldValue;
                            if (this.saveFileDialog1.ShowDialog() != DialogResult.OK)
                            {
                                Logger.WriteLog("Aborted.", false);
                                return;
                            }
                            if (!this.saveFileDialog1.FileName.EndsWith(oldValue))
                            {
                                this.saveFileDialog1.FileName = this.saveFileDialog1.FileName + oldValue;
                            }
                            else
                            {
                                this.saveFileDialog1.FileName = this.saveFileDialog1.FileName.Replace(oldValue + oldValue, oldValue);
                            }
                            Logger.WriteLog("Filename: " + this.saveFileDialog1.FileName, false);
                            this.destinationfile = this.saveFileDialog1.FileName;
                            if (System.IO.File.Exists(this.destinationfile))
                            {
                                customMessageBox box = new customMessageBox("The destination file already exists.\r\nWould you like to append it (resume download)?", "Append", DialogResult.Yes, "Overwrite", DialogResult.No, "Cancel", DialogResult.Cancel, SystemIcons.Warning.ToBitmap());
                                switch (box.ShowDialog())
                                {
                                    case DialogResult.No:
                                        System.IO.File.Delete(this.destinationfile);
                                        break;

                                    case DialogResult.Cancel:
                                        Logger.WriteLog("Aborted.", false);
                                        return;
                                }
                            }
                        }
                        else
                        {
                            this.destinationfile = this.FW.Filename;
                        }
                    }
                    Utility.TaskBarProgressState(false);
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += delegate (object o, DoWorkEventArgs _e) {
                        MethodInvoker method = null;
                        MethodInvoker invoker2 = null;
                        MethodInvoker invoker3 = null;
                        try
                        {
                            this.ControlsEnabled(false);
                            Utility.ReconnectDownload = false;
                            if (method == null)
                            {
                                method = delegate {
                                    this.download_button.Enabled = true;
                                    this.download_button.Text = "Pause";
                                };
                            }
                            this.download_button.Invoke(method);
                            if (this.FW.Filename == this.destinationfile)
                            {
                                Logger.WriteLog("Trying to download " + this.FW.Filename, false);
                            }
                            else
                            {
                                Logger.WriteLog("Trying to download " + this.FW.Filename + " to " + this.destinationfile, false);
                            }
                            SamFirm.Command.Download(this.FW.Path, this.FW.Filename, this.FW.Version, this.FW.Region, this.FW.Model_Type, this.destinationfile, this.FW.Size, true);
                            if (this.PauseDownload)
                            {
                                Logger.WriteLog("Download paused", false);
                                this.PauseDownload = false;
                                if (Utility.ReconnectDownload)
                                {
                                    Logger.WriteLog("Reconnecting...", false);
                                    Utility.Reconnect(new Action<object, EventArgs>(this.download_button_Click));
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Download finished", false);
                                if (this.checkbox_crc.Checked)
                                {
                                    if (this.FW.CRC == null)
                                    {
                                        Logger.WriteLog("Unable to check CRC. Value not set by Samsung", false);
                                    }
                                    else
                                    {
                                        Logger.WriteLog("\nChecking CRC32...", false);
                                        if (!Utility.CRCCheck(this.destinationfile, this.FW.CRC))
                                        {
                                            Logger.WriteLog("Error: CRC does not match. Please redownload the file.", false);
                                            System.IO.File.Delete(this.destinationfile);
                                            goto Label_01C9;
                                        }
                                        Logger.WriteLog("Success: CRC match!", false);
                                    }
                                }
                                if (invoker2 == null)
                                {
                                    invoker2 = () => this.decrypt_button.Enabled = true;
                                }
                                this.decrypt_button.Invoke(invoker2);
                                if (this.checkbox_autodecrypt.Checked)
                                {
                                    this.decrypt_button_Click(o, null);
                                }
                            }
                        Label_01C9:
                            if (!Utility.ReconnectDownload)
                            {
                                this.ControlsEnabled(true);
                            }
                            if (invoker3 == null)
                            {
                                invoker3 = () => this.download_button.Text = "Download";
                            }
                            this.download_button.Invoke(invoker3);
                        }
                        catch (Exception exception)
                        {
                            Logger.WriteLog(exception.Message, false);
                            Logger.WriteLog(exception.ToString(), false);
                        }
                    };
                    worker.RunWorkerAsync();
                }
            }
        }

        private void Form1_Close(object sender, EventArgs e)
        {
            Settings.SetSetting("Model", this.model_textbox.Text.ToUpper());
            Settings.SetSetting("Region", this.region_textbox.Text.ToUpper());
            Settings.SetSetting("PDAVer", this.pda_textbox.Text);
            Settings.SetSetting("CSCVer", this.csc_textbox.Text);
            Settings.SetSetting("PHONEVer", this.phone_textbox.Text);
            Settings.SetSetting("AutoInfo", this.checkbox_auto.Checked.ToString());
            Settings.SetSetting("SaveFileDialog", this.SaveFileDialog.ToString());
            Settings.SetSetting("BinaryNature", this.binary_checkbox.Checked.ToString());
            Settings.SetSetting("CheckCRC", this.checkbox_crc.Checked.ToString());
            Settings.SetSetting("AutoDecrypt", this.checkbox_autodecrypt.Checked.ToString());
            this.PauseDownload = true;
            Thread.Sleep(100);
            Imports.FreeModule();
            Logger.SaveLog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Logger.form = this;
            Web.form = this;
            Crypto.form = this;
            this.model_textbox.Text = Settings.ReadSetting("Model");
            this.region_textbox.Text = Settings.ReadSetting("Region");
            this.pda_textbox.Text = Settings.ReadSetting("PDAVer");
            this.csc_textbox.Text = Settings.ReadSetting("CSCVer");
            this.phone_textbox.Text = Settings.ReadSetting("PHONEVer");
            if (Settings.ReadSetting("AutoInfo").ToLower() == "true")
            {
                this.checkbox_auto.Checked = true;
            }
            else
            {
                this.checkbox_manual.Checked = true;
            }
            if (Settings.ReadSetting("SaveFileDialog").ToLower() == "false")
            {
                this.SaveFileDialog = false;
            }
            if (Settings.ReadSetting("BinaryNature").ToLower() == "true")
            {
                this.binary_checkbox.Checked = true;
            }
            if (Settings.ReadSetting("CheckCRC").ToLower() == "false")
            {
                this.checkbox_crc.Checked = false;
            }
            if (Settings.ReadSetting("AutoDecrypt").ToLower() == "false")
            {
                this.checkbox_autodecrypt.Checked = false;
            }
            this.tooltip_binary.SetToolTip(this.binary_lbl, "Full firmware including PIT file");
            this.tooltip_binary_box.SetToolTip(this.binary_checkbox, "Full firmware including PIT file");
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Logger.WriteLog("SamFirm v" + versionInfo.FileVersion, false);
            ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => true;
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            ComponentResourceManager manager = new ComponentResourceManager(typeof(Form1));
            this.model_textbox = new TextBox();
            this.model_lbl = new Label();
            this.download_button = new Button();
            this.log_textbox = new RichTextBox();
            this.region_lbl = new Label();
            this.region_textbox = new TextBox();
            this.pda_lbl = new Label();
            this.pda_textbox = new TextBox();
            this.csc_lbl = new Label();
            this.csc_textbox = new TextBox();
            this.update_button = new Button();
            this.phone_lbl = new Label();
            this.phone_textbox = new TextBox();
            this.file_lbl = new Label();
            this.file_textbox = new TextBox();
            this.version_lbl = new Label();
            this.version_textbox = new TextBox();
            this.groupBox1 = new GroupBox();
            this.groupBox3 = new GroupBox();
            this.checkbox_manual = new CheckBox();
            this.checkbox_auto = new CheckBox();
            this.binary_checkbox = new CheckBox();
            this.binary_lbl = new Label();
            this.progressBar = new ProgressBar();
            this.decrypt_button = new Button();
            this.groupBox2 = new GroupBox();
            this.label1 = new Label();
            this.lbl_speed = new Label();
            this.checkbox_autodecrypt = new CheckBox();
            this.checkbox_crc = new CheckBox();
            this.size_textbox = new TextBox();
            this.size_lbl = new Label();
            this.tooltip_binary = new ToolTip(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.tooltip_binary_box = new ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            base.SuspendLayout();
            this.model_textbox.CharacterCasing = CharacterCasing.Upper;
            this.model_textbox.Location = new Point(0x55, 0x16);
            this.model_textbox.Name = "model_textbox";
            this.model_textbox.Size = new Size(0x95, 20);
            this.model_textbox.TabIndex = 0;
            this.model_lbl.AutoSize = true;
            this.model_lbl.Location = new Point(8, 0x19);
            this.model_lbl.Name = "model_lbl";
            this.model_lbl.Size = new Size(0x24, 13);
            this.model_lbl.TabIndex = 1;
            this.model_lbl.Text = "Model";
            this.download_button.Location = new Point(0x4a, 0x75);
            this.download_button.Margin = new Padding(0);
            this.download_button.Name = "download_button";
            this.download_button.Size = new Size(0x5e, 0x17);
            this.download_button.TabIndex = 13;
            this.download_button.Text = "Download";
            this.download_button.UseVisualStyleBackColor = true;
            this.download_button.Click += new EventHandler(this.download_button_Click);
            this.log_textbox.Location = new Point(12, 0xe9);
            this.log_textbox.Name = "log_textbox";
            this.log_textbox.ReadOnly = true;
            this.log_textbox.Size = new Size(0x27f, 0x8a);
            this.log_textbox.TabIndex = 3;
            this.log_textbox.TabStop = false;
            this.log_textbox.Text = "";
            this.region_lbl.AutoSize = true;
            this.region_lbl.Location = new Point(8, 0x33);
            this.region_lbl.Name = "region_lbl";
            this.region_lbl.Size = new Size(0x29, 13);
            this.region_lbl.TabIndex = 5;
            this.region_lbl.Text = "Region";
            this.region_textbox.CharacterCasing = CharacterCasing.Upper;
            this.region_textbox.Location = new Point(0x55, 0x30);
            this.region_textbox.Name = "region_textbox";
            this.region_textbox.Size = new Size(0x95, 20);
            this.region_textbox.TabIndex = 1;
            this.pda_lbl.AutoSize = true;
            this.pda_lbl.Location = new Point(10, 15);
            this.pda_lbl.Name = "pda_lbl";
            this.pda_lbl.Size = new Size(0x1d, 13);
            this.pda_lbl.TabIndex = 7;
            this.pda_lbl.Text = "PDA";
            this.pda_textbox.CharacterCasing = CharacterCasing.Upper;
            this.pda_textbox.Location = new Point(0x4f, 12);
            this.pda_textbox.Name = "pda_textbox";
            this.pda_textbox.Size = new Size(0x95, 20);
            this.pda_textbox.TabIndex = 4;
            this.csc_lbl.AutoSize = true;
            this.csc_lbl.Location = new Point(10, 0x29);
            this.csc_lbl.Name = "csc_lbl";
            this.csc_lbl.Size = new Size(0x1c, 13);
            this.csc_lbl.TabIndex = 9;
            this.csc_lbl.Text = "CSC";
            this.csc_textbox.CharacterCasing = CharacterCasing.Upper;
            this.csc_textbox.Location = new Point(0x4f, 0x26);
            this.csc_textbox.Name = "csc_textbox";
            this.csc_textbox.Size = new Size(0x95, 20);
            this.csc_textbox.TabIndex = 5;
            this.update_button.Location = new Point(0x8d, 0xba);
            this.update_button.Name = "update_button";
            this.update_button.Size = new Size(0x5d, 0x17);
            this.update_button.TabIndex = 10;
            this.update_button.Text = "Check Update";
            this.update_button.UseVisualStyleBackColor = true;
            this.update_button.Click += new EventHandler(this.update_button_Click);
            this.phone_lbl.AutoSize = true;
            this.phone_lbl.Location = new Point(10, 0x43);
            this.phone_lbl.Name = "phone_lbl";
            this.phone_lbl.Size = new Size(0x26, 13);
            this.phone_lbl.TabIndex = 12;
            this.phone_lbl.Text = "Phone";
            this.phone_textbox.CharacterCasing = CharacterCasing.Upper;
            this.phone_textbox.Location = new Point(0x4f, 0x40);
            this.phone_textbox.Name = "phone_textbox";
            this.phone_textbox.Size = new Size(0x95, 20);
            this.phone_textbox.TabIndex = 6;
            this.file_lbl.AutoSize = true;
            this.file_lbl.Location = new Point(6, 0x19);
            this.file_lbl.Name = "file_lbl";
            this.file_lbl.Size = new Size(0x17, 13);
            this.file_lbl.TabIndex = 13;
            this.file_lbl.Text = "File";
            this.file_textbox.Location = new Point(0x4b, 0x12);
            this.file_textbox.Name = "file_textbox";
            this.file_textbox.ReadOnly = true;
            this.file_textbox.Size = new Size(290, 20);
            this.file_textbox.TabIndex = 20;
            this.file_textbox.TabStop = false;
            this.version_lbl.AutoSize = true;
            this.version_lbl.Location = new Point(6, 0x33);
            this.version_lbl.Name = "version_lbl";
            this.version_lbl.Size = new Size(0x2a, 13);
            this.version_lbl.TabIndex = 15;
            this.version_lbl.Text = "Version";
            this.version_textbox.Location = new Point(0x4b, 0x2c);
            this.version_textbox.Name = "version_textbox";
            this.version_textbox.ReadOnly = true;
            this.version_textbox.Size = new Size(290, 20);
            this.version_textbox.TabIndex = 30;
            this.version_textbox.TabStop = false;
            this.groupBox1.Controls.Add(this.groupBox3);
            this.groupBox1.Controls.Add(this.checkbox_manual);
            this.groupBox1.Controls.Add(this.checkbox_auto);
            this.groupBox1.Controls.Add(this.binary_checkbox);
            this.groupBox1.Controls.Add(this.binary_lbl);
            this.groupBox1.Controls.Add(this.model_textbox);
            this.groupBox1.Controls.Add(this.model_lbl);
            this.groupBox1.Controls.Add(this.update_button);
            this.groupBox1.Controls.Add(this.region_textbox);
            this.groupBox1.Controls.Add(this.region_lbl);
            this.groupBox1.Location = new Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(0x106, 0xd7);
            this.groupBox1.TabIndex = 0x11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Firmware Info";
            this.groupBox3.Controls.Add(this.phone_textbox);
            this.groupBox3.Controls.Add(this.csc_lbl);
            this.groupBox3.Controls.Add(this.csc_textbox);
            this.groupBox3.Controls.Add(this.pda_lbl);
            this.groupBox3.Controls.Add(this.pda_textbox);
            this.groupBox3.Controls.Add(this.phone_lbl);
            this.groupBox3.Location = new Point(6, 0x5d);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new Size(250, 0x59);
            this.groupBox3.TabIndex = 0x11;
            this.groupBox3.TabStop = false;
            this.checkbox_manual.AutoSize = true;
            this.checkbox_manual.Location = new Point(0x81, 0x4b);
            this.checkbox_manual.Name = "checkbox_manual";
            this.checkbox_manual.Size = new Size(0x3d, 0x11);
            this.checkbox_manual.TabIndex = 3;
            this.checkbox_manual.Text = "Manual";
            this.checkbox_manual.UseVisualStyleBackColor = true;
            this.checkbox_manual.CheckedChanged += new EventHandler(this.checkbox_manual_CheckedChanged);
            this.checkbox_auto.AutoSize = true;
            this.checkbox_auto.Location = new Point(11, 0x4b);
            this.checkbox_auto.Name = "checkbox_auto";
            this.checkbox_auto.Size = new Size(0x30, 0x11);
            this.checkbox_auto.TabIndex = 2;
            this.checkbox_auto.Text = "Auto";
            this.checkbox_auto.UseVisualStyleBackColor = true;
            this.checkbox_auto.CheckedChanged += new EventHandler(this.checkbox_auto_CheckedChanged);
            this.binary_checkbox.AutoSize = true;
            this.binary_checkbox.Location = new Point(0x55, 0xbf);
            this.binary_checkbox.Name = "binary_checkbox";
            this.binary_checkbox.Size = new Size(15, 14);
            this.binary_checkbox.TabIndex = 7;
            this.binary_checkbox.UseVisualStyleBackColor = true;
            this.binary_lbl.AutoSize = true;
            this.binary_lbl.Location = new Point(8, 190);
            this.binary_lbl.Name = "binary_lbl";
            this.binary_lbl.Size = new Size(0x47, 13);
            this.binary_lbl.TabIndex = 13;
            this.binary_lbl.Text = "Binary Nature";
            this.progressBar.Location = new Point(0x4b, 0x92);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(290, 0x17);
            this.progressBar.TabIndex = 0x12;
            this.decrypt_button.Enabled = false;
            this.decrypt_button.Location = new Point(0xbc, 0x75);
            this.decrypt_button.Name = "decrypt_button";
            this.decrypt_button.Size = new Size(0x7f, 0x17);
            this.decrypt_button.TabIndex = 14;
            this.decrypt_button.Text = "Decrypt";
            this.decrypt_button.UseVisualStyleBackColor = true;
            this.decrypt_button.Click += new EventHandler(this.decrypt_button_Click);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.lbl_speed);
            this.groupBox2.Controls.Add(this.checkbox_autodecrypt);
            this.groupBox2.Controls.Add(this.checkbox_crc);
            this.groupBox2.Controls.Add(this.size_textbox);
            this.groupBox2.Controls.Add(this.size_lbl);
            this.groupBox2.Controls.Add(this.progressBar);
            this.groupBox2.Controls.Add(this.decrypt_button);
            this.groupBox2.Controls.Add(this.download_button);
            this.groupBox2.Controls.Add(this.file_lbl);
            this.groupBox2.Controls.Add(this.file_textbox);
            this.groupBox2.Controls.Add(this.version_textbox);
            this.groupBox2.Controls.Add(this.version_lbl);
            this.groupBox2.Location = new Point(280, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new Size(0x173, 0xd7);
            this.groupBox2.TabIndex = 20;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Download";
            this.label1.AutoSize = true;
            this.label1.Location = new Point(6, 0xb0);
            this.label1.Name = "label1";
            this.label1.Size = new Size(0x26, 13);
            this.label1.TabIndex = 0x19;
            this.label1.Text = "Speed";
            this.lbl_speed.AutoSize = true;
            this.lbl_speed.Location = new Point(0x48, 0xb0);
            this.lbl_speed.Name = "lbl_speed";
            this.lbl_speed.Size = new Size(0x24, 13);
            this.lbl_speed.TabIndex = 0x18;
            this.lbl_speed.Text = "0kB/s";
            this.checkbox_autodecrypt.AutoSize = true;
            this.checkbox_autodecrypt.Checked = true;
            this.checkbox_autodecrypt.CheckState = CheckState.Checked;
            this.checkbox_autodecrypt.Location = new Point(0xbd, 0x60);
            this.checkbox_autodecrypt.Name = "checkbox_autodecrypt";
            this.checkbox_autodecrypt.Size = new Size(0x7f, 0x11);
            this.checkbox_autodecrypt.TabIndex = 12;
            this.checkbox_autodecrypt.Text = "Decrypt automatically";
            this.checkbox_autodecrypt.UseVisualStyleBackColor = true;
            this.checkbox_crc.AutoSize = true;
            this.checkbox_crc.Checked = true;
            this.checkbox_crc.CheckState = CheckState.Checked;
            this.checkbox_crc.Location = new Point(0x4b, 0x60);
            this.checkbox_crc.Name = "checkbox_crc";
            this.checkbox_crc.Size = new Size(0x5e, 0x11);
            this.checkbox_crc.TabIndex = 11;
            this.checkbox_crc.Text = "Check CRC32";
            this.checkbox_crc.UseVisualStyleBackColor = true;
            this.size_textbox.Location = new Point(0x4b, 70);
            this.size_textbox.Name = "size_textbox";
            this.size_textbox.ReadOnly = true;
            this.size_textbox.Size = new Size(290, 20);
            this.size_textbox.TabIndex = 40;
            this.size_textbox.TabStop = false;
            this.size_lbl.AutoSize = true;
            this.size_lbl.Location = new Point(6, 0x4b);
            this.size_lbl.Name = "size_lbl";
            this.size_lbl.Size = new Size(0x1b, 13);
            this.size_lbl.TabIndex = 20;
            this.size_lbl.Text = "Size";
            this.saveFileDialog1.SupportMultiDottedExtensions = true;
            base.AcceptButton = this.update_button;
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.ClientSize = new Size(0x29c, 0x17f);
            base.Controls.Add(this.groupBox2);
            base.Controls.Add(this.groupBox1);
            base.Controls.Add(this.log_textbox);
            base.Icon = (Icon) manager.GetObject("$this.Icon");
            base.Name = "Form1";
            this.Text = "SamFirm";
            base.FormClosing += new FormClosingEventHandler(this.Form1_Close);
            base.Load += new EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            base.ResumeLayout(false);
        }

        public void SetProgressBar(int Progress)
        {
            if (Progress > 100)
            {
                Progress = 100;
            }
            this.progressBar.Invoke(new Action(delegate {
                this.progressBar.Value = Progress;
                try
                {
                    TaskbarManager.Instance.SetProgressValue(Progress, 100);
                }
                catch (Exception)
                {
                }
            }));
        }

        private void update_button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.model_textbox.Text))
            {
                Logger.WriteLog("Error: Please specify a model", false);
            }
            else if (string.IsNullOrEmpty(this.region_textbox.Text))
            {
                Logger.WriteLog("Error: Please specify a region", false);
            }
            else if (this.checkbox_manual.Checked && ((string.IsNullOrEmpty(this.pda_textbox.Text) || string.IsNullOrEmpty(this.csc_textbox.Text)) || string.IsNullOrEmpty(this.phone_textbox.Text)))
            {
                Logger.WriteLog("Error: Please specify PDA, CSC and Phone version or use Auto Method", false);
            }
            else
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += delegate (object o, DoWorkEventArgs _e) {
                    MethodInvoker method = null;
                    MethodInvoker invoker2 = null;
                    MethodInvoker invoker3 = null;
                    MethodInvoker invoker4 = null;
                    MethodInvoker invoker5 = null;
                    MethodInvoker invoker6 = null;
                    try
                    {
                        this.SetProgressBar(0);
                        this.ControlsEnabled(false);
                        Utility.ReconnectDownload = false;
                        if (this.checkbox_auto.Checked)
                        {
                            this.FW = SamFirm.Command.UpdateCheckAuto(this.model_textbox.Text, this.region_textbox.Text, this.binary_checkbox.Checked);
                        }
                        else
                        {
                            this.FW = SamFirm.Command.UpdateCheck(this.model_textbox.Text, this.region_textbox.Text, this.pda_textbox.Text, this.csc_textbox.Text, this.phone_textbox.Text, this.pda_textbox.Text, this.binary_checkbox.Checked, false);
                        }
                        if (!string.IsNullOrEmpty(this.FW.Filename))
                        {
                            if (method == null)
                            {
                                method = () => this.file_textbox.Text = this.FW.Filename;
                            }
                            this.file_textbox.Invoke(method);
                            if (invoker2 == null)
                            {
                                invoker2 = () => this.version_textbox.Text = this.FW.Version;
                            }
                            this.version_textbox.Invoke(invoker2);
                            if (invoker3 == null)
                            {
                                invoker3 = () => this.size_textbox.Text = ((long.Parse(this.FW.Size) / 0x400L) / 0x400L) + " MB";
                            }
                            this.size_textbox.Invoke(invoker3);
                        }
                        else
                        {
                            if (invoker4 == null)
                            {
                                invoker4 = () => this.file_textbox.Text = string.Empty;
                            }
                            this.file_textbox.Invoke(invoker4);
                            if (invoker5 == null)
                            {
                                invoker5 = () => this.version_textbox.Text = string.Empty;
                            }
                            this.version_textbox.Invoke(invoker5);
                            if (invoker6 == null)
                            {
                                invoker6 = () => this.size_textbox.Text = string.Empty;
                            }
                            this.size_textbox.Invoke(invoker6);
                        }
                        this.ControlsEnabled(true);
                    }
                    catch (Exception exception)
                    {
                        Logger.WriteLog(exception.Message, false);
                        Logger.WriteLog(exception.ToString(), false);
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        public class DownloadEventArgs : EventArgs
        {
            public bool isReconnect;
        }
    }
}

