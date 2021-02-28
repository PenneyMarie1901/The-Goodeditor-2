﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using HiHoFile;
using static HiHoFile.Extensions;

namespace TheGoodEditor2
{
    public partial class MainWindow : Form
    {
        
        public MainWindow()

        {
            InitializeComponent();
        }

        public void label6_Click(object sender, EventArgs e)
        {

        }
        private void FillLayerListBox()
        {
            label4.Text = "File: " + fileName;
            listBoxLayers.Items.Clear();
            foreach (var layer in hoFile.MAST.sectionSect2.layers)
                listBoxLayers.Items.Add(layer.layerType.Replace("\0", ""));
        }
        private void listBoxLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBoxAssets.BeginUpdate();
            listBoxAssets.Items.Clear();
            if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
            {
                if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                {
                    foreach (var asset in psl.assets)
                    {
                        string type;
                        if (Enum.IsDefined(typeof(AssetTypeHashed), asset.assetType))
                            type = ((AssetTypeHashed)asset.assetType).ToString();
                        else
                            type = asset.assetType.ToString("X8");

                        string name = $"[{type}] [{asset.assetID.ToString("X16")}]";
                        listBoxAssets.Items.Add(name);
                    }
                }
            }
            listBoxAssets.EndUpdate();
            listBoxAssets_SelectedIndexChanged(sender, e);
        }
        HoFile hoFile;
        string fileName;
        byte[] editableHoFile;
        public void loadHoParcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFile = new OpenFileDialog())
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    editableHoFile = File.ReadAllBytes(openFile.FileName);
                    hoFile = new HoFile(openFile.FileName);
                    fileName = openFile.FileName;

                    FillLayerListBox();
                }
        }
        public ulong GetSelectedAssetID()
        {
            if (listBoxAssets.SelectedItem != null)
                return Convert.ToUInt64(listBoxAssets.SelectedItem.ToString().Substring(listBoxAssets.SelectedItem.ToString().LastIndexOf("[") + 1, 16), 16);

            return 0;
        }
        public void ReplaceInEditableArray(int offset, byte[] file, int maxSize)
        {
            for (int i = 0; i < file.Length; i++)
                editableHoFile[offset + i] = file[i];
            for (int i = file.Length; i < maxSize; i++)
                editableHoFile[offset + i] = 0x33;
        }

        public void WriteNewSizeInEditableArray(int offset, int length)
        {
            byte[] bytes = BitConverter.GetBytes(length.Switch());
            for (int i = 0; i < bytes.Length; i++)
                editableHoFile[offset + i] = bytes[i];
        }

        public void listBoxAssets_SelectedIndexChanged(object sender, EventArgs e)
        {
            label6.Text = "";
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                            label6.Text = "Size: " + psl.assets[listBoxAssets.SelectedIndex].actualSize.ToString() + "\nMax Size: " + psl.assets[listBoxAssets.SelectedIndex].totalDataSize.ToString();
        }
        public void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            dontSwitch = checkBox1.Checked;
        }

        public void buttonExtract_Click(object sender, EventArgs e)
        {
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                            using (SaveFileDialog saveFile = new SaveFileDialog()
                            {
                                FileName = psl.assets[listBoxAssets.SelectedIndex].assetID.ToString("X16")
                            })
                                if (saveFile.ShowDialog() == DialogResult.OK)
                                    File.WriteAllBytes(saveFile.FileName, psl.assets[listBoxAssets.SelectedIndex].data);
        }

        public void buttonReplace_Click(object sender, EventArgs e)
        {
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                            using (OpenFileDialog openFile = new OpenFileDialog()
                            {
                                Title = "Choose a file to replace."
                            })
                                if (openFile.ShowDialog() == DialogResult.OK)
                                {
                                    byte[] file = File.ReadAllBytes(openFile.FileName);
                                    if (file.Length <= psl.assets[listBoxAssets.SelectedIndex].totalDataSize)
                                    {
                                        psl.assets[listBoxAssets.SelectedIndex].data = file;
                                        ReplaceInEditableArray(psl.assets[listBoxAssets.SelectedIndex].absoluteDataOffset, file, psl.assets[listBoxAssets.SelectedIndex].totalDataSize);

                                        psl.assets[listBoxAssets.SelectedIndex].actualSize = file.Length;
                                        WriteNewSizeInEditableArray(psl.assets[listBoxAssets.SelectedIndex].absoluteActualSizeOffset, file.Length);

                                        listBoxAssets_SelectedIndexChanged(sender, e);
                                    }
                                    else
                                        MessageBox.Show($"Please choose a file with at most the maximum size of the asset you want to replace.\n" +
                                            $"Maximum asset size: {psl.assets[listBoxAssets.SelectedIndex].totalDataSize} bytes\n" +
                                            $"Size of your file: {file.Length} bytes");
                                }
        }

        public void buttonExtractAll_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                    hoFile.ExtractAssetsToFolders(folderBrowser.SelectedPath, true);
        }
        private void saveHoParcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            File.WriteAllBytes(fileName, editableHoFile);
        }

        public void listBoxAssets_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        public void exitTheGoodEditor2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        byte[] x;
        private void button2_Click(object sender, EventArgs e)
        {
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                        {

                            x = psl.assets[listBoxAssets.SelectedIndex].data;
                            byte[] PlaceableData = x;

                            if (BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(x, 0x2C, 4);
                                Array.Reverse(x, 0x30, 4);
                                Array.Reverse(x, 0x34, 4);

                                Array.Reverse(x, 0x20, 4);
                                Array.Reverse(x, 0x24, 4);
                                Array.Reverse(x, 0x28, 4);

                                Array.Reverse(x, 0x38, 4);
                                Array.Reverse(x, 0x3C, 4);
                                Array.Reverse(x, 0x40, 4);
                                Array.Reverse(x, 0x10, 0);
                                Array.Reverse(x, 0x13, 0);

                            }
                            float myFloat = System.BitConverter.ToSingle(PlaceableData, 0x2C); //Read Position X's Float
                            float myFloat2 = System.BitConverter.ToSingle(PlaceableData, 0x30); //Read Position Y's Float
                            float myFloat3 = System.BitConverter.ToSingle(PlaceableData, 0x34); //Read Position Z's Float

                            float myFloat4 = System.BitConverter.ToSingle(PlaceableData, 0x20); //Read Roation X's Float
                            float myFloat5 = System.BitConverter.ToSingle(PlaceableData, 0x24); //Read Rotation Y's Float
                            float myFloat6 = System.BitConverter.ToSingle(PlaceableData, 0x28); //Read Rotation Z's Float

                            float myFloat7 = System.BitConverter.ToSingle(PlaceableData, 0x38); //Read Scale X's Float
                            float myFloat8 = System.BitConverter.ToSingle(PlaceableData, 0x3C); //Read Scale Y's Float
                            float myFloat9 = System.BitConverter.ToSingle(PlaceableData, 0x40); //ReadScale Z's Float
                            int FlagVisible = PlaceableData[0x10]; // Read the visibility flag of a SIMP (not PLAT)
                            int FlagColl = PlaceableData[0x13]; // Read the collision flag of a SIMP (not PLAT)
                            string flag1 = FlagVisible.ToString();
                            string flag2 = FlagColl.ToString();
                            string z = myFloat.ToString();
                            string j = myFloat2.ToString();
                            string i = myFloat3.ToString();

                            string q = myFloat4.ToString();
                            string w = myFloat5.ToString();
                            string r = myFloat6.ToString();

                            string t = myFloat7.ToString();
                            string y = myFloat8.ToString();
                            string o = myFloat9.ToString();



                            txtPosX.Text = z;
                            txtPosY.Text = j;
                            txtPosZ.Text = i;

                            txtRotX.Text = q;
                            txtRotY.Text = w;
                            txtRotZ.Text = r;

                            txtScaleX.Text = t;
                            txtScaleY.Text = y;
                            txtScaleZ.Text = o;
                            txtFlag.Text = flag1;
                            txtCollFlag.Text = flag2;
                        }
        }

        private void buttonExtract_Click_1(object sender, EventArgs e)
        {
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                            using (SaveFileDialog saveFile = new SaveFileDialog()
                            {
                                FileName = psl.assets[listBoxAssets.SelectedIndex].assetID.ToString("X16")
                            })
                                if (saveFile.ShowDialog() == DialogResult.OK)
                                    File.WriteAllBytes(saveFile.FileName, psl.assets[listBoxAssets.SelectedIndex].data);
        }

        private void buttonReplace_Click_1(object sender, EventArgs e)
        {
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                            using (OpenFileDialog openFile = new OpenFileDialog()
                            {
                                Title = "Choose a file to replace."
                            })
                                if (openFile.ShowDialog() == DialogResult.OK)
                                {
                                    byte[] file = File.ReadAllBytes(openFile.FileName);
                                    if (file.Length <= psl.assets[listBoxAssets.SelectedIndex].totalDataSize)
                                    {
                                        psl.assets[listBoxAssets.SelectedIndex].data = file;
                                        ReplaceInEditableArray(psl.assets[listBoxAssets.SelectedIndex].absoluteDataOffset, file, psl.assets[listBoxAssets.SelectedIndex].totalDataSize);

                                        psl.assets[listBoxAssets.SelectedIndex].actualSize = file.Length;
                                        WriteNewSizeInEditableArray(psl.assets[listBoxAssets.SelectedIndex].absoluteActualSizeOffset, file.Length);

                                        listBoxAssets_SelectedIndexChanged(sender, e);
                                    }
                                    else
                                        MessageBox.Show($"Please choose a file with at most the maximum size of the asset you want to replace.\n" +
                                            $"Maximum asset size: {psl.assets[listBoxAssets.SelectedIndex].totalDataSize} bytes\n" +
                                            $"Size of your file: {file.Length} bytes");
                                }
        }

        byte[] posX;
        byte[] posY;
        byte[] posZ;

        byte[] rotX;
        byte[] rotY;
        byte[] rotZ;

        byte[] scaleX;
        byte[] scaleY;
        byte[] scaleZ;
        public void saveDataEdited_Click(object sender, EventArgs e)
        {
            ulong assetID = GetSelectedAssetID();

            if (assetID != 0)
                if (listBoxLayers.SelectedIndex > -1 && listBoxLayers.SelectedIndex < hoFile.MAST.sectionSect2.layers.Count)
                    if (hoFile.MAST.sectionSect2.layers[listBoxLayers.SelectedIndex].subLayer is SubLayer_PSL psl)
                        if (listBoxAssets.SelectedIndex > -1 && listBoxAssets.SelectedIndex < psl.assets.Count)
                        {
                            float parsePosX = float.Parse(txtPosX.Text);
                            float parsePosY = float.Parse(txtPosY.Text);
                            float parsePosZ = float.Parse(txtPosZ.Text);
                            float s = parsePosX;
                            float i = parsePosY;
                            float p = parsePosZ;
                            float all1 = float.Parse(s.ToString());
                            float all2 = float.Parse(i.ToString());
                            float all3 = float.Parse(p.ToString());
                            posX = BitConverter.GetBytes(all1);
                            posY = BitConverter.GetBytes(all2);
                            posZ = BitConverter.GetBytes(all3);

                            float parseRotX = float.Parse(txtRotX.Text);
                            float parseRotY = float.Parse(txtRotY.Text);
                            float parseRotZ = float.Parse(txtRotZ.Text);
                            float q = parseRotX;
                            float w = parseRotY;
                            float r = parseRotZ;
                            float all4 = float.Parse(q.ToString());
                            float all5 = float.Parse(w.ToString());
                            float all6 = float.Parse(r.ToString());
                            rotX = BitConverter.GetBytes(all4);
                            rotY = BitConverter.GetBytes(all5);
                            rotZ = BitConverter.GetBytes(all6);

                            float parseScaleX = float.Parse(txtScaleX.Text);
                            float parseScaleY = float.Parse(txtScaleY.Text);
                            float parseScaleZ = float.Parse(txtScaleZ.Text);
                            float a = parseScaleX;
                            float l = parseScaleY;
                            float d = parseScaleZ;
                            float all7 = float.Parse(a.ToString());
                            float all8 = float.Parse(l.ToString());
                            float all9 = float.Parse(d.ToString());
                            scaleX = BitConverter.GetBytes(all7);
                            scaleY = BitConverter.GetBytes(all8);
                            scaleZ = BitConverter.GetBytes(all9);

                            byte parseFlag = Byte.Parse(txtFlag.Text);
                            byte parseFlag1 = Byte.Parse(txtCollFlag.Text);

                            if (BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(posX);
                                Array.Reverse(posY);
                                Array.Reverse(posZ);

                                Array.Reverse(rotX);
                                Array.Reverse(rotY);
                                Array.Reverse(rotZ);

                                Array.Reverse(scaleX);
                                Array.Reverse(scaleY);
                                Array.Reverse(scaleZ);

                            }

                            posX.CopyTo(x, 0x2C);
                            posY.CopyTo(x, 0x30);
                            posZ.CopyTo(x, 0x34);

                            rotX.CopyTo(x, 0x20);
                            rotY.CopyTo(x, 0x24);
                            rotZ.CopyTo(x, 0x28);

                            scaleX.CopyTo(x, 0x38);
                            scaleY.CopyTo(x, 0x3C);
                            scaleZ.CopyTo(x, 0x40);
                            x[0x10] = parseFlag;
                            x[0x13] = parseFlag1;


                            psl.assets[listBoxAssets.SelectedIndex].data = x;
                            ReplaceInEditableArray(psl.assets[listBoxAssets.SelectedIndex].absoluteDataOffset, x, psl.assets[listBoxAssets.SelectedIndex].totalDataSize);

                            psl.assets[listBoxAssets.SelectedIndex].actualSize = x.Length;
                            WriteNewSizeInEditableArray(psl.assets[listBoxAssets.SelectedIndex].absoluteActualSizeOffset, x.Length);

                            listBoxAssets_SelectedIndexChanged(sender, e);
                        }

        }

        private void button1_Click(object sender, EventArgs e)  // Randomizer Generator
        {
            // Generate Random Numbers for the Position Fields
            Random slumpGenerator1 = new Random();
            int txt = slumpGenerator1.Next(0, 100);
            Random slumpGenerator2 = new Random();
            int txt1 = slumpGenerator2.Next(0, 50);
            Random slumpGenerator3 = new Random();
            int txt2 = slumpGenerator3.Next(0, 90);
            txtPosX.Text = txt.ToString();
            txtPosY.Text = txt1.ToString();
            txtPosZ.Text = txt2.ToString();

            // Generate Random Numbers for the Rotation Fields
            Random slumpGenerator4 = new Random();
            int txt4 = slumpGenerator4.Next(0, 100);
            Random slumpGenerator5 = new Random();
            int txt5 = slumpGenerator5.Next(0, 50);
            Random slumpGenerator6 = new Random();
            int txt6 = slumpGenerator6.Next(0, 90);
            txtRotX.Text = txt4.ToString();
            txtRotY.Text = txt5.ToString();
            txtRotZ.Text = txt6.ToString();

            // Generate Random Numbers for the Scale Fields
            Random slumpGenerator7 = new Random();
            int txt7 = slumpGenerator7.Next(0, 100);
            Random slumpGenerator8 = new Random();
            int txt8 = slumpGenerator8.Next(-100, 89);
            Random slumpGenerator9 = new Random();
            int txt9 = slumpGenerator9.Next(0, 53);
            txtScaleX.Text = txt7.ToString();
            txtScaleY.Text = txt8.ToString();
            txtScaleZ.Text = txt9.ToString();
        }

        private void btnApplyScaleX_Click(object sender, EventArgs e)
        {
            string input1 = txtScaleX.Text;

            txtScaleY.Text = input1;
            txtScaleZ.Text = input1;
        }


        private void radioButtonTrue_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rdoFalse_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void buttonExtractAll_Click_1(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                    hoFile.ExtractAssetsToFolders(folderBrowser.SelectedPath, true);
        }

        private void exportLayerListToTextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFile = new SaveFileDialog();
            saveFile.Filter = "Text (*.txt)|*.txt";
            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFile.FileName, false))
                    foreach (var item in listBoxLayers.Items)
                        sw.Write(item.ToString() + Environment.NewLine);
                MessageBox.Show("Successfully Wrote Layers to Text File!");
            }
        }

        private void exportAssetsListToTextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFile = new SaveFileDialog();
            saveFile.Filter = "Text (*.txt)|*.txt";
            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (var sw = new StreamWriter(saveFile.FileName, false))
                    foreach (var item in listBoxAssets.Items)
                        sw.Write(item.ToString() + Environment.NewLine);
                MessageBox.Show("Successfully Wrote Assets List to Text File!");
            }
        }

        private void saveHoParcelAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFile = new SaveFileDialog()
            {
                FileName = fileName
            })
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    fileName = saveFile.FileName;
                    saveHoParcelToolStripMenuItem_Click(sender, e);
                }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txtPosX.Text = String.Empty;
            txtPosY.Text = String.Empty;
            txtPosZ.Text = String.Empty;
            txtRotX.Text = String.Empty;
            txtRotY.Text = String.Empty;
            txtRotZ.Text = String.Empty;
            txtScaleX.Text = String.Empty;
            txtScaleY.Text = String.Empty;
            txtScaleZ.Text = String.Empty;
        }

        private void btnFindLayer_Click(object sender, EventArgs e)
        {
            listBoxLayers.ClearSelected();

            int index = listBoxLayers.FindString(txtLayerSearch.Text);

            if (index < 0)
            {
                MessageBox.Show("Could not find a layer with that search string.");
                txtLayerSearch.Text = String.Empty;
            }
            else
            {
                listBoxLayers.SelectedIndex = index;
            }
        }

        private void btnFindAsset_Click(object sender, EventArgs e)
        {
            listBoxAssets.ClearSelected();

            int index = listBoxAssets.FindString(txtAssetSearch.Text);

            if (index < 0)
            {
                MessageBox.Show("Could not find an asset with that search string.");
                txtAssetSearch.Text = String.Empty;
            }
            else
            {
                listBoxAssets.SelectedIndex = index;
            }
        }

        private void btnLayerUp_Click(object sender, EventArgs e)
        {
            if (listBoxLayers.SelectedIndex != listBoxLayers.Items.Count + 1)
                listBoxLayers.SelectedIndex = listBoxLayers.SelectedIndex - 1;
        }

        private void btnLayerDown_Click(object sender, EventArgs e)
        {
            if (listBoxLayers.SelectedIndex > 0)
                listBoxLayers.SelectedIndex = listBoxLayers.SelectedIndex + 1;
        }

        private void btnAssetUp_Click(object sender, EventArgs e)
        {
            if (listBoxAssets.SelectedIndex != listBoxAssets.Items.Count + 1)
                listBoxAssets.SelectedIndex = listBoxAssets.SelectedIndex - 1;
        }

        private void btnAssetDown_Click(object sender, EventArgs e)
        {
            if (listBoxAssets.SelectedIndex > 0)
                listBoxAssets.SelectedIndex = listBoxAssets.SelectedIndex + 1;
        }

        private void aboutTheGoodEditor2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var aboutWindow = new About();
            aboutWindow.Show();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gHelpToolTips.Active = true;
        }

        private void disableToolTipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gHelpToolTips.Active = false;
        }

        private void checkBox1_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("Check this checkbox to open ho files that are little endian. Example: Family Guy: Back to the Multiverse's ho files", checkBox1);
        }

        private void listBoxLayers_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("This listbox shows which layers are loaded from the ho file. Click on one to view which assets are in that layer.", listBoxLayers);
        }

        private void listBoxAssets_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("This listbox shows which assets are in a layer.", listBoxAssets);
        }

        private void txtLayerSearch_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("This searchbox lets you search and highlight the first search string in the search box", txtLayerSearch);
        }

        private void btnFindLayer_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("Press this button to search for a string.", btnFindLayer);
        }

        private void btnLayerUp_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("fine scroll up through layers.", btnLayerUp);
        }

        private void btnLayerDown_MouseHover(object sender, EventArgs e)
        {
            gHelpToolTips.Show("fine scroll down through layers.", btnLayerDown);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            DiscordRPCController.ToggleDiscordRichPresence(checkBox2.Checked);
        }
    }
}