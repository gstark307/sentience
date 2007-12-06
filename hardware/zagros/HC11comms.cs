/*
    Serial communications with HC11
    Copyright (C) 2000-2007 Bob Mottram
    fuzzgun@gmail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Reflection;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;


namespace zgros
{
	/// <summary>
	/// Class used for HC11 communications
	/// </summary>
	public class HC11comms
	{
		// serial port object
		private SerialPort comms = new SerialPort();
		
#region "constructors"

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="COM_port">
		/// A <see cref="System.Int32"/>
		/// COM port number, beginning at 1
		/// </param>
        public HC11comms(int COM_port)
		{
	    	// Configure serial port Parameters
	    	comms.DataBits = 8;
	    	comms.BaudRate = 9600;
			comms.Parity= Parity.None;
			comms.StopBits = StopBits.One;
	    	comms.Handshake= Handshake.None;
	    	comms.ReadTimeout = 150;
	    	
	    	if (IsWindows() == true)
				comms.PortName = "COM" + COM_port.ToString();
			else
				// note that COM ports in gnu/linux start at zero, so we subtract one
				comms.PortName = "/dev/ttyS" + (COM_port-1).ToString();
		}
		
#endregion

#region "starting and stoping comms"
		
		/// <summary>
		/// end communications
		/// </summary>
        public void Stop()
		{
			if (comms.IsOpen)
			{
				comms.Close();
				Console.WriteLine("Serial communications with the HC11 stopped");
			}
		}

		/// <summary>
		/// begin communications
		/// </summary>
        public void Start()
		{			
			Stop();
			
			try
			{
			    comms.Open();
			}
			catch(Exception e)
			{
				Console.WriteLine("error detected: " + e.Message);
			}
			
			if (comms.IsOpen)
			{
		        System.Threading.Thread.Sleep(200);		// Wait 200 milliseconds				
			    comms.DiscardInBuffer();
			    comms.DiscardOutBuffer();
				Console.WriteLine("Serial communications with the HC11 started...");
			}
		}
		
#endregion
				
		
#region "platform specific code"		
		
		/// <summary>
		/// Method to detect if runtime platform is a MS Windows or not 
		/// </summary>
		private bool IsWindows()
		{
			return Path.DirectorySeparatorChar == '\\';
		}
		
#endregion

#region "sending and receiving data"		
		
		/// <summary>
		/// Reads the buffer and returns a hexadecimal value
		/// </summary>
		/// <param name="delay">Milliseconds for timeout</param>
		/// <param name="_internalSpace">want a space between bytes</param>
		/// <returns>hexadecimal value of byte array</returns>
		public string Receive(int delay, bool _internalSpace)
		{
			int theByte = 0;
			string rxHexString = "";
			string rxHexStringOut = "";
			
			if (comms.IsOpen)
			{			
				comms.ReadTimeout = delay;
				
				try
				{
					// Read all Buffer data				
					Console.WriteLine(""); Console.WriteLine("reading...");
					
					while (1==1)
					{	
							theByte = comms.ReadByte();
							rxHexString += theByte.ToString("X2");
					}
					
				}
				catch(Exception e)
				{
					Console.WriteLine("error on readData method: " + e.Message);
				}
				
				Console.WriteLine("rxHexString = " + rxHexString);
				
				// Insert space between bytes
				if (_internalSpace == true)
				{
					for (int j=0; j<rxHexString.Length; j+=2)
						rxHexStringOut = rxHexStringOut + rxHexString.Substring(j,2) + " ";
				}
				else
					rxHexStringOut = rxHexString;
	 
			}
			
			return rxHexStringOut.ToUpper() ;
		}

		
		
		/// <summary>
		/// Write data to the buffer
		/// </summary>		
		/// <param name="dataIN">hexadecimal value to write</param>
		public void Send(string dataIN)
		{
			if (comms.IsOpen)
			{
				byte[] tmpBytes;
				
				int writeBytes;
				int n = 0;
				int j = 0;
				
				string tmpInString = dataIN;
				tmpInString = tmpInString.Replace(" ","");
				
				
				if (tmpInString.Trim() == "" )
					return;
				
				writeBytes = tmpInString.Length;
				
				// Prepare data buffer
				tmpBytes = new byte[writeBytes/2];
				
				for (int k=0; k<writeBytes; k+=2)
				{
					n = Convert.ToInt32(tmpInString.Substring(k,2), 16);				
					tmpBytes[j] = (byte)n;
					j++;
					
				}	
				

				try
				{
					 comms.Write(tmpBytes,0,tmpBytes.Length);				
				}
				catch(Exception e)
				{
					Console.WriteLine("error on writeData method: " + e.Message);
				}
			}
			
			return;
		}
		
#endregion
		
		
	}
}
