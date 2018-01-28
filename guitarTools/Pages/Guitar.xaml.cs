﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Linq;
using Controls;
using FretboardLibrary;
using ServicesLibrary;

namespace GuitarScales.Pages
{
  /// <summary>
  /// Interaction logic for Guitar.xaml
  /// </summary>
  public partial class Guitar : BasePage
  {
    #region Constructor

    /// <summary>
    /// </summary>
    public Guitar()
    {
      PageLoadAnimation = PageAnimation.SlideAndFadeInFromLeft;

      InitializeComponent();

      #region Defining variables

      ushort frets = 12 * 1;
      ushort strings = 6;
      Doc = new XDocument(XDocument.Load(Directory.GetCurrentDirectory() + @"\Data\Data.xml"));

      HiddenMenu = true;
      SettingsPanel = null;

      #endregion

      cbRoot = new RadialMenu(MusicKeys, 4);
      cbRoot.SelectionChanged += Root_SelectionChanged;

      ViewRoot.Child = cbRoot;

      // Creating default fretboard
      fretboard = new Fretboard(mainGrid, strings, frets, 4, tuning, scale);

      LabelTuning.Content = tuning;
      LabelScale.Content = scale;

      SetupControls(4, 0, strings);
      SetupSearchScale();
    }

    #endregion

    #region Properties

    private Fretboard fretboard;
    private readonly RadialMenu cbRoot;

    private XDocument Doc { get; }

    /// <summary>
    /// </summary>
    public bool HiddenMenu { get; set; }

    /// <summary>
    /// </summary>
    public StackPanel SettingsPanel { get; set; }

    /// <summary>
    /// </summary>
    public bool init;

    /// <summary>
    /// Array of music keys
    /// </summary>
    public string[] MusicKeys = { "C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B" };

    /// <summary>
    /// Array of comboboxes
    /// </summary>
    public ComboBox[,] Menu;

    /// <summary>
    /// </summary>
    public int Active = 2;

    private readonly string scale = "Ionian";
    private readonly string tuning = "Standard E";

    #endregion

    #region Setup Settings

    /// <summary>
    /// </summary>
    /// <param name="root"></param>
    /// <param name="scale"></param>
    /// <param name="strings"></param>
    private void SetupControls(int root, int scale, int strings)
    {
      // The root notes are constant - no need to fetch from database
      cbRoot.SelectedIndex = root; // Setting default root note to "E"
      LabelRoot.Content = MusicKeys[root];

      Dispatcher.Invoke(() => { FillTunings(strings); });
      Dispatcher.Invoke(() => { FillScales(scale); });
    }

    /// <summary>
    /// </summary>
    /// <param name="strings"></param>
    private void FillTunings(int strings)
    {
      if (cbTuning.HasItems) cbTuning.Items.Clear();

      // Adding tunings from database
      var tunings = from node in Doc.Descendants("Tunings").Elements("Tuning")
                    where node.Attribute("strings").Value == strings.ToString()
                    select node.Element("Name").Value;

      foreach (var item in tunings) cbTuning.Items.Add(item);
      cbTuning.SelectedIndex = 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="scale"></param>
    private void FillScales(int scale)
    {
      // Adding scales from database
      var scales = from node in Doc.Descendants("Scales").Elements("Scale")
                   select node.Element("Name").Value;

      foreach (var item in scales)
        cbScale.Items.Add(item);
      cbScale.SelectedIndex = scale;
    }

    /// <summary>
    /// </summary>
    private void SetupSearchScale()
    {
      var chords = from node in Doc.Descendants("Chords").Elements("Chord")
                   select node.Element("Name").Value;

      Dispatcher.Invoke(() =>
      {
        foreach (var item in MusicKeys)
        {
          tbOne.Items.Add(item);
          tbTwo.Items.Add(item);
          tbThree.Items.Add(item);
        }
      }, DispatcherPriority.ContextIdle);

      Dispatcher.Invoke(() =>
      {
        foreach (var item in chords)
        {
          cbOne.Items.Add(item);
          cbTwo.Items.Add(item);
          cbThree.Items.Add(item);
        }
      }, DispatcherPriority.ContextIdle);

      Menu = new[,] { { tbOne, tbTwo, tbThree }, { cbOne, cbTwo, cbThree } };
      init = true;

      SearchScaleNoteMenu.Children.Add(new Label { Content = "Note 1", FontSize = 18 });
      SearchScaleNoteMenu.Children.Add(new Viewbox { Child = new RadialMenu(MusicKeys, 0), Width = 180 });
      SearchScaleNoteMenu.Children.Add(new Label { Content = "Note 2", FontSize = 18 });
      SearchScaleNoteMenu.Children.Add(new Viewbox { Child = new RadialMenu(MusicKeys, 0), Width = 180 });
      SearchScaleNoteMenu.Children.Add(new Label { Content = "Note 3", FontSize = 18 });
      SearchScaleNoteMenu.Children.Add(new Viewbox { Child = new RadialMenu(MusicKeys, 0), Width = 180 });
    }

    #endregion

    #region Controls

    /// <summary>
    /// </summary>
    public void Root_SelectionChanged(object sender, EventArgs e)
    {
      fretboard.UpdateRoot(cbRoot.SelectedIndex);
      LabelRoot.Content = MusicKeys[cbRoot.SelectedIndex];
    }

    /// <summary>
    /// </summary>
    private void cbTuning_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      try
      {
        if (fretboard.Tuning != cbTuning.SelectedValue.ToString())
        {
          LabelTuning.Content = cbTuning.SelectedValue.ToString();
          fretboard.UpdateTuning(cbTuning.SelectedValue.ToString());
        }
      }
      catch
      {
      }
    }

