using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameArchives;
using GameArchives.STFS;
using DtxCS;
using DtxCS.DataTypes;
using LibForge;
using LibForge.Ark;
using LibForge.CSV;
using LibForge.Lipsync;
using LibForge.Mesh;
using LibForge.Midi;
using LibForge.Milo;
using LibForge.RBSong;
using LibForge.SongData;
using LibForge.Texture;
using LibForge.Util;

namespace ForgeToolGUI.Inspectors
{
  public partial class StartupInspector : Inspector
  {
    public StartupInspector()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      fb.OpenConverter();
    }

    private void openFileButton_Click(object sender, EventArgs e)
    {
      fb.openFile_Click(sender, e);
    }

    private void openPackageButton_Click(object sender, EventArgs e)
    {
      fb.openPackage_Click(sender, e);
    }

    private void openFolderButton_Click(object sender, EventArgs e)
    {
      fb.openFolder_Click(sender, e);
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
      var ocd = new OpenFileDialog();
      if (ocd.ShowDialog(this) == DialogResult.OK)
        {
          FolderBrowserDialog opd = new FolderBrowserDialog();
          if (opd.ShowDialog(this) == DialogResult.OK)
          {
          var rbvrPath = opd.SelectedPath;
          var conFile = STFSPackage.OpenFile(Util.LocalFile(ocd.FileName));
          var tempDir = Path.Combine(Path.GetTempPath(), "forgetool_tmp_build");
          var songs = PkgCreator.ConvertDLCPackage(conFile.RootDirectory.GetDirectory("songs"), false);
          var entitlementNames = new List<string>();
          foreach (DLCSong song in songs)
          {
            Directory.CreateDirectory(tempDir);
            var shortname = song.SongData.Shortname;
            var filePrefix = $"pc/songs_download/{shortname}/{shortname}";
            var arks = new List<List<Tuple<string, IFile, int>>>();
            arks.Add(new List<Tuple<string, IFile, int>>());

            // convert mogg.dta to dta_pc
            using (FileStream fs = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.mogg.dta_dta_pc")))
              DTX.ToDtb(song.MoggDta, fs, 3, false);
            arks[0].Add(Tuple.Create($"{filePrefix}.mogg.dta_dta_pc", Util.LocalFile($"{tempDir}/{shortname}.mogg.dta_dta_pc"), -1));
            // convert moggsong to dta_pc
            using (FileStream fs = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.moggsong_dta_pc")))
              DTX.ToDtb(song.MoggSong, fs, 3, false);
            arks[0].Add(Tuple.Create($"{filePrefix}.moggsong_dta_pc", Util.LocalFile($"{tempDir}/{shortname}.moggsong_dta_pc"), -1));
            // add empty vrevents to rbmid
            song.RBMidi.Format = RBMid.FORMAT_RBVR;
            song.RBMidi.VREvents = new RBMid.RBVREVENTS();
            using (var rbmid = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.rbmid_pc")))
              RBMidWriter.WriteStream(song.RBMidi, rbmid);
            arks[0].Add(Tuple.Create($"{filePrefix}.rbmid_pc", Util.LocalFile($"{tempDir}/{shortname}.rbmid_pc"), -1));
            // add rbsong
            using (var rbsongFile = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.rbsong")))
              new RBSongResourceWriter(rbsongFile).WriteStream(song.RBSong);
            arks[0].Add(Tuple.Create($"{filePrefix}.rbsong", Util.LocalFile($"{tempDir}/{shortname}.rbsong"), -1));
            // add songdta
            song.SongData.KeysRank = 0;
            song.SongData.RealKeysRank = 0;
            using (var songdtaFile = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.songdta_pc")))
              SongDataWriter.WriteStream(song.SongData, songdtaFile);
            arks[0].Add(Tuple.Create($"{filePrefix}.songdta_pc", Util.LocalFile($"{tempDir}/{shortname}.songdta_pc"), -1));
            // add album art (incompatible)
            if (song.SongData.AlbumArt)
            {
              using (var artFile = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.png_pc")))
                TextureWriter.WriteStream(song.Artwork, artFile);
              arks[0].Add(Tuple.Create($"{filePrefix}.png_pc", Util.LocalFile($"{tempDir}/{shortname}.png_pc"), -1));
            }
            // add mogg
            arks[0].Add(Tuple.Create($"{filePrefix}.mogg", song.Mogg, -1));

            var builder = new ArkBuilder($"{shortname}_pc", arks);
            Console.WriteLine($"Writing {shortname}_pc.hdr and ARK files to {rbvrPath}...");
            builder.Save(rbvrPath, 0x0);
            entitlementNames.Add($"DLC = song_{shortname}");
            Directory.Delete(tempDir, true);
          }
          Console.WriteLine("Conversion complete! Add the following DLC SKUs to your entitlement list:");
          foreach (string sku in entitlementNames)
          {
            File.WriteAllLines(rbvrPath + "/SKUs.txt", entitlementNames);
            Console.WriteLine(sku);
          }
          MessageBox.Show("Conversion Complete! Add the DLC SKUs in SKUs.txt to GammonConfig.ini");
        }
        }
    }

    private void button2_Click(object sender, EventArgs e)
    {
      fb.VRConverter();
    }
  }
}
