using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using UpdateServiceInitializer.Models;

namespace UpdateServiceInitializer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.ComponentModel.BackgroundWorker worker1;

        public MainWindow()
        {
            InitializeComponent();
            load();
        }
        void load()
        {
            Cmb_Apps.Items.Clear();
            foreach (var f in Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory))
                Cmb_Apps.Items.Add(System.IO.Path.GetFileName(f));
            var ens = Enum.GetNames(typeof(UpdateVersionPriority));
            Cmb_Priority.Items.Clear();
            foreach (var en in ens)
                Cmb_Priority.Items.Add(en);
            Cmb_Priority.SelectedIndex = 1;
        }

        private void Cmb_Apps_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Cmb_Apps.SelectedItem == null) return;
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + Cmb_Apps.SelectedItem.ToString() + @"\systemRequirementInfo.json"))
                return;
            var json = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + Cmb_Apps.SelectedItem.ToString() + @"\systemRequirementInfo.json");
            var obj =JsonConvert.DeserializeObject<AppRequirementSystemInfo>(json);
            Txt_MinimumRamSize.Text = obj.MinimumRam.ToString();


            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + Cmb_Apps.SelectedItem.ToString() + @"\UpdateInfo.json"))
                return;
            var json2 = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + Cmb_Apps.SelectedItem.ToString() + @"\UpdateInfo.json");
            var obj2 = JsonConvert.DeserializeObject<UpdateAppInfo>(json2);
            Cmb_Priority.SelectedIndex = (int)obj2.updatePriority;
        }

        private void Btn_Initialize_Click(object sender, RoutedEventArgs e)
        {
            if (Cmb_Priority.SelectedItem == null) { MessageBox.Show("Please Select Priority");return; }
            if(Cmb_Apps.SelectedItem == null) { MessageBox.Show("Please Select App"); return; }
            AddToList(Cmb_Apps.SelectedItem.ToString() + " Start Initializing");
            var p = AppDomain.CurrentDomain.BaseDirectory + Cmb_Apps.SelectedItem.ToString();
            this.Priority = Cmb_Priority.SelectedItem.ToString();
            worker1 = new System.ComponentModel.BackgroundWorker();
            worker1.DoWork += Worker1_DoWork;
            worker1.ProgressChanged += Worker1_ProgressChanged;
            worker1.RunWorkerCompleted += Worker1_RunWorkerCompleted;
            worker1.WorkerSupportsCancellation = true;
            worker1.WorkerReportsProgress = true;

            totalParts = 0;
            var files = Directory.GetFiles(p + @"\files", "*.*", SearchOption.AllDirectories);
            foreach(var f in files)
            {
                var len = new FileInfo(f).Length;
                totalParts += (int)(len / 100000);
                if (len % 100000 > 0) totalParts++;
            }


            var appRequirementSystemInfo = new AppRequirementSystemInfo() { MinimumRam = Convert.ToInt32(Txt_MinimumRamSize.Text) };
            File.WriteAllText(p + @"\systemRequirementInfo.json", JsonConvert.SerializeObject(appRequirementSystemInfo));
            worker1.RunWorkerAsync(p);
        }

        private void Worker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            Prg_InitProgress.Dispatcher.Invoke(new Action<int>((int p)=> {
                Prg_InitProgress.Value = p;
            }),e.ProgressPercentage);
        }

        int totalParts = 0;
        string Priority;
        private void Worker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            AddToList("Update Data Created Successfully!");
            MessageBox.Show("Update Data Created Successfully!");
        }

        private void Worker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var p = e.Argument.ToString();

            File.WriteAllText(p + @"\init.txt", "");

            var InitDirectories = false;
            if (!Directory.Exists(p)) { Directory.CreateDirectory(p); InitDirectories = true; }
            if (!Directory.Exists(p + @"\files")) { Directory.CreateDirectory(p + @"\files"); InitDirectories = true; }
            if (!Directory.Exists(p + @"\parts")) { Directory.CreateDirectory(p + @"\parts"); InitDirectories = true; }
            if (!File.Exists(p + @"\UpdateInfo.json")) { File.WriteAllText(p + @"\UpdateInfo.json", ""); InitDirectories = true; }
            if (!File.Exists(p + @"\New_Features.txt")) { File.WriteAllText(p + @"\New_Features.txt", ""); InitDirectories = true; }

            if (InitDirectories) { MessageBox.Show("Init Directories Completed,Now Copy Files"); return; }
            if (Directory.GetFiles(p, "*.*", SearchOption.AllDirectories).Length == 0) { MessageBox.Show("Please copy update files in (files) folder"); return; }

            string[] files = System.IO.Directory.GetFiles(p + @"\files", "*.*", SearchOption.AllDirectories);
            UpdateAppInfo updateAppInfo = new UpdateAppInfo();
            updateAppInfo.files = new List<UpdateFileInfo>();

            updateAppInfo.updatePriority = (UpdateVersionPriority)Enum.Parse(typeof(UpdateVersionPriority), Priority);
            updateAppInfo.AppId = int.Parse(System.IO.Path.GetFileName(p).Split('-')[0]);
            updateAppInfo.NewFeatures = File.ReadAllText(p + @"\New_Features.txt");

            int partCounter = 0;
            int partSize = 100000;
            foreach (var f1 in Directory.GetFiles(p + $@"\parts"))
            {
                File.Delete(f1);
            }
            foreach (string f in files)
            {
                //create files for each part

                var updateFileInfo = new UpdateFileInfo();
                var fileinfo = new System.IO.FileInfo(f);
                updateFileInfo.CreationTime = fileinfo.CreationTime;
                updateFileInfo.ModifiedTime = fileinfo.LastWriteTime;
                updateFileInfo.IsZipped = (System.IO.Path.GetExtension(f).ToLower() == ".rar") || (System.IO.Path.GetExtension(f).ToLower() == ".zip");
                //updateFileInfo.AssemblyVersion = 
                updateFileInfo.Length = fileinfo.Length;
                updateFileInfo.FileName = f.Substring(p.Length).Replace("\\files", "");//use releative address
                updateFileInfo.StartPartId = partCounter;
                int partCount = (int)Math.Ceiling((double)fileinfo.Length / partSize);
                var bt = File.ReadAllBytes(f);
                for (int i = 0; i < partCount; i++)
                {
                    var bt2 = bt.Skip(i * partSize).Take(partSize).ToArray();
                    File.WriteAllBytes(p + $@"\parts\part_{(partCounter)}", bt2);
                    AddToList($@"Part {partCounter} Created");
                    partCounter++;
                    if (worker1.CancellationPending)
                    {
                        AddToList("Initialize Canceled,Rolling Back");
                        Directory.Delete(p + @"\parts", true);
                        Directory.CreateDirectory(p + @"\parts");
                        return;
                    }
                    worker1.ReportProgress(Convert.ToInt32(((float)partCounter / totalParts)*100));
                }

                updateFileInfo.EndPartId = partCounter - 1;
                updateAppInfo.files.Add(updateFileInfo);
            }
            File.WriteAllText(p + @"\UpdateInfo.json", JsonConvert.SerializeObject(updateAppInfo));
            Lst_Log.Dispatcher.Invoke(new Action(() => {
                Lst_Log.Items.Add($@"UpdateInfo.json Created");
            }));
            File.Delete(p + @"\init.txt");
        }

        void AddToList(string s1)
        {
            Lst_Log.Dispatcher.Invoke(new Action<string>((string s) => {
                Lst_Log.Items.Add(s);
                Lst_Log.ScrollIntoView(Lst_Log.Items[Lst_Log.Items.Count - 1]);
            }), s1);
        }
        private void Btn_OpenAppDir_Click(object sender, RoutedEventArgs e)
        {
            if (Cmb_Apps.SelectedItem == null) { MessageBox.Show("Please Select App"); return; }
            var p = AppDomain.CurrentDomain.BaseDirectory + Cmb_Apps.SelectedItem.ToString();
            Process.Start(p);
        }

        private void Btn_AddApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Txt_AppNAme.Text)) {MessageBox.Show("Please Enter App Name"); return; }
                var exists = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory).Any(x => System.IO.Path.GetFileName(x).Split('-')[1].ToLower() == Txt_AppNAme.Text.ToLower());
                if (exists) { MessageBox.Show("App Name Already Exists"); return; }
                var max = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory).Select(x => Convert.ToInt32(System.IO.Path.GetFileName(x).Split('-')[0])).Max();

                var p = AppDomain.CurrentDomain.BaseDirectory + (max+1).ToString() + "-" + Txt_AppNAme.Text;
                
                Directory.CreateDirectory(p);

                if (!Directory.Exists(p)) { Directory.CreateDirectory(p); }
                if (!Directory.Exists(p + @"\files")) { Directory.CreateDirectory(p + @"\files"); }
                if (!Directory.Exists(p + @"\parts")) { Directory.CreateDirectory(p + @"\parts"); }
                if (!File.Exists(p + @"\UpdateInfo.json")) { File.WriteAllText(p + @"\UpdateInfo.json", ""); }
                if (!File.Exists(p + @"\New_Features.txt")) { File.WriteAllText(p + @"\New_Features.txt", ""); }

                Txt_AppNAme.Text = "";
                MessageBox.Show("Copy files in the (files) folder and then find your app in Apps ComboBox And Press Initialize Button");

                if (Chk_OpenAfterCreateApp.IsChecked ?? false)
                {
                    Process.Start(p); 
                }
                load();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Btn_StopInitialize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                worker1.CancelAsync();
            }
            catch { }
        }

        private void Btn_ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Lst_Log.Items.Clear();
        }

    }
}
