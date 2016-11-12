using System;
using Gtk;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

public partial class MainWindow: Gtk.Window
{
	string fileList = "";

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnButton6Clicked (object sender, EventArgs e)
	{
		Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog ("select files",
			this,
			Gtk.FileChooserAction.Open,
			"Cancel", Gtk.ResponseType.Cancel,
			"Open", Gtk.ResponseType.Accept);
		fc.Filter = new FileFilter ();
		fc.Filter.AddPattern ("*.jpg");
		fc.Filter.AddPattern ("*.JPG");
		fc.SelectMultiple = true;

		if (fc.Run () == (int)Gtk.ResponseType.Accept) {
			fileList = String.Join (",", fc.Filenames);
			textview3.Buffer.Text = String.Join ("\n", fc.Filenames);
		}
		fc.Destroy ();
	}

	protected void OnButton4Clicked (object sender, EventArgs e)
	{
		if (outfile.Text.Length < 5) {
			Gtk.MessageDialog md = new Gtk.MessageDialog (this, DialogFlags.Modal, MessageType.Error, ButtonsType.Close, "No Filename set");
			if ((ResponseType)md.Run () == ResponseType.Close) {
				md.Destroy ();
			}
			return;
		}
		if (fileList.Length < 5) {
			Gtk.MessageDialog md = new Gtk.MessageDialog (this, 
				DialogFlags.Modal, 
				MessageType.Error, 
				ButtonsType.Close, 
				"You can't have a file selected");
			if ((ResponseType)md.Run () == ResponseType.Close) {
				md.Destroy ();
			}
			return;
		}

		textview1.Buffer.Text += "Running, please wait\n";

		Thread thr = new Thread (new ThreadStart (processVideo));
		thr.Start ();

	}

	protected void processVideo ()
	{
		int count = 1;
		string fmt = "000000";
		string tempSubDir = Guid.NewGuid ().ToString ("n").Substring (0, 16);
		string[] movieFiles = fileList.Split (',');
		string workingDir = $"/tmp/{tempSubDir}";
		System.IO.Directory.CreateDirectory (workingDir);

		foreach (string f in movieFiles) {
			//copy it to temp
			string newFileName = $"{workingDir}/movieimg_{count.ToString(fmt)}.jpg";
			System.IO.File.Copy (f, newFileName);
			count += 1;
		}

		int fr = Convert.ToInt32 (framerate.Text);
		string com = $"-framerate {fr} -f image2 -i {workingDir}/movieimg_%06d.jpg -c:v h264 -crf 1 {workingDir}/{outfile.Text}";
		
		Gtk.Application.Invoke (delegate {
			textview1.Buffer.Text += $"{com}\n";
		});

		Process proc = new System.Diagnostics.Process ();
		proc.StartInfo.FileName = "/usr/bin/avconv";
		proc.StartInfo.Arguments = com;
		proc.StartInfo.UseShellExecute = false; 
		proc.StartInfo.RedirectStandardOutput = true;
		proc.StartInfo.RedirectStandardError = true;
		proc.Start ();
		StreamReader myStreamReader = proc.StandardError;
		while (!myStreamReader.EndOfStream) {
			string s = $"{myStreamReader.ReadLine ()}\n";
			Gtk.Application.Invoke (delegate {
				textview1.Buffer.Text += s;
			});
		}
		proc.WaitForExit ();
		//System.IO.Directory.Delete($"/tmp/{tempSubDir}", true );
		Gtk.Application.Invoke (delegate {
			textview1.Buffer.Text += "Seems we are done\n";
			Process.Start($"file://{workingDir}");

		});
	}
}

