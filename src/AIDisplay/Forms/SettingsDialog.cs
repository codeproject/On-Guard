﻿using SAAI.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SAAI
{

  /// <summary>
  /// SettingsDialog allows the user to enter/edit the application wide settings.
  /// This includes the ip address/port of the AI.
  /// </summary>
  public partial class SettingsDialog : Form
  {
    public SettingsDialog()
    {
      InitializeComponent();


      aiLocationListView.Sorting = SortOrder.Ascending;

      // The older (pre 1-6-1) version may have used the old registry format.
      // If so, get it, but delete it.
      string oldIPAddress = Storage.GetGlobalString("DeepStackIPAddress");
      if (!string.IsNullOrEmpty(oldIPAddress))
      {
        int aiPort = Storage.GetGlobalInt("DeepStackPort");
        AILocation location = new AILocation(Guid.NewGuid(), oldIPAddress, aiPort);
        AILocation.AILocations.Add(location); // put it in the new format
        Storage.RemoveGlobalValue("DeepStackIPAddress");  // get rid of the old format
        Storage.RemoveGlobalValue("DeepStackPort");
      }

      foreach (var location in AILocation.AILocations)
      {
        ListViewItem item = new ListViewItem(new string[] { location.IPAddress, location.Port.ToString() });
        aiLocationListView.Items.Add(item);
        item.Tag = location;
      }

      double snapshot = Storage.GetGlobalDouble("FrameInterval");
      if (snapshot == 0.0)
      {
        snapshot = (double)Settings.Default.TimePerFrame;
      }
      snapshotNumeric.Value = (decimal)snapshot;

      int maxEvent = Storage.GetGlobalInt("MaxEventTime");
      if (maxEvent == 0)
      {
        maxEvent = Settings.Default.MaxEventTime;
      }
      maxEventNumeric.Value = maxEvent;

      int eventInterval = Storage.GetGlobalInt("EventInterval");
      if (eventInterval == 0)
      {
        eventInterval = Settings.Default.EventInterval;
      }
      eventIntervalNumeric.Value = eventInterval;
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
      Storage.SetGlobalDouble("FrameInterval", (double)snapshotNumeric.Value);
      Storage.SetGlobalInt("MaxEventTime", (int)maxEventNumeric.Value);
      Storage.SetGlobalInt("EventInterval", (int)eventIntervalNumeric.Value);
      DialogResult = DialogResult.OK;

    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.Cancel;

    }


    private void AddButton_Click(object sender, EventArgs e)
    {
      using (AILocationDialog dlg = new AILocationDialog())
      {
        DialogResult result = dlg.ShowDialog();
        if (result == DialogResult.OK)
        {
          ListViewItem item = new ListViewItem(new string[] { dlg.Location.IPAddress, dlg.Location.Port.ToString() });
          item.Tag = dlg.Location;
          aiLocationListView.Items.Add(item);
        }
      }

      DialogResult = DialogResult.None;
    }

    private void RemoveButton_Click(object sender, EventArgs e)
    {
      if (aiLocationListView.SelectedItems.Count > 0)
      {
        int index = aiLocationListView.SelectedIndices[0];
        AILocation location = (AILocation) aiLocationListView.Items[index].Tag;
        Storage.RemoveAILocation(location.ID.ToString());
        AILocation.Refresh();
        aiLocationListView.Items.RemoveAt(index);
      }
    }

    private void OnActivate(object sender, EventArgs e)
    {
      if (aiLocationListView.SelectedItems.Count > 0)
      {
        int index = aiLocationListView.SelectedIndices[0];
        ListViewItem item = aiLocationListView.Items[index];
        AILocation location = (AILocation)item.Tag;
        using (AILocationDialog dlg = new AILocationDialog(location))
        {
          DialogResult result = dlg.ShowDialog();
          if (result == DialogResult.OK)
          {
            aiLocationListView.Items[index].SubItems[0].Text = dlg.Location.IPAddress;
            aiLocationListView.Items[index].SubItems[1].Text = dlg.Location.Port.ToString();
          }
        }
      }

    }
  }
}
