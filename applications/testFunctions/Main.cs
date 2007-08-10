// /home/motters/develop/sentience/applications/testFunctions/test/Main.cs created with MonoDevelop
// User: motters at 7:38 PM 7/30/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//
// project created on 7/30/2007 at 7:38 PM
using System;
using Gtk;

namespace test
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