    /// <summary>
    /// </summary>
    public void cbScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (fretboard.Scale != cbScale.SelectedValue.ToString())
      {
        LabelScale.Content = cbScale.SelectedValue.ToString();
        fretboard.UpdateScale(cbScale.SelectedValue.ToString());
      }
    }

    /// <summary>
    /// </summary>
    private void FretboardApply_Click(object sender, RoutedEventArgs e)
    {
      if (SliderFrets.Value != fretboard.Frets || SliderStrings.Value != fretboard.Strings)
      {
        int oldStrings = fretboard.Strings;
        fretboard.ClearFretboard();

        if (SliderStrings.Value != oldStrings)
        {
          var t = (from node in Doc.Descendants("Tunings").Elements("Tuning")
                   where node.Attribute("strings").Value == SliderStrings.Value.ToString()
                   select node.Element("Name").Value).First();

          fretboard = new Fretboard(mainGrid,
              (ushort)SliderStrings.Value,
              (ushort)SliderFrets.Value,
              4,
              t,
              cbScale.SelectedValue.ToString());

          LabelTuning.Content = t;
          LabelScale.Content = cbScale.SelectedValue.ToString();

          FillTunings((int)SliderStrings.Value); // Causes error
        }
        else
        {
          fretboard = new Fretboard(mainGrid,
              (ushort)SliderStrings.Value,
              (ushort)SliderFrets.Value,
              cbRoot.SelectedIndex,
              (from node in Doc.Descendants("Tunings").Elements("Tuning")
               where node.Attribute("strings").Value == SliderStrings.Value.ToString() &&
                               node.Element("Name").Value == cbTuning.SelectedValue.ToString()
               select node.Element("Name").Value).Single(),
              cbScale.SelectedValue.ToString());
        }
      }
    }

    /// <summary>
    /// </summary>
    private void Menu_Click(object sender, RoutedEventArgs e)
    {
      var sb = HiddenMenu ? Resources["sbShowLeftMenu"] as Storyboard : Resources["sbHideLeftMenu"] as Storyboard;

      sb.Begin(pnlLeftMenu);
      HiddenMenu = !HiddenMenu;
    }

    #endregion

    #region Settings Panels

