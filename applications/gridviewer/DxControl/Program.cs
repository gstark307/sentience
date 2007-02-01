/*
    Sentience 3D Perception System: Grid viewer
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DirectX;
using Container = System.ComponentModel.Container;
using Gosub;
using DirectXSample;


/// <summary>
/// The main windows form for the application.
/// </summary>
public class MainClass
{
    /// <summary>
    // Main entry point of the application.
    /// </summary>
	[STAThread]
    public static void Main()
    {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new frmGridViewer());
    }
}
