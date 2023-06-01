using System;
using System.IO;
using System.Windows;
using Updater.Models;

namespace SampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Updater.MainUpdater mainUpdater;
        public MainWindow()
        {
            InitializeComponent();
            

            mainUpdater = new Updater.MainUpdater(new ClientParams
            {
                LogToTxtFile = true,
                ServerUrl = "http://localhost:57718/UpdateService.svc",
                TargetAppName = "SampleApp",
                CreateBackup = false,
                Force = false,
                LatencyBetweenParts = 50
            });

            mainUpdater.Evt_ProgressChanged += MainUpdater_Evt_ProgressChanged;
            mainUpdater.Evt_Logs += MainUpdater_Evt_Logs;
            mainUpdater.Evt_UpdateEvents += MainUpdater_Evt_UpdateEvents;


            if (mainUpdater.UnapplyUpdateExists())
            {
                if (MessageBox.Show($"Unapplied Update Found ,Apply Now?", "restart", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                mainUpdater.RestartAppAndApplyUpdate();
            }

            foreach (var b in mainUpdater.GetBackups())
                Cmb_Backups.Items.Add(b);

        }


        private void MainUpdater_Evt_UpdateEvents(UpdateEventState state)
        {
            switch (state)
            {
                case UpdateEventState.UpdateCompleted:
                    if (MessageBox.Show($"Application requires restart", "restart", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                    mainUpdater.RestartAppAndApplyUpdate();
                    break;
                default:
                    MessageBox.Show(state.ToString());
                break;
            }
        }

        private async void Btn_StartUpdate_Click(object sender, RoutedEventArgs e)
        {
            mainUpdater.ClientParams.Force = Chk_ForceUpdate.IsChecked ?? false;
            mainUpdater.ClientParams.CreateBackup = Chk_CreateBackup.IsChecked ?? false;

            if (!await mainUpdater.InitAsync()) return;

            Txb_RemainSize.Text = Updater.MainUpdater.EasyReadLength(mainUpdater.UpdateSize);
            mainUpdater.StartUpdate();
        }
        
        private void MainUpdater_Evt_Logs(LogTypes arg1, string arg2)
        {
            this.Dispatcher.Invoke(new Action<string>((string s) =>
            {
                Lst1.Items.Add(s);
            }), arg2);
        }

        private void MainUpdater_Evt_ProgressChanged(int obj)
        {
            Txb_RemainSize.Dispatcher.Invoke(new Action(() => { Txb_RemainSize.Text = Updater.MainUpdater.EasyReadLength(mainUpdater.RemainingUpdateSize); }));
            Prg_Update.Dispatcher.Invoke(new Action<int>((int p) => { Prg_Update.Value = p; }),obj);
            Txb_ProgressPercent.Dispatcher.Invoke(new Action<string>((string p) => { Txb_ProgressPercent.Text = p; }), obj.ToString());
        }

        

        private void Btn_CancelUpdate_Click(object sender, RoutedEventArgs e)
        {
            mainUpdater.CancelAsync();
        }
        
        private void Btn_ClearLogHistory_Click(object sender, RoutedEventArgs e)
        {
            Lst1.Items.Clear();
        }

        private void Btn_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            mainUpdater.RestoreBackup(Cmb_Backups.SelectedItem.ToString());
        }
    }
}