    /// <summary>
    /// </summary>
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
      var sb = Resources["sbShowSetting"] as Storyboard;
      var panelName = (sender as Button).Name.Replace("btn", "");
      SettingsPanel = (StackPanel)FindName(panelName);
      sb.Begin(SettingsPanel);
    }

    /// <summary>
    /// </summary>
    private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (SettingsPanel != null && !SettingsPanel.IsMouseOver)
      {
        var sb = Resources["sbHideSetting"] as Storyboard;
        sb.Begin(SettingsPanel);
        SettingsPanel = null;
      }
    }

    #endregion

    #region Search Scale

    /// <summary>
    /// </summary>
    private void btnSearch_Click(object sender, RoutedEventArgs e)
    {
      lbResults.Items.Clear();
      var chordNotes = new List<int>();
      var pointA = Array.IndexOf(MusicKeys, tbOne.SelectedItem);
      var sem = new Semaphore(1, Environment.ProcessorCount);

      for (var row = 0; row < Menu.GetLength(1) - Active; row++)
        Dispatcher.Invoke(() =>
        {
          sem.WaitOne();
          var chord = (from node in Doc.Descendants("Chords").Elements("Chord")
                       where node.Element("Name").Value == (string)Menu[1, row].SelectedValue
                       select node.Element("Interval").Value).Single().Split(' ');

          var pointB = Array.IndexOf(MusicKeys, Menu[1, row]);
          if (pointA - pointB != 0)
          {
            var shiftBy = new IntLimited(pointA + pointB, 0, 12);
          }
          foreach (var item in chord)
            if (!chordNotes.Contains(int.Parse(item)))
              chordNotes.Add(int.Parse(item));
          sem.Release();
        }, DispatcherPriority.ContextIdle);

      chordNotes.Sort();

      var scales = from node in Doc.Descendants("Scales").Elements("Scale")
                   select node.Element("Interval").Value;

      var found = new List<string>();
      foreach (var item in scales)
        new Thread(() =>
        {
          sem.WaitOne();
          for (var note = 0; note < chordNotes.Count; note++)
            if (item.IndexOf(note.ToString()) != -1)
            {
              if (note == chordNotes.Count - 1)
                found.Add(item);
            }
            else
            {
              break;
            }
          sem.Release();
        }).Start();

      if (found.Count > 0)
        foreach (var item in found)
          lbResults.Items.Add(
              (from node in Doc.Descendants("Scales").Elements("Scale")
               where node.Element("Interval").Value == item
               select node.Element("Name").Value).Single()
          );
      else lbResults.Items.Add("Unknown scale");
    }

    /// <summary>
    /// </summary>
    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (init)
      {
        btnSearch.IsEnabled = false;
        if (tbOne.SelectedIndex != 0)
        {
          cbOne.IsEnabled = true;
          if (cbOne.SelectedIndex != 0)
          {
            btnSearch.IsEnabled = true;
            tbTwo.IsEnabled = true;
            Active = 2;
            if (tbTwo.SelectedIndex != 0)
            {
              btnSearch.IsEnabled = false;
              cbTwo.IsEnabled = true;
              if (cbTwo.SelectedIndex != 0)
              {
                btnSearch.IsEnabled = true;
                tbThree.IsEnabled = true;
                Active = 1;
                if (tbThree.SelectedIndex != 0)
                {
                  btnSearch.IsEnabled = false;
                  cbThree.IsEnabled = true;
                  if (cbThree.SelectedIndex != 0)
                  {
                    btnSearch.IsEnabled = true;
                    Active = 0;
                  }
                  else
                  {
                    btnSearch.IsEnabled = false;
                  }
                }
                else
                {
                  btnSearch.IsEnabled = true;
                  cbThree.IsEnabled = false;
                }
              }
              else
              {
                btnSearch.IsEnabled = false;
                tbThree.IsEnabled = false;
                cbThree.IsEnabled = false;
              }
            }
            else
            {
              btnSearch.IsEnabled = true;
              cbTwo.IsEnabled = false;
              tbThree.IsEnabled = false;
              cbThree.IsEnabled = false;
            }
          }
          else
          {
            btnSearch.IsEnabled = false;
            tbTwo.IsEnabled = false;
            cbTwo.IsEnabled = false;
            tbThree.IsEnabled = false;
            cbThree.IsEnabled = false;
          }
        }
        else
        {
          cbOne.IsEnabled = false;
          tbTwo.IsEnabled = false;
          cbTwo.IsEnabled = false;
          tbThree.IsEnabled = false;
          cbThree.IsEnabled = false;
        }
      }
    }

    /// <summary>
    /// </summary>
    private void lbResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      cbScale.SelectedValue = lbResults.SelectedValue;
      cbRoot.SelectedIndex = Array.IndexOf(MusicKeys, tbOne.SelectedValue);
      Root_SelectionChanged(cbRoot, null);
      cbScale_SelectionChanged(cbScale, null);
    }

    #endregion
  }
}