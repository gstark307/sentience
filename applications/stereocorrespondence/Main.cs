// Main.cs created with MonoDevelop
// User: motters at 8:12 AM 9/24/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
// project created on 9/24/2007 at 8:12 AM
using System;
using Gtk;

namespace stereocorrespondence
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
		}
	}
}