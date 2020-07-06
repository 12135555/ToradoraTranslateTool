﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using OBJEditor;
using Newtonsoft.Json.Linq;

namespace ToradoraTranslateTool
{
    public partial class FormTranslation : Form
    {
        string currentFile;
        string mainFilePath = Path.Combine(Application.StartupPath, "Translation.json");

        public FormTranslation()
        {
            InitializeComponent();

            try
            {
                if (!File.Exists(mainFilePath))
                    File.WriteAllText(mainFilePath, "{ }");

                List<String> directories = new List<string>();
                directories.AddRange(Directory.GetDirectories(Path.Combine(Application.StartupPath, "Data", "Txt")).Select(Path.GetFileName)); // Get all directories with .obj and .txt files
                directories.AddRange(Directory.GetDirectories(Path.Combine(Application.StartupPath, "Data", "Obj")).Select(Path.GetFileName));

                dataGridViewFiles.Rows.Add("Total: ", "0%");

                JObject mainFile = JObject.Parse(File.ReadAllText(mainFilePath));
                foreach (string name in directories) // Adding files to the table
                {
                    string translationPercent = "0";
                    if (mainFile[name] != null)  // If json have saved translation
                    {
                        int stringCount = mainFile[name].Children().Children().Count(); // scary
                        int translatedCount = 0;
                        for (int i = 0; i < stringCount; i++)
                        {
                            if (mainFile[name][i.ToString()].ToString() != "")
                                translatedCount++;
                        }
                        translationPercent = Math.Round((double)(translatedCount * 100) / stringCount, 1).ToString();
                    }

                    dataGridViewFiles.Rows.Add(name, translationPercent + "%");
                }
                updateTotalPercent();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridViewFiles_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex > 0) // Ignore row with total translation percent
                    LoadFile(dataGridViewFiles[0, e.RowIndex].Value.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFile(string filename)
        {
            if (currentFile != null)
                SaveProgress();

            currentFile = filename;
            string[] myStrings;
            Dictionary<int, string> myNames = new Dictionary<int, string>();
            if (Path.GetExtension(currentFile) == ".obj")
            {
                string filepath = Path.Combine(Application.StartupPath, "Data", "Obj", currentFile, currentFile);
                OBJHelper myHelper = new OBJHelper(File.ReadAllBytes(filepath));
                myStrings = myHelper.Import();
                myNames = myHelper.Actors;
            }
            else // Else it is .txt file
            {
                string filepath = Path.Combine(Application.StartupPath, "Data", "Txt", currentFile, currentFile);
                myStrings = File.ReadAllLines(filepath, new UnicodeEncoding(false, false)); // Txt file has encoding UTF-16 LE (Unicode without BOM)
            }

            JObject mainFile = JObject.Parse(File.ReadAllText(mainFilePath));
            bool haveTranslation = false;
            if (mainFile[currentFile] != null)
                haveTranslation = true;

            dataGridViewStrings.Rows.Clear();
            for (int i = 0; i < myStrings.Length; i++)
            {
                string name = "";
                string sentence = "";
                string translated = "";
                if (myStrings[i].StartsWith("「") && myStrings[i].EndsWith("」"))
                {
                    name = myNames[i];
                    sentence = myStrings[i].TrimStart('「').TrimEnd('」'); // Remove brackets from the beginning and end of the original sentence
                }
                else
                    sentence = myStrings[i];

                if (haveTranslation)
                {
                    translated = mainFile[currentFile][i.ToString()].ToString();

                    if (translated.StartsWith("「") && translated.EndsWith("」"))
                        translated = translated.TrimStart('「').TrimEnd('」'); // Remove brackets from the beginning and end of the original sentence
                }

                dataGridViewStrings.Rows.Add(name, sentence, translated);
            }
        }

        private void SaveProgress()
        {
            JObject mainFile = JObject.Parse(File.ReadAllText(mainFilePath));

            int translatedCount = 0;
            if (mainFile[currentFile] != null)
            {
                for (int i = 0; i < dataGridViewStrings.Rows.Count; i++) // Updating translation in json
                {
                    string translatedString = dataGridViewStrings.Rows[i].Cells[2].Value?.ToString();
                    if (translatedString != "" && translatedString != null)
                    {
                        translatedCount++;
                        if (dataGridViewStrings.Rows[i].Cells[0].Value?.ToString() != "") // If have a name, then add the necessary brackets
                            translatedString = "「" + translatedString + "」";
                    }
                    mainFile[currentFile][i.ToString()] = translatedString;
                }
            }
            else
            {
                JObject translatedStrings = new JObject(); // Creating json with all strings
                for (int i = 0; i < dataGridViewStrings.Rows.Count; i++)
                {
                    string translatedString = dataGridViewStrings.Rows[i].Cells[2].Value?.ToString();
                    if (translatedString != "")
                    {
                        translatedCount++;
                        if (dataGridViewStrings.Rows[i].Cells[0].Value?.ToString() != "" && dataGridViewStrings.Rows[i].Cells[0].Value?.ToString() != null)
                            translatedString = "「" + translatedString + "」";
                    }
                    translatedStrings.Add(i.ToString(), translatedString);
                }
                mainFile.Add(new JProperty(currentFile, translatedStrings));
            }

            string translationPercent = Math.Round((double)(translatedCount * 100.0) / dataGridViewStrings.Rows.Count, 1).ToString();
            DataGridViewRow myRow = dataGridViewFiles.Rows.Cast<DataGridViewRow>().Where(r => r.Cells[0].Value.ToString().Equals(currentFile)).First(); // Find row by filename
            myRow.Cells[1].Value = translationPercent.ToString() + "%";
            updateTotalPercent();

            File.WriteAllText(mainFilePath, mainFile.ToString());
        }

        private void FormTranslation_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (currentFile != null)
                    SaveProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridViewStrings_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.ColumnIndex != -1 && e.RowIndex != -1 && e.Button == MouseButtons.Right)
                {
                    DataGridViewCell c = (sender as DataGridView)[e.ColumnIndex, e.RowIndex];
                    c.DataGridView.ClearSelection();
                    c.DataGridView.CurrentCell = c;
                    c.Selected = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void itemExportStrings_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentFile == null)
                {
                    MessageBox.Show("First select the file!", "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (SaveFileDialog mySaveFileDialog = new SaveFileDialog())
                {
                    mySaveFileDialog.Filter = "Text file (*.txt) | *.txt";

                    if (mySaveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string[] myStrings = new string[dataGridViewStrings.Rows.Count];
                        for (int i = 0; i < myStrings.Length; i++)
                        {
                            myStrings[i] = dataGridViewStrings.Rows[i].Cells[0].Value?.ToString() + ";" + dataGridViewStrings.Rows[i].Cells[1].Value?.ToString() + ";" + dataGridViewStrings.Rows[i].Cells[2].Value?.ToString();
                        }
                        File.WriteAllLines(mySaveFileDialog.FileName, myStrings);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void itemImportStrings_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentFile == null)
                {
                    MessageBox.Show("First select the file!", "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (OpenFileDialog myOpenFileDialog = new OpenFileDialog())
                {
                    myOpenFileDialog.Filter = "Text file (*.txt) | *.txt";

                    if (myOpenFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string[] myStrings = File.ReadAllLines(myOpenFileDialog.FileName);
                        for (int i = 0; i < myStrings.Length; i++)
                        {
                            dataGridViewStrings.Rows[i].Cells[2].Value = myStrings[i];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void updateTotalPercent()
        {
            double currentPercent = 0;
            for (int i = 1; i < dataGridViewFiles.Rows.Count; i++)
            {
                currentPercent += Double.Parse(dataGridViewFiles.Rows[i].Cells[1].Value?.ToString().Replace("%", ""));
            }
            string currentTotalPercent = Math.Round((double)(currentPercent * 100.0) / ((dataGridViewFiles.Rows.Count - 1) * 100.0), 1).ToString();
            dataGridViewFiles.Rows[0].Cells[1].Value = currentTotalPercent + "%";
        }

        private List<string> GetAllNames()
        {
            List<string> uniqueNames = new List<string>();

            for (int i = 1; i < dataGridViewFiles.Rows.Count; i++)
            {
                string filename = dataGridViewFiles.Rows[i].Cells[0].Value?.ToString();
                if (Path.GetExtension(filename) != ".obj")
                    continue;

                string filepath = Path.Combine(Application.StartupPath, "Data", "Obj", filename, filename);
                OBJHelper myHelper = new OBJHelper(File.ReadAllBytes(filepath));
                Dictionary<int, string> myNames = new Dictionary<int, string>();
                myHelper.Import();
                myNames = myHelper.Actors;

                for (int ii = 0; ii < myNames.Count; ii++)
                {
                    if (uniqueNames.Contains(myNames[ii]) == false)
                        uniqueNames.Add(myNames[ii]);
                }
            }

            uniqueNames.Remove(null);
            return uniqueNames;
        }

        private void translateNamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormNames myForm = new FormNames(GetAllNames());
            myForm.Show();
        }

        private void itemLineBreaks_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentFile == null)
                {
                    MessageBox.Show("First select the file!", "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                for (int i = 0; i < dataGridViewStrings.RowCount; i++)
                {
                    string translatedString = dataGridViewStrings.Rows[i].Cells[2].Value?.ToString();
                    if (translatedString.Length > 40) // Dialog box can fit ~31 uppercase characters, and ~53 lowercase characters
                    {
                        var charCount = 0;
                        var lines = translatedString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                        .GroupBy(w => (charCount += w.Length + 1) / 41)
                                        .Select(g => string.Join(" ", g.ToArray()));

                        dataGridViewStrings.Rows[i].Cells[2].Value = String.Join("＿", lines.ToArray());
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error!" + Environment.NewLine + ex.ToString(), "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonFilesGridHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This table contains 363 files with 26508 lines to be translated." + Environment.NewLine + "Double-click a file to load it", "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonTextGridHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This table contains all the sentences stored in the selected file." + Environment.NewLine +
                "All entered data will be automatically saved for later use." + Environment.NewLine +
                "You can export all rows to a .txt file from the context menu, and import this file into Excel or Google Docs to get a nice table. The separator for tables is \";\"." + Environment.NewLine +
                "You can also import the finished translation into the program. To do this, you need an .txt file in which each sentence will be from a new line." + Environment.NewLine +
                "Learn more at: https://github.com/12135555/ToradoraTranslateTool", "ToradoraTranslateTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
