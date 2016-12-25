﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using guitarTools.Classes.Fretboard;
using guitarTools.Classes;

namespace guitarTools
{
    /// <summary>
    /// Contains the window logic.
    /// Creates UI elements with default settings upon initiation.
    /// </summary>

    public partial class MainWindow : Window
    {
        Fretboard fretboard;

        public MainWindow()
        {
            InitializeComponent();

            #region Defining variables
            ushort frets = 12*2;
            ushort strings = 7;
            List<List<FretNote>> NoteList = new List<List<FretNote>>(); // Row is a string, column is a fret
            #endregion

            // Creating default fretboard
            fretboard = new Fretboard(mainGrid, noteGrid, strings, frets, NoteList, 4);
            cbRoot.SelectedIndex = 4;

            foreach (var item in SQLCommands.FetchList<string>("SELECT Name FROM tableScales"))
                cbScale.Items.Add(item);
        }

        private void cbRoot_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (fretboard.Root != cbRoot.SelectedIndex)
                fretboard.UpdateRoot(cbRoot.SelectedIndex);
        }

        private void WindowTop_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    Top = 0;
                    Left = Mouse.GetPosition(this).X - ActualWidth / 2;
                }

                DragMove();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            //WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Minimized;
        }

        private void mainWindow_StateChanged(object sender, System.EventArgs e)
        {
            //if (WindowState == WindowState.Normal)
            //{
            //    DoubleAnimation da = new DoubleAnimation();
            //    da.From = 1;
            //    da.To = 0;
            //    da.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            //    MainWindow.OpacityProperty.Be
            //}
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            //change the WindowStyle back to None, but only after the Window has been activated
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => WindowStyle = WindowStyle.None));
        }
    }
}