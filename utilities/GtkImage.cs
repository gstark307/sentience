/*
    Functions to enable accessing Gtk Image objects
    Copyright (C) 2007 Bob Mottram    
    fuzzgun@gmail.com
    Adapted from public domain code originally written by Federico Mena-Quintero <federico@novell.com>

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
using System.IO;
using System.Collections;
using Gdk;
using Gtk;

namespace sluggish.utilities.gtk
{
	class Image {
		public Image (string filename)
		{
			load (filename);
		}

		public int XPos {
			get {
				return xpos;
			}

			set {
				xpos = value;
			}
		}

		public int YPos {
			get {
				return ypos;
			}

			set {
				ypos = value;
			}
		}

		public int Width {
			get {
				return width;
			}
		}

		public int Height {
			get {
				return height;
			}
		}

		public byte[] CompressedData {
			get {
				return compressed_data;
			}
			set {
				compressed_data = value;
			}
		}

		public Gdk.Pixmap ServerPixmap {
			get {
				return server_pixmap;
			}

			set {
				server_pixmap = value;
			}
		}

		public Gdk.Pixbuf ClientPixbuf {
			get {
				return client_pixbuf;
			}

			set {
				client_pixbuf = value;
			}
		}

		public Gdk.Pixbuf MakePixbufFromCompressedData ()
		{
			Gdk.Pixbuf pixbuf;

			using (Gdk.PixbufLoader loader = new Gdk.PixbufLoader ()) {
				loader.Write (compressed_data);
				loader.Close ();
				pixbuf = loader.Pixbuf;
			}

			return pixbuf;
		}

		private int xpos, ypos;
		private int width, height;
		private byte[] compressed_data;
		private Gdk.Pixmap server_pixmap;
		private Gdk.Pixbuf client_pixbuf;

		private void load (string filename)
		{
			using (FileStream fs = new FileStream (filename, FileMode.Open, FileAccess.Read)) {
				long length;
				bool done;
				long pos;
				byte[] buffer;

				length = fs.Length;
				compressed_data = new byte[length];
				fs.Read (compressed_data, 0, (int) length);

				/* Feed small chunks at a time to a pixbuf loader until
				 * it emits the "size-prepared" signal.  This lets us
				 * avoid uncompressing the whole image just to figure
				 * out its size.
				 */

				using (Gdk.PixbufLoader loader = new Gdk.PixbufLoader ()) {
					loader.SizePrepared += new SizePreparedHandler (
						delegate (object o, SizePreparedArgs args) {
							done = true;
							width = args.Width;
							height = args.Height;
						});

					done = false;
					pos = 0;
					buffer = new byte[512];

					while (!done) {
						long to_copy;

						to_copy = length - pos;
						if (to_copy > 512)
							to_copy = 512;
						else if (to_copy == 0)
							break;

						Array.Copy (compressed_data, pos, buffer, 0, to_copy);
						loader.Write (buffer);

						pos += to_copy;
					}

					loader.Close ();
				}
			}
		}
	}

}