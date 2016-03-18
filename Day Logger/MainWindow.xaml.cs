﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Day_Logger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Initializers
        /// <summary>
        /// Default initializer for the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeWindow();
            InitializeConfig();
        }

        /// <summary>
        /// Initializer for opening a new file with a given filepath.
        /// </summary>
        /// <param name="filePath">The file path to get the information from.</param>
        public MainWindow(string filePath)
        {
            // Initialized the window.
            InitializeWindow();
            InitializeConfig();

            // Set the window's file path.
            FilePath = filePath;

            // Get the stream to initialize the window with.
            StreamReader sRead = new StreamReader(filePath);

            // Set the filename to be the name of the file from the given path.
            this.FileName = System.IO.Path.GetFileName(filePath);

            // Position the reader to the second line, as the first is only for the user.
            sRead.ReadLine();

            // Set the timestamps in the DataGrid with the information from the file.
            while (!sRead.EndOfStream)
            {
                string input = sRead.ReadLine();

                string[] res = input.Split(',');

                (Resources["StmpDs"] as TimeStampCollection).AddStamp(new TimeStamp()
                {
                    STime = res[0],
                    ETime = res[1],
                    Status = res[3],
                    Description = res[4]
                });
            }

            // Set txtStartTime to be the end time of the last timestamp.
            int itemNo = (Resources["StmpDs"] as TimeStampCollection).Count - 1;

            if (itemNo >= 0)
                txtStartTime.Text = (dgStamps.Items[itemNo] as TimeStamp).ETime;

            this.Title = FileName + " - Day Logger";
        }

        /// <summary>
        /// This method is used to initialize the window.
        /// </summary>
        private void InitializeWindow()
        {
            InitializeComponent();
            changeHandler = new DataGridChangeHandler();

            txtEndTime.Text = DateTime.Now.ToString("HH:mm");

            DispatcherTimer dispTimer = new DispatcherTimer();
            dispTimer.Tick += new EventHandler(dispTimer_Tick);
            dispTimer.Interval = new TimeSpan(0, 0, 10);
            dispTimer.Start();

            AddHandler(Keyboard.KeyDownEvent, (System.Windows.Input.KeyEventHandler)HandleKeyEvent);

            this.dgStamps.PreviewMouseLeftButtonDown +=
                new MouseButtonEventHandler(dgStamps_PreviewMouseLeftButtonDown);
            this.dgStamps.Drop += new DragEventHandler(dgStamps_Drop);
            this.dgStamps.CellEditEnding += dgStamps_CellEditEnding;

            changeHandler.Changed += new DataGridChangeHandler.ChangedEventHandler(HandleChangedEvent);
        }
        #endregion
        #region Window Functions
        /// <summary>
        /// This function is used to initialize the combo boxes from the applicaton
        ///     configuration file.
        /// </summary>
        private void InitializeConfig()
        {
            foreach (string s in ConfigOperations.GetStatusConfig())
                cStatusBox.Items.Add(s);

            foreach (string s in ConfigOperations.GetCallTypeConfig())
                cCallTypeBox.Items.Add(s);

            foreach (string s in ConfigOperations.GetCustomerTypeConfig())
                cCustomerTypeBox.Items.Add(s);

            cStatusBox.Items.Add("");
            cCallTypeBox.Items.Add("");
            cCustomerTypeBox.Items.Add("");
        }

        private void HandleChangedEvent(object sender, RoutedEventArgs e)
        {
            // Make sure the asterisk was not already added for the change.
            if (!this.Title.EndsWith("*"))
                this.Title += "*";

            this.changed = true;
        }
        #endregion
        #region DataGrid Events
        /// <summary>
        /// This function responds to the CellEditEnding event to update the DataGrid columns.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The cell that was changed.</param>
        private void dgStamps_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var classInstance = e.EditingElement.DataContext;
            string newValue = (e.EditingElement as TextBox).Text;

            TimeStamp stamp = (e.Row.Item as TimeStamp);

            switch(e.Column.Header.ToString())
            {
                case "Start Time":
                    stamp.STime = newValue;
                    break;
                case "End Time":
                    stamp.ETime = newValue;
                    break;
                case "Status":
                    stamp.Status = newValue;
                    break;
                case "Description":
                    stamp.Description = newValue;
                    break;
            }
        }
        #endregion
        #region Button Events
        /// <summary>
        /// Handles the event for adding timestamps to the ListView.
        /// </summary>
        /// <param name="sender">The Event sender</param>
        /// <param name="e">The RoutedEventArgs</param>
        private void btnAddStamp_Click(object sender, RoutedEventArgs e)
        {
            TimeStampCollection tS = (Resources["StmpDs"] as TimeStampCollection);

            TimeStamp addition = new TimeStamp();

            addition.STime = txtStartTime.Text;
            addition.ETime = txtEndTime.Text;
            addition.Status = cStatusBox.SelectionBoxItem.ToString();
            addition.Description = TimestampFunctions.GetDesString(cCallTypeBox.SelectionBoxItem.ToString(), 
                                                                   cCustomerTypeBox.SelectionBoxItem.ToString(),
                                                                   txtReferenceNumber.Text);

            (Resources["StmpDs"] as TimeStampCollection).AddStamp(addition);

            txtStartTime.Text = txtEndTime.Text;

            TimeStampCollection stamps = (Resources["StmpDs"] as TimeStampCollection);
            changeHandler.AddChange(() => { stamps.RemoveAt(stamps.Count - 1); },
                                    () => { stamps.Insert(stamps.Count, addition); });
        }

        /// <summary>
        /// Handles the event for removing timestamps from the ListView.
        /// </summary>
        /// <param name="sender">The Event sender</param>
        /// <param name="e">The RoutedEventArgs</param>
        private void btnRemoveStamp_Click(object sender, RoutedEventArgs e)
        {
            List<TimeStamp> rList = new List<TimeStamp>();

            try
            {
                foreach (TimeStamp tStamp in dgStamps.SelectedItems)
                {
                    rList.Add(tStamp);
                }
            }
            catch
            {
                MessageBox.Show("Cannot delete that item", "Error");
            }

            for (int i = 0; i < rList.Count; ++i)
                (Resources["StmpDs"] as TimeStampCollection).Remove(rList[i]);
        }
        #endregion
        #region Menu Events
        /// <summary>
        /// Handle the OnNew event for creating a new document.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The RoutedEventArgs</param>
        private void OnNew_Click(object sender, RoutedEventArgs e)
        {
            if (changed == true)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Would you like to save the current changes?", "Confirm Changes",
                                MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                    OnSave_Click(this, new RoutedEventArgs());
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            FilePath = String.Empty;

            List<TimeStamp> rList = new List<TimeStamp>();

            try
            {
                foreach (TimeStamp tStamp in dgStamps.Items)
                {
                    rList.Add(tStamp);
                }
            }
            catch { }

            for (int i = 0; i < rList.Count; ++i)
                dgStamps.Items.Remove(rList[i]);

            txtStartTime.Text = String.Empty;
            changed = false;
            this.Title = "New Document - Day Logger";
            this.FileName = "New Document";
        }

        /// <summary>
        /// Handle the OnOpen event for saving the document.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The RoutedEventArgs</param>
        private void OnOpen_Click(object sender, RoutedEventArgs e)
        {
            // Get the file path for the file to open.
            string filePath = TimestampFunctions.OpenFile();

            // Make sure that a file was selected.
            if (filePath != String.Empty)
            {
                // Create the new window with the StreamReader as the input.
                MainWindow mWin = new MainWindow(filePath);

                // Show the new window.
                mWin.Show();
                this.Close();
            }
        }

        /// <summary>
        /// Handle the OnSave event for saving the document.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The RoutedEventArgs</param>
        private void OnSave_Click(object sender, RoutedEventArgs e)
        {
            // Create the string for the file, starting with the header information.
            StringBuilder sString = new StringBuilder("Start Time, End Time, Duration, Status\r\n");

            // Loop through all of the timestamps in the DataGrid.
            for (int i = 0; i < dgStamps.Items.Count - 1; ++i )
            {
                TimeStamp tStamp = (dgStamps.Items[i] as TimeStamp);

                // Add the TimeStamp information to the file string.
                sString.AppendLine(tStamp.STime + "," + tStamp.ETime + "," + tStamp.Duration + "," + tStamp.Status + "," + tStamp.Description);
            }

            // Check to see if FilePath has been determined previously.
            if (FilePath == null || FilePath == String.Empty)
            {
                // Create FilePath if it was not aready determined.
                FilePath = TimestampFunctions.GetSaveFile();
            }

            // Check FilePath to see if the user hit "cancel"
            if (FilePath != String.Empty)
            {
                // Write all of the information to the file.
                File.WriteAllText(FilePath, sString.ToString());
                changed = false;

                // Set the FileName and the window title.
                this.FileName = System.IO.Path.GetFileName(FilePath);
                this.Title = FileName + " - Day Logger";
            }
        }

        /// <summary>
        /// Handle the Save As event for saving the document.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The RoutedEventArgs</param>
        private void OnSaveAs_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Handle the OnExit event for closing the document.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The RoutedEventArgs.</param>
        private void OnExit_Click(object sender, RoutedEventArgs e)
        {
            if(changed == true)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to save your file before closing?", "Confirm Changes",
                                MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                    OnSave_Click(this, new RoutedEventArgs());
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            Application.Current.Shutdown();
        }

        private void OnUndo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OnRedo_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion
        #region Key Events
        /// <summary>
        /// Handles the keyboard macros for interacting with the main window.
        /// </summary>
        /// <param name="sender">The Event sender</param>
        /// <param name="e">The KeyEventArgs for key input</param>
        private void HandleKeyEvent(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.S:
                    if (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
                        OnSave_Click(this, new RoutedEventArgs());
                    break;
                case Key.Y:
                    if (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
                        changeHandler.Redo();
                    break;
                case Key.Z:
                    if (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
                        changeHandler.Undo();
                    break;
                case Key.Delete:
                    btnRemoveStamp_Click(this, new RoutedEventArgs());
                    break;
            }
        }
        #endregion
        #region Timer Events
        /// <summary>
        /// The event handler for the tick event, which goes over once a minute.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispTimer_Tick(object sender, EventArgs e)
        {
            // Update the end time textbox with the current time.
            txtEndTime.Text = DateTime.Now.ToString("HH:mm");

            // Force the CommandManager to raise the RequerySuggest event.
            CommandManager.InvalidateRequerySuggested();
        }
        #endregion
        #region Drag And Drop
        /// <summary>
        /// This function is used to verify that the mouse is on the
        ///     target row that will be moved.
        /// </summary>
        /// <param name="target">The Visual target row.</param>
        /// <param name="pos">The DragDropPosition of the mouse.</param>
        /// <returns></returns>
        private bool IsMouseOnTargetRow(Visual target, GetDragDropPosition pos)
        {
            if (target == null)
                return false;

            Rect posBounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = pos((IInputElement)target);
            return posBounds.Contains(mousePos);
        }

        /// <summary>
        /// This function is used to get the DataGridRow that will be
        ///     moved.
        /// </summary>
        /// <param name="index">The index for the row.</param>
        /// <returns type="DataGridRow">The DataGridRow if one is found, otherwise NULL</returns>
        private DataGridRow GetDataGridRowItem(int index)
        {
            if (dgStamps.ItemContainerGenerator.Status
                != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return dgStamps.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        }

        /// <summary>
        /// This function is used to get the current row index
        ///     for the drag and drop position when the user hovers over one.
        /// </summary>
        /// <param name="pos">The DragDropPosition.</param>
        /// <returns type="int">The index for the current row item, -1 if no row is hovered over.</returns>
        private int GetDataGridItemCurrentRowIndex(GetDragDropPosition pos)
        {
            int curIndex = -1;
            for (int i = 0; i < dgStamps.Items.Count; i++)
            {
                DataGridRow itm = GetDataGridRowItem(i);
                if (IsMouseOnTargetRow(itm, pos))
                {
                    curIndex = i;
                    break;
                }
            }
            return curIndex;
        }

        /// <summary>
        /// This function is used to get the position of the mouse when hovering with a
        ///     row to move.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The MouseButtonEventArgs.</param>
        private void dgStamps_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            prevRowIndex = GetDataGridItemCurrentRowIndex(e.GetPosition);

            if (prevRowIndex < 0)
                return;

            dgStamps.SelectedIndex = prevRowIndex;

            TimeStamp selectedStamp = dgStamps.Items[prevRowIndex] as TimeStamp;

            if (selectedStamp == null)
                return;

            DragDropEffects dragDropEffects = DragDropEffects.Move;

            if(DragDrop.DoDragDrop(dgStamps, selectedStamp, dragDropEffects) != DragDropEffects.None)
            {
                dgStamps.SelectedItem = selectedStamp;
            }
        }

        /// <summary>
        /// This function is used to move a timestamp in the DataGrid
        ///     when the user moves it to another row.
        /// </summary>
        /// <param name="sender">The Event sender.</param>
        /// <param name="e">The DragEventArgs.</param>
        private void dgStamps_Drop(object sender, DragEventArgs e)
        {
            if (prevRowIndex < 0)
                return;

            int index = this.GetDataGridItemCurrentRowIndex(e.GetPosition);

            if (index < 0 || index == prevRowIndex)
                return;

            if (index == dgStamps.Items.Count - 1)
            {
                MessageBox.Show("This row-index cannot be used for Drop Operations");
                return;
            }

            TimeStampCollection stamp = Resources["StmpDs"] as TimeStampCollection;

            TimeStamp movedStmp = stamp[prevRowIndex];
            stamp.RemoveAt(prevRowIndex);
            stamp.Insert(index, movedStmp);
        }
        #endregion
        #region Delegates
        public delegate Point GetDragDropPosition(IInputElement element);
        #endregion
        #region Instance Fields
        private string FilePath;
        private string FileName;
        private bool changed = false;
        private int prevRowIndex;
        private DataGridChangeHandler changeHandler;
        #endregion
    }
}