﻿using Morphic.Focus.JSONService;
using Morphic.Focus.Models;
using Morphic.Focus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Morphic.Focus.Screens
{
    public delegate void EventHandler(Session? currSession1);

    /// <summary>
    /// Interaction logic for winFocusMain.xaml
    /// </summary>
    public partial class FocusMain : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private ObservableCollection<BlockList> blockLists = new ObservableCollection<BlockList>();
        private bool _isFocusRunning = false;
        Settings scrSettings = null;
        public event EventHandler SessionUpdate;
        private Session _currSession1 = null;

        public FocusMain()
        {
            InitializeComponent();

            GetBlockLists();
            CheckIsFocusRunning();
            DataContext = this;
        }

        private void CheckIsFocusRunning()
        {
            IsFocusRunning = File.Exists(Common.MakeFilePath(Common.SESSION_FILE_NAME));
        }

        public bool IsFocusRunning
        {
            get
            {
                return _isFocusRunning;
            }
            set
            {
                _isFocusRunning = value;
                NotifyPropertyChanged("IsFocusRunning"); // method implemented below

                if (SessionUpdate != null)
                {
                    SessionUpdate(CurrSession1);
                }
            }
        }

        private void GetBlockLists()
        {
            IDataService<BlockList> dataService = new GenericDataService<BlockList>(new FocusDbContextFactory());
            BlockLists = new ObservableCollection<BlockList>(dataService.GetAll().Result);
        }

        public ObservableCollection<BlockList> BlockLists
        {
            get
            {
                return blockLists;
            }
            set
            {
                blockLists = value;
                NotifyPropertyChanged("BlockLists"); // method implemented below
            }
        }

        public Session CurrSession1 { get => _currSession1; set => _currSession1 = value; }

        public void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Allow user to drag the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        #region Temporary - Will be removed
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FocusStatus focusStatus = new FocusStatus();
            focusStatus.Show();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FocusBreakSequence focusBreakSequence = new FocusBreakSequence();
            focusBreakSequence.Show();
        }


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            FocusSessionComplete focusSessionComplete = new FocusSessionComplete();
            focusSessionComplete.Show();

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Focus2HourBreak focus2HourBreak = new Focus2HourBreak();
            focus2HourBreak.Show();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Focus2HourBreakUntilStop focus2HourBreakUntilStop = new Focus2HourBreakUntilStop();
            focus2HourBreakUntilStop.Show();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            FocusBreakEnd focusBreakEnd = new FocusBreakEnd();
            focusBreakEnd.Show();

            FocusReminder focusReminder = new FocusReminder();
            focusReminder.Show();
        }

        #endregion

        #region Info Text Visibility
        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            SetVisibility();
        }

        private void FocusChange(object sender, RoutedEventArgs e)
        {
            var control = (dynamic)sender;

            switch (control.Name.ToString())
            {
                case "chkDND":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (chkDND.IsFocused || (btnInfo.IsChecked ?? false))
                        txtInfo1.Visibility = Visibility.Visible;
                    else
                        txtInfo1.Visibility = Visibility.Hidden;
                    break;

                case "sPnlBreakSetting":
                case "chkProvide":
                case "cmbBreakTIme":
                case "cmbEvery":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (sPnlBreakSetting.IsKeyboardFocusWithin || (btnInfo.IsChecked ?? false))
                        txtInfo2.Visibility = Visibility.Visible;
                    else
                        txtInfo2.Visibility = Visibility.Hidden;
                    break;

                case "sPnlStartSession":
                case "btnSessionTillStop":
                case "btn30Min":
                case "btn1Hr":
                case "btn2Hrs":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (sPnlStartSession.IsKeyboardFocusWithin || btnSessionTillStop.IsFocused || (btnInfo.IsChecked ?? false))
                        txtInfo3.Visibility = Visibility.Visible;
                    else
                        txtInfo3.Visibility = Visibility.Hidden;
                    break;
                case "btnSettings":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (btnSettings.IsFocused || (btnInfo.IsChecked ?? false))
                        txtInfo4.Visibility = Visibility.Visible;
                    else
                        txtInfo4.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void SetVisibility()
        {
            bool showInfo = btnInfo.IsChecked ?? false;

            txtInfo1.Visibility = txtInfo2.Visibility = txtInfo3.Visibility = txtInfo4.Visibility = showInfo ? Visibility.Visible : Visibility.Hidden;
        }

        private void MouseOver(object sender, MouseEventArgs e)
        {
            var control = (dynamic)sender;

            switch (control.Name.ToString())
            {
                case "chkDND":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (chkDND.IsMouseOver || (btnInfo.IsChecked ?? false))
                        txtInfo1.Visibility = Visibility.Visible;
                    else
                        txtInfo1.Visibility = Visibility.Hidden;
                    break;

                case "sPnlBreakSetting":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (sPnlBreakSetting.IsMouseOver || (btnInfo.IsChecked ?? false))
                        txtInfo2.Visibility = Visibility.Visible;
                    else
                        txtInfo2.Visibility = Visibility.Hidden;
                    break;

                case "sPnlStartSession":
                case "btnSessionTillStop":
                    //Show DND Info if either i button is clicked or focus is on checkbos
                    if (sPnlStartSession.IsMouseOver || btnSessionTillStop.IsMouseOver || (btnInfo.IsChecked ?? false))
                        txtInfo3.Visibility = Visibility.Visible;
                    else
                        txtInfo3.Visibility = Visibility.Hidden;
                    break;
                case "btnSettings":
                    //Show DND Info if either i button is clicked or focus/hover is on checkbos
                    if (btnSettings.IsMouseOver || (btnInfo.IsChecked ?? false))
                        txtInfo4.Visibility = Visibility.Visible;
                    else
                        txtInfo4.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void chkBlockProgram_Click(object sender, RoutedEventArgs e)
        {
            bool showBlocklist = chkBlockProgram.IsChecked ?? false;

            sPnlBlockList.Visibility = showBlocklist ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion

        /// <summary>
        /// Navigate to Settings Screen with Edit Blocklists Menu Item Open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewEditBlockList_Click(object sender, RoutedEventArgs e)
        {
            if (scrSettings == null) scrSettings = new Settings(this);
            scrSettings.OpenBlocklist = true;
            this.Hide();
            scrSettings.Show();
        }

        /// <summary>
        /// Close this window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// Close current window and open Settings Screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (scrSettings == null) scrSettings = new Settings(this);
            this.Hide();
            scrSettings.Show();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            GetBlockLists();
        }

        private void FocusStart(object sender, RoutedEventArgs e)
        {
            try
            {
                //Create Session Object
                Session session = new Session()
                {
                    TurnONDND = chkDND.IsChecked ?? false,

                    ProvideBreak = chkProvide.IsChecked ?? false,
                    BreakDuration = int.Parse(((ComboBoxItem)cmbBreakTIme.SelectedItem).Tag.ToString()),
                    BreakGap = int.Parse(((ComboBoxItem)cmbEvery.SelectedItem).Tag.ToString()),

                    BlockListName = cmbBlockList.SelectedValue == null ? "" : cmbBlockList.SelectedValue.ToString(),

                    CreatedBy = Environment.UserName,
                    DateCreated = DateTime.Now,

                    ActualStartTime = DateTime.Now,

                    SessionDuration = int.Parse(((Button)sender).Tag.ToString())
                };

                //Add to database
                JSONHelper jSONHelper = new JSONHelper(Common.SESSION_FILE_NAME);
                string jsonString = jSONHelper.Save(session);

                if (IsFocusRunning)
                {
                    //Log Session
                    LoggingService.WriteToLog("Session Restarted : " + jsonString);
                }
                else
                {
                    //Log Session
                    LoggingService.WriteToLog("Session Started : " + jsonString);
                }

                CurrSession1 = session;
                IsFocusRunning = true;

                //Hide this dialog
                this.Hide();
            }
            catch (Exception ex)
            {
                LoggingService.WriteToLog(ex.Message + ex.StackTrace);
            }

        }
    }
}
