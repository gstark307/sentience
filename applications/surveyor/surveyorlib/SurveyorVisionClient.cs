/*
    Vision client for Surveyor robots
    Copyright (C) 2008 Bob Mottram
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
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Threading;

namespace surveyor.vision
{
    public class SurveyorVisionClient
    {
        public const int GRAB_MONOCULAR = 0;
        public const int GRAB_MULTI_CAMERA = 1;
        
        // this defines how images will be acquired
        public int grab_mode = GRAB_MONOCULAR;
        
        // whether to show extra info
        public bool Verbose;

		// whether the service is running
		public bool Running;
		public bool Streaming;
		public bool frame_arrived;
		public bool frame_processed;
		public bool disparities_arrived;
		
		// desired frames per second
		public float fps = 5;
		
		// are we waiting for an image to be returned ?
		public bool waiting_for_reply;

        public string image_request_command = "I";
		public string processing_type_command = "g";
		public string processing_type_stereo_command = "5";
		public string processing_type_normal_command = "9";
        public string request_320_240 = "b";
        public string request_640_480 = "c";
        
        public int image_width, image_height;
        public Bitmap current_frame;
		public bool current_frame_busy;
		public bool current_frame_swapping;
		
		public bool Embedded;
		public int cam_index;  // 0 = left 1 = right
		
        #region "platform specific code"		
		
		/// <summary>
		/// Method to detect if runtime platform is a MS Windows or not 
		/// </summary>
		protected bool IsWindows()
		{
			return Path.DirectorySeparatorChar == '\\';
		}
		
        #endregion
		        
        #region "constructors"

        public SurveyorVisionClient()
        {
        }

        #endregion

        #region "sockets stuff"

        const int DATA_BUFFER_SIZE = 1024;
		public AsyncCallback m_pfnCallBack;
		public Socket m_clientSocket;
		public DateTime data_last_received;

        /// <summary>
        /// returns true if the given address is an IP address
        /// </summary>
        /// <param name="nameOrAddress">host name or IP address</param>
        /// <returns>true if the given address is an IP address</returns>
        private bool isIPAddress(string nameOrAddress)
        {
            bool is_ip = true;

            nameOrAddress = nameOrAddress.Trim();
            int i = 0;
            while ((i < nameOrAddress.Length) && (is_ip))
            {
                char c = Convert.ToChar(nameOrAddress.Substring(i, 1));
                if (!((c == '.') || ((c >= 48) && (c <= 57)))) is_ip = false;
                i++;
            }
            return (is_ip);
        }


        public void Start(string server_address, int serverPort)
		{
			Running = false;

            Console.WriteLine("Connecting client to " + server_address + ":" + serverPort.ToString());

            string serverIP = server_address;

            // get the IP address from the host name
            if (!isIPAddress(server_address))
            {
                serverIP = "";
                IPAddress[] addresslist = Dns.GetHostAddresses(server_address);
                if (addresslist != null)
                {
                    if (addresslist.Length > 0)
                    {
                        int i = 0;
                        while ((i < addresslist.Length) && (serverIP == ""))
                        {
                            if (!addresslist[i].ToString().Contains(":"))
                                serverIP = addresslist[0].ToString();
                            i++;
                        }
                        
                        if (serverIP != "")
                            Console.WriteLine(server_address + " = " + serverIP);
                    }
                }
            }

			try
			{
                // Create the socket instance
				m_clientSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );				

                m_clientSocket.ReceiveTimeout = 100;
                
				// Get the remote IP address
				IPAddress ip = IPAddress.Parse(serverIP);
				int iPortNo = System.Convert.ToInt16(serverPort);
				
                // Create the end point 
				IPEndPoint ipEnd = new IPEndPoint (ip, iPortNo);
				
                // Connect to the remote host
				m_clientSocket.Connect ( ipEnd );
				if(m_clientSocket.Connected) 
                {					
					//Wait for data asynchronously 
					WaitForData();
					
					Running = true;
				}
				else
				{
					Console.WriteLine("Not connected");
				}
			}
			catch(SocketException se)
			{
				Console.WriteLine("\nConnection failed, is the SVS running?\n" + se.Message);
			}		
		}		
		
		NetworkStream networkStream;
		System.IO.StreamWriter streamWriter;

		public void StopSend()
		{
			if (streamWriter != null)
			{
				streamWriter.Close();
			}
			if (networkStream != null)
			{
				networkStream.Close();
			}
		}
		
        /// <summary>
        /// send a message to the server
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wait_for_reply">whether to wait for a reply</param>
        private void Send(
		    string msg, 
		    bool wait_for_reply)
		{            
			try
			{
				networkStream = new NetworkStream(m_clientSocket);
				streamWriter = new System.IO.StreamWriter(networkStream);
				streamWriter.WriteLine(msg);
				streamWriter.Flush();
                waiting_for_reply = wait_for_reply;
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message);
			}
		}

        public void WaitForReply(int timeout_sec)
        {
            DateTime start_time = DateTime.Now;
            int seconds_elapsed = 0;
            while ((seconds_elapsed < timeout_sec) && (waiting_for_reply))
            {
                TimeSpan diff = DateTime.Now.Subtract(start_time);
                seconds_elapsed = (int)diff.TotalSeconds;
                System.Threading.Thread.Sleep(50);
            }
            if (seconds_elapsed >= timeout_sec)
                Console.WriteLine("Timed out waiting for SRV1 reply");
        }

        SocketPacket theSocPkt;

        /// <summary>
        /// waits for data to come back from the server
        /// </summary>
        public void WaitForData()
		{
			try
			{
				if  ( m_pfnCallBack == null ) 
				{
					m_pfnCallBack = new AsyncCallback (OnDataReceived);
				}
				if (theSocPkt == null)
				    theSocPkt = new SocketPacket ();
				theSocPkt.thisSocket = m_clientSocket;
				
				// Start listening to the data asynchronously				
				m_clientSocket.BeginReceive (theSocPkt.dataBuffer,
				                             0, theSocPkt.dataBuffer.Length,
				                             SocketFlags.None, 
				                             m_pfnCallBack, 
				                             theSocPkt);				
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message);
			}
		}

        internal class SocketPacket
		{
			public System.Net.Sockets.Socket thisSocket;
            public byte[] dataBuffer = new byte[DATA_BUFFER_SIZE];
		}


        public List<byte> received_data = new List<byte>();
        private long previous_frame_size, frame_size;
        private int previous_image_width, previous_image_height;
        private int previous_image_header_pos = -1, image_header_pos = -1;
        private int previous_disp_header_pos = -1, disp_header_pos = -1;
		private int previous_no_of_matches = -1;
        private int search_pos;
        private byte[] received_frame;
        private MemoryStream memstream;

        // possible image resolutions (identifier, width, height)
        int[] ImageResolutions = {
            0, 0,
            80, 64,
            0, 0,
            160, 128,
            0, 0,
            320, 240,
            0, 0,
            640, 480,
            0, 0,
            1280, 1024
        };

        private void ReceiveFrame(
		    List<byte> received_data, 
            int start_index, int end_index,
            long frame_size,
            int image_width, int image_height)
        {
            if ((frame_size > 0) && 
			    (received_data.Count - start_index - 100 >= frame_size)) // &&
			    //(!current_frame_busy))
            {
				current_frame_swapping = true;
                const int initial_block = 10;
                end_index = (int)(start_index + initial_block + frame_size);

                if (received_frame == null)
                    received_frame = new byte[image_width*image_height*3];
                    
                int n = 0;
                
                for (int i = start_index + initial_block; i < end_index; i++, n++)
                    received_frame[n] = received_data[i];           
                 
                try
                {
                    MemoryStream new_memstream = new MemoryStream(received_frame, 0, (int)frame_size);
                    Bitmap new_current_frame = (Bitmap) Bitmap.FromStream(new_memstream);
					if (new_current_frame != null)
					{
					    if (!current_frame_busy)
					    {						
						    if (current_frame != null) current_frame.Dispose();
						    if (memstream != null) memstream.Dispose();
						    memstream = new_memstream;
						    current_frame = new_current_frame;
                            if (Verbose) 
							    Console.WriteLine("Frame received");						
						}
						else Console.WriteLine("current_frame_busy = true");
					}
					else Console.WriteLine("new_current_frame = null");
                }
                catch (Exception ex)
                {
                    current_frame = null;
                    Console.WriteLine("Error converting to jpeg: " + ex.Message);                    
                }

                received_data.RemoveRange(0, end_index);
                image_header_pos -= end_index;
                search_pos -= end_index;
                previous_image_header_pos = image_header_pos;
	            frame_arrived = true;
				current_frame_swapping = false;
            }
            
        }

        private bool ReceiveFrame(List<byte> received_data)
        {
			bool received = false;
            data_last_received = DateTime.Now;
            while (search_pos < received_data.Count - 10)
            {
                if ((received_data[search_pos] == (byte)'#') && 
                    (received_data[search_pos + 1] == (byte)'#') &&                
                    (received_data[search_pos + 2] == (byte)'I') && 
                    (received_data[search_pos + 3] == (byte)'M') &&
                    (received_data[search_pos + 4] == (byte)'J'))
                {                
                    int image_number = Convert.ToInt32(Convert.ToString((char)received_data[search_pos + 5]));
                    
                    if ((image_number == 1) ||
                        (image_number == 3) ||
                        (image_number == 5) ||
                        (image_number == 7) ||
                        (image_number == 9))
                    {
                        previous_image_width = image_width;
                        previous_image_height = image_height;
                        image_width = ImageResolutions[image_number * 2];
                        image_height = ImageResolutions[image_number * 2 + 1];

                        // get the frame size
                        previous_frame_size = frame_size;
                        frame_size = 0;
                        for (int j = 0; j < 4; j++) 
    					    frame_size += (0xFF & received_data[search_pos + 6 + j]) << (8 * j);
    					    
    					//Console.WriteLine("Frame size: " + frame_size.ToString());
    					        					
    					image_header_pos = search_pos;
    					
    					if (previous_image_header_pos > -1)
    					{
    					    ReceiveFrame(received_data, 
    					                 previous_image_header_pos, image_header_pos, 
    					                 previous_frame_size, 
    					                 previous_image_width, 
    					                 previous_image_height);        					                 
							received = true;
    					}
    					else
    					{
    					    previous_image_header_pos = image_header_pos;
    					}
    					
                    }
                    
                }
                search_pos++;
            }
			return(received);
        }

        private void ReceiveDisparities(
		    List<byte> received_data, 
            int start_index, int end_index,
            int no_of_matches)
        {
            if (no_of_matches > 0)
            {
                const int initial_block = 9;
                end_index = (int)(start_index + initial_block + (no_of_matches * 2 * 3));

				int x,y,disp;
                int i = start_index + initial_block;
				
                while (i < end_index)
				{
					for (int v = 0; v < 3; v++)
					{
                        byte low_byte = received_data[i++];
						byte high_byte = received_data[i++];
						int val = 0;
                        if (high_byte < 0x80)
                            val = high_byte * 0x100 | low_byte;
                        else
                            val = (high_byte |0xFF00) * 0x100 | low_byte;
						switch(v)
						{
						    case 0:
						    {
							    x = val;
							    break;
						    }
						    case 1:
						    {
							    y = val;
							    break;
						    }
						    case 2:
						    {
							    disp = val;
							    break;
						    }
						}
					}
				}
                   
                received_data.RemoveRange(0, end_index);
                disp_header_pos -= end_index;
                search_pos -= end_index;
                previous_disp_header_pos = disp_header_pos;
				disparities_arrived = true;
            }
            
        }
		
        private bool ReceiveDisparities(List<byte> received_data)
        {
			bool received = false;
            data_last_received = DateTime.Now;
            while (search_pos < received_data.Count - 9)
            {
                if ((received_data[search_pos] == (byte)'#') && 
                    (received_data[search_pos + 1] == (byte)'#') &&                
                    (received_data[search_pos + 2] == (byte)'D') && 
                    (received_data[search_pos + 3] == (byte)'S') &&
                    (received_data[search_pos + 4] == (byte)'P'))
                {                    		
                    int no_of_matches = Convert.ToInt32(Convert.ToString((char)received_data[search_pos + 5]));
					
    			    disp_header_pos = search_pos;
    					
    				if (previous_disp_header_pos > -1)
    				{
    					ReceiveDisparities(
						    received_data,
    					    previous_disp_header_pos, disp_header_pos,
						    previous_no_of_matches);
						received = true;
    				}
    				else
    				{
    				    previous_disp_header_pos = disp_header_pos;
						previous_no_of_matches = no_of_matches;
    				}
    					
                }
                    
                search_pos++;
            }
			return(received);
        }
		
        private void OnDataReceived(IAsyncResult asyn)
		{
			try
			{
				SocketPacket theSockId = (SocketPacket)asyn.AsyncState ;
				int iRx  = theSockId.thisSocket.EndReceive (asyn);
				
				for (int i = 0; i < iRx; i++)
				    received_data.Add(theSockId.dataBuffer[i]);
				    
				if (Verbose)
				    Console.WriteLine(received_data.Count.ToString() + " bytes received");
				
				//Console.Write(".");
				
                ReceiveFrame(received_data);
				//ReceiveDisparities(received_data);
				WaitForData();
			}
			catch (ObjectDisposedException )
			{
				System.Diagnostics.Debugger.Log(0,"1","\nOnDataReceived: Socket has been closed\n");
			}
			catch(SocketException se)
			{
				Console.WriteLine(se.Message);
			}
		}
		
        public void Stop()
		{
			if ( m_clientSocket != null )
			{
				m_clientSocket.Close();
				m_clientSocket = null;				
			}
            
			Running = false;
		}

        #endregion

        #region "start and stop streaming"

        //SurveyorVisionThreadGrabFrame grab_frames;

        #region "callbacks"

        /// <summary>
        /// a new image has arrived
        /// </summary>
        /// <param name="state"></param>
        private void FrameGrabCallbackClient(object state)
        {            
            //SurveyorVisionClient istate = (SurveyorVisionClient)state;
			Thread.Sleep(10);
        }

        #endregion

        /// <summary>
        /// start streaming data from the camera
        /// </summary>
        public void StartStream()
        {
            Streaming = true;
            //grab_frames = new SurveyorVisionThreadGrabFrame(new WaitCallback(FrameGrabCallbackClient),this);        
            //Thread grabber_thread = new Thread(new ThreadStart(grab_frames.Execute));
            //grabber_thread.Priority = ThreadPriority.Normal;
            //grabber_thread.Start();        
        }

		/// <summary>
		/// enables onboard stereo correspondence on the SVS 
		/// </summary>
		public void EnableEmbeddedStereo()
		{
			Send(processing_type_command + processing_type_stereo_command, false);
		}

		/// <summary>
		/// disables onboard stereo correspondence on the SVS 
		/// </summary>
		public void DisableEmbeddedStereo()
		{
			Send(processing_type_command + processing_type_normal_command, false);
		}
		
        public void RequestFrame()
        {            
			if (!((Embedded) && (cam_index == 1)))
			{
				frame_arrived = false;
                Send(image_request_command, true);
			}
			else
			{
				frame_arrived = true;
			}
        }

        public void RequestResolution640x480()
        {            
            Send(request_640_480, false);
        }

        public void RequestResolution320x256()
        {            
            Send(request_320_240, false);
        }
        
        /// <summary>
        /// request the status of all devices
        /// </summary>
        public void StopStream()
        {
            Streaming = false;
            //grab_frames.Halt = true;
        }

        #endregion




		
    }
}
