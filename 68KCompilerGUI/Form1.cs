/********************************************************************************/
/*                                                                              */
/*  Set the "VBCC" environment variable to "C:\amiga-dev\targets\m68k-amigaos"  */
/*  Add the following to the "Path" environment variable to C:\amiga-dev\bin\"  */
/*                                                                              */
/*                  To compile execute: vc -o hello hello.c                     */
/*                                                                              */
/********************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Interop
{
    public partial class Form1 : Form
    {
        private string path = string.Empty;
        private string temp = string.Empty;
        private string loadedFileName = string.Empty;
        private vc page;

        private List<string> log = new List<string>();
        private List<string> file = new List<string>();

        public Form1()
        {
            // Get current directories immediate parent
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory());

            var configPath = parentDir.Parent.FullName.ToString() + @"\amiga-dev-master\";
            var vbccPath = parentDir.Parent.FullName.ToString() + @"\amiga-dev-master\targets\m68k-amigaos";
            var binPath = parentDir.Parent.FullName.ToString() + @"\amiga-dev-master\bin";

            var temp = parentDir.Parent.FullName.ToString() + @"/amiga-dev-master/";

            log.Add("configPath = " + configPath);
            log.Add("vbccPath = " + vbccPath);
            log.Add("binPath = " + binPath);
            log.Add("temp0 = " + temp);

            if (temp.Contains("\\"))
            {
                var tump = temp.Replace("\\", "/");
                this.temp = tump;
            }

            log.Add("temp1 = " + this.temp);

            //https://stackoverflow.com/questions/14553830/set-environment-variables-for-a-process
            //For getting
            string Pathsave = System.Environment.GetEnvironmentVariable("VBCC", EnvironmentVariableTarget.User);

            log.Add("Pathsave = " + Pathsave);

            //Set the “VBCC” environment variable to “C:\amiga-dev\targets\m68k-amigaos”.
            Environment.SetEnvironmentVariable("VBCC", vbccPath, EnvironmentVariableTarget.User);

            // Get the path environment variables
            string pathvar = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            log.Add("pathvar = " + pathvar);


            if (!pathvar.Contains(binPath))
            {
                Environment.SetEnvironmentVariable("Path", pathvar + ";" + binPath, EnvironmentVariableTarget.User);
            }

            generateConfigFile(vbccPath, configPath);

            InitializeComponent();
            textBox1.Multiline = true;
            button1.Enabled = false;
            toolStripMenuItem2.Enabled = false;
        }

        // Open File method
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "WigWam Interop";
            openFileDialog.InitialDirectory = @"*.*";
            openFileDialog.Filter = "All files (*.*)|*.*|All files (*.c)|*.c";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Clear();
                path = openFileDialog.FileName;
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        // Read the input stream & display the contents
                        while (!sr.EndOfStream)
                        {
                            file.Add(sr.ReadLine());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The file could not be read: " + ex.Message);
                }
                finally
                {
                    textBox1.Lines = file.ToArray();
                    loadedFileName = openFileDialog.SafeFileName;
                    button1.Enabled = true;
                    this.Text = "68K x-compiler - " + openFileDialog.SafeFileName;
                    toolStripMenuItem2.Enabled = true;
                }
            }
        }

        // Save File method
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if(loadedFileName != string.Empty)
            {
                save(loadedFileName);
            }
            else
            {
                saveAs();
            }
        }

        // Exit the application
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // New File
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            loadedFileName = string.Empty;
            this.Text = "68K x-compiler";
        }

        // Compile button click
        private void button1_Click(object sender, EventArgs e)
        {
            compile();
            File.WriteAllLines("log.log", log);
        }

        // Method to save the file 
        private void save(string fileName)
        {
            FileStream ostrm = null;
            StreamWriter writer = null;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot open " + fileName + " for writing: " + ex.Message);
                return;
            }
            finally
            {
                writer = new StreamWriter(ostrm);
                Console.SetOut(writer);

                foreach (string line in textBox1.Lines)
                {
                    Console.WriteLine(line);
                }

                path = ostrm.Name;
                Console.SetOut(oldOut);
                writer.Close();
                ostrm.Close();
                loadedFileName = Path.GetFileName(fileName);
                button1.Enabled = true;
            }
        }

        private void saveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save File";
            saveFileDialog.InitialDirectory = @"*.*";
            saveFileDialog.Filter = "All files (*.*)|*.*|All files (*.c)|*.c";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                path = saveFileDialog.FileName;
                save(saveFileDialog.FileName);
                 this.Text = "68K x-compiler - " + loadedFileName;
                toolStripMenuItem2.Enabled = true;
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAs();
        }



        // Method to start CMD and pass in the relevent command line args
        private void compile()
        {
            //https://stackoverflow.com/questions/4788863/how-to-send-series-of-commands-to-a-command-window-process
            ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd.exe");
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;

            Process process = Process.Start(processStartInfo);

            if (process != null)
            {
                process.StandardInput.WriteLine("cd " + temp);
                //process.StandardInput.WriteLine("vc -o hello hello.c");
                var name = loadedFileName.Replace(".c", "");
                process.StandardInput.WriteLine("vc -o " + name + " " + loadedFileName);
                process.StandardInput.WriteLine("y");
                process.StandardInput.WriteLine("y");

                process.StandardInput.Close(); // line added to stop process from hanging on ReadToEnd()

                string outputString = process.StandardOutput.ReadToEnd();
                var tp = outputString.Replace("Microsoft Windows [Version 6.1.7601]\r\nCopyright (c) 2009 Microsoft Corporation.  All rights reserved.\r\n\r\n", "");
            }
        }

        // Method to generate the config file using the template
        private void generateConfigFile(string prefix, string configPath)
        {
            page = new vc();
            page.WriteLine(@"-cc=vbccm68k -quiet -hunkdebug %s -o= %s %s -O=%ld -I""" + prefix + @"/include/""");
            page.WriteLine(@"-ccv=vbccm68k -hunkdebug %s -o= %s %s -O=%ld -I""" + prefix + @"/include/""");
            page.WriteLine(@"-as=vasmm68k_mot -quiet -Fhunk -phxass -opt-fconst -nowarn=62 %s -o %s");
            page.WriteLine(@"-asv=vasmm68k_mot -Fhunk -phxass -opt-fconst -nowarn=62 %s -o %s");
            page.WriteLine(@"-rm=del quiet %s");
            page.WriteLine(@"-rmv=del %s");
            page.WriteLine(@"-ld=vlink -bamigahunk -x -Bstatic -Cvbcc -nostdlib """ + prefix + @"/lib/startup.o"" %s %s -L""" + prefix + @"/lib/"" -lvc -o %s");
            page.WriteLine(@"-l2=vlink -bamigahunk -x -Bstatic -Cvbcc -nostdlib %s %s -L""" + prefix + @"/lib/"" -o %s");
            page.WriteLine(@"-ldv=vlink -bamigahunk -t -x -Bstatic -Cvbcc -nostdlib """ + prefix + @"/lib/startup.o"" %s %s -L""" + prefix + @"/lib/"" -lvc -o %s");
            page.WriteLine(@"-l2v=vlink -bamigahunk -t -x -Bstatic -Cvbcc -nostdlib %s %s -L""" + prefix + @"/lib/"" -o %s");
            page.WriteLine(@"-ldnodb=-s -Rshort");
            page.WriteLine(@"-ul=-l%s");
            page.WriteLine(@"-cf=-F%s");
            page.WriteLine(@"-ml=500");

            string pageContent = page.TransformText();
            File.WriteAllText(configPath + "vc.cfg", pageContent);
        }


    }
}
