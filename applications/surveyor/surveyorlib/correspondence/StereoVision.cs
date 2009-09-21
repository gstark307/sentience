/*
    base class for stereo vision
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
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using sluggish.utilities;

namespace surveyor.vision
{
    /// <summary>
    /// base class for stereo correspondence
    /// </summary>
    public class StereoVision
    {
        public BaseVisionStereo vision;

        // the type of stereo correspondence algorithm being used
        public const int SIMPLE = 0;        
		public const int DENSE = 1;
		public const int EDGES = 2;
		public const int GEOMETRIC = 3;
        public int algorithm_type = EDGES;

        // Select rows at random from which to obtain disparities
        // this helps to reduce processing time
        // If this value is set to zero then all rows of the image are considered
        public int random_rows;
    
        // stereo features detected
        public List<StereoFeature> features;
        
        /// <remarks>
        /// max disparity as a percentage of image width
        /// in the range 1-100
        /// </remarks>
        public int max_disparity = 30;        
        
        // determines the size of stereo features displayed as dots
        public float feature_scale = 0.2f;

        protected int image_width, image_height;
        protected byte[][] img = new byte[4][];        

        public StereoVision()
        {
            features = new List<StereoFeature>();
        }

        /// <summary>
        /// returns the distance for the given stereo disparity
        /// </summary>
        /// <param name="disparity_pixels">disparity in pixels</param>
        /// <param name="focal_length_mm">focal length in millimetres</param>
        /// <param name="sensor_pixels_per_mm">number of pixels per millimetre on the sensor chip</param>
        /// <param name="baseline_mm">distance between cameras in millimetres</param>
        /// <returns>range in millimetres</returns>        
        public static float DisparityToDistance(float disparity_pixels,
                                                float focal_length_mm,
                                                float sensor_pixels_per_mm,
                                                float baseline_mm)
        {
            float focal_length_pixels = focal_length_mm * sensor_pixels_per_mm;
            float distance_mm = baseline_mm * focal_length_pixels / disparity_pixels;
            return(distance_mm);
        }

        /// <summary>
        /// returns the distance for the given stereo disparity
        /// </summary>
        /// <param name="disparity_pixels">disparity in pixels</param>
        /// <param name="focal_length_pixels">focal length in pixels</param>
        /// <param name="baseline_mm">distance between cameras in millimetres</param>
        /// <returns>range in millimetres</returns>        
        public static float DisparityToDistance(float disparity_pixels,
                                                float focal_length_pixels,
                                                float baseline_mm)
        {
            float distance_mm = baseline_mm * focal_length_pixels / disparity_pixels;
            return(distance_mm);
        }
    
        /// <summary>
        /// update stereo correspondence
        /// </summary>
        /// <param name="left_bmp_colour">rectified left image data</param>
        /// <param name="right_bmp_colour">rectified right_image_data</param>
        /// <param name="left_bmp">rectified left image data</param>
        /// <param name="right_bmp">rectified right_image_data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="calibration_offset_x">offset calculated during camera calibration</param>
        /// <param name="calibration_offset_y">offset calculated during camera calibration</param>
        public virtual void Update(
            byte[] left_bmp_colour, byte[] right_bmp_colour,
		    byte[] left_bmp, byte[] right_bmp,
            int image_width, int image_height,
            float calibration_offset_x, 
            float calibration_offset_y)
        {
            this.image_width = image_width;
            this.image_height = image_height;
        }
		
		/// <summary>
		/// update the colours of stereo features
		/// </summary>
		/// <param name="left_img_colour">left colour image</param>
		/// <param name="right_img_colour">right colour image</param>
		private void UpdateFeatureColours(byte[] left_img_colour, byte[] right_img_colour)
		{
			for (int f = 0; f < features.Count; f++)
			{
				StereoFeature feature = features[f];
				int n = (((int)feature.y * image_width) + (int)feature.x) * 3;
				
				feature.colour = new byte[3];
				for (int col = 0; col < 3; col++)					
				    feature.colour[col] = left_img_colour[n+col]; 
			}
		}

        /// <summary>
        /// main update for stereo correspondence
        /// </summary>
        /// <param name="rectified_left">left image bitmap object</param>
        /// <param name="rectified_right">right image bitmap object</param>
        /// <param name="calibration_offset_x">horizontal offset from calibration, correcting for non-parallel alignment of the cameras</param>
        /// <param name="calibration_offset_y">vertical offset from calibration, correcting for non-parallel alignment of the cameras</param>
        public void Update(
            Bitmap rectified_left, 
            Bitmap rectified_right,
            float calibration_offset_x, 
            float calibration_offset_y)
        {
            image_width = rectified_left.Width;
            image_height = rectified_left.Height;
            int pixels = image_width * image_height * 3;

            if (img[0] == null)
            {
                for (int i = 0; i < 4; i++)
                    img[i] = new byte[pixels];
            }
            else
            {
                if (img[0].Length != pixels)
                {
                    for (int i = 0; i < 4; i++)
                        img[i] = new byte[pixels];
                }
            }

            BitmapArrayConversions.updatebitmap(rectified_left, img[0]);
            BitmapArrayConversions.updatebitmap(rectified_right, img[1]);

            // convert colour images to mono
            monoImage(img[0], image_width, image_height, 1, ref img[2]);
            monoImage(img[1], image_width, image_height, 1, ref img[3]);

            // main stereo correspondence routine
            Update(img[0], img[1],
                   img[2], img[3],
                   rectified_left.Width, rectified_left.Height,
                   calibration_offset_x, calibration_offset_y);

            if (BroadcastStereoFeatureColours)
            {
                // assign a colour to each feature
                UpdateFeatureColours(img[0], img[1]);
            }

            // send stereo features to connected clients
            BroadcastStereoFeatures();
        }

        /// <summary>
        /// convert the given colour image to mono
        /// </summary>
        /// <param name="img_colour">colour image data</param>
        /// <param name="img_width">image width</param>
        /// <param name="img_height">image height</param>
        /// <param name="conversion_type">method for converting to mono</param>
        protected void monoImage(byte[] img_colour, int img_width, int img_height,
                                 int conversion_type, ref byte[] output)
        {
            byte[] mono_image = null;
            if (output == null)
            {
                mono_image = new byte[img_width * img_height];
            }
            else
            {
                if (output.Length == img_width * img_height)
                    mono_image = output;
                else
                    mono_image = new byte[img_width * img_height];
            }

            if (img_colour.Length == mono_image.Length)
            {
                for (int i = 0; i < mono_image.Length; i++)
                    mono_image[i] = img_colour[i];
            }
            else
            {
                int n = 0;
                short tot = 0;
                float luminence = 0;

                for (int i = 0; i < img_width * img_height * 3; i += 3)
                {
                    switch (conversion_type)
                    {
                        case 0: // magnitude
                            {
                                tot = 0;
                                for (int col = 0; col < 3; col++)
                                {
                                    tot += img_colour[i + col];
                                }
                                mono_image[n] = (byte)(tot * 0.3333f);
                                break;
                            }
                        case 1: // luminance
                            {
                                luminence = ((img_colour[i + 2] * 299) +
                                             (img_colour[i + 1] * 587) +
                                             (img_colour[i] * 114)) * 0.001f;
                                //if (luminence > 255) luminence = 255;
                                mono_image[n] = (byte)luminence;
                                break;
                            }
                        case 2: // hue
                            {
                                int r = img_colour[i + 2];
                                int g = img_colour[i + 1];
                                int b = img_colour[i];
                                
                                int hue = 0;
                                int min = 255;
                                if ((r < g) && (r < b)) min = r;
                                if ((g < r) && (g < b)) min = g;
                                if ((b < g) && (b < r)) min = b;
                                
                                if ((r > g) && (r > b) && (r - min > 0))
                                    hue = (60 * (g - b) / (r - min)) % 360; 
                                if ((b > g) && (b > r) && (b - min > 0))
                                    hue = (60 * (b - r) / (b - min)) + 120;
                                if ((g > b) && (g > r) && (g - min > 0))
                                    hue = (60 * (r - g) / (g - min)) + 240;
                                hue = hue * 255 / 360;
                                
                                mono_image[n] = (byte)hue;                            
                                break;
                            }
                    }
                    n++;
                }
            }
            output = mono_image;
        }
        
        /// <summary>
        /// show results
        /// </summary>
        /// <param name="left_bmp">left image data</param>
        /// <param name="image_width">width of the image</param>
        /// <param name="image_height">height of the image</param>
        /// <param name="output_bmp">image to be displayed</param>
        public virtual void Show(byte[] left_bmp, int image_width, int image_height,
                                 byte[] output_bmp)
        {
            if (left_bmp.Length == output_bmp.Length)
            {
                Buffer.BlockCopy(left_bmp, 0, output_bmp, 0, left_bmp.Length);
            }
            else
            {
                if (left_bmp.Length == output_bmp.Length/3)
                {
                    int n = 0;
                    for (int i = 0; i < output_bmp.Length; i+=3, n++)
                    {
                        for (int col=0;col<3;col++)
                            output_bmp[i+col] = left_bmp[n];
                    }
                    
                }
            }
            
            for (int i = 0; i < features.Count; i++)
            {
                StereoFeature f = features[i];
                drawing.drawSpotBlended(output_bmp, image_width, image_height, (int)f.x, (int)f.y, (int)(f.disparity*feature_scale), 100, 255, 100);
            }
        }
        
        /// <summary>
        /// show stereo features or depth map
        /// </summary>
        /// <param name="output">bitmap object into which to insert the image</param>
        public virtual void Show(ref Bitmap output)
        {
            byte[] output_img = new byte[image_width * image_height  * 3];
            if (output == null)
                output = new Bitmap(image_width, image_height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Show(img[0], image_width, image_height, output_img);            
            BitmapArrayConversions.updatebitmap_unsafe(output_img, output);
        }
        
        
        #region "broadcasting stereo features to other applications"

        // include colour information when broadcasting
        protected bool BroadcastStereoFeatureColours;
        
        // max number of features to broadcast to other applications
        public int maximum_broadcast_features = 200;

        [StructLayout(LayoutKind.Sequential)]
        public struct BroadcastStereoFeatureColour
        {
	        public float x;         // 4 bytes
	        public float y;         // 4 bytes
	        public float disparity; // 4 bytes
	        public byte r, g, b;    // 3 bytes
            public byte pack;       // 1 byte
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BroadcastStereoFeature
        {
	        public float x;         // 4 bytes
	        public float y;         // 4 bytes
	        public float disparity; // 4 bytes
        }

        /// <summary>
        /// serialize any object
        /// </summary>
        /// <param name="anything"></param>
        /// <returns>byte array containing serialized data</returns>
        private byte[] RawSerialize(object anything)
        {
        	int rawsize = Marshal.SizeOf(anything);
        	byte[] rawdata = new byte[rawsize];
        	GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
        	Marshal.StructureToPtr(anything, handle.AddrOfPinnedObject(), false);
        	handle.Free();
        	return(rawdata);
        }
        
        /// <summary>
        /// returns stereo feature data as a byte array suitable for broadcasting
        /// </summary>
        /// <returns>byte array containing stereo feature data</returns>
        private byte[] SerializedStereoFeatures()
        {
            byte[] serialized = null;
            
            if (features != null)
            {
                int n = 0;

                // limit the maximum number of features broadcast
                int max = features.Count;
                if (max > maximum_broadcast_features) max = maximum_broadcast_features;
                Random rnd = new Random();

                if (BroadcastStereoFeatureColours)
                {
                    // include colour data in the broadcast
                    for (int i = 0; i < max; i++)
                    {
                        int ii = i;
                        if (features.Count > maximum_broadcast_features)
                            ii = rnd.Next(features.Count-1);

                        BroadcastStereoFeatureColour data = new BroadcastStereoFeatureColour();
                        data.x = features[ii].x;
                        data.y = features[ii].y;
                        data.disparity = features[ii].disparity;
                        data.r = features[ii].colour[0];
                        data.g = features[ii].colour[1];
                        data.b = features[ii].colour[2];
                        byte[] serial_data = RawSerialize(data);
                        if (serialized == null)
                        {
                            serialized = new byte[(max * serial_data.Length) + 1];
                            serialized[n++] = 1; // first byte indicates colour info
                        }
                        for (int j = 0; j < serial_data.Length; j++, n++)
                            serialized[n] = serial_data[j];
                    }
                    
                }
                else
                {
                    // broadcast only the basics for each stereo feature
                    for (int i = 0; i < max; i++)
                    {
                        int ii = i;
                        if (features.Count > maximum_broadcast_features)
                            ii = rnd.Next(features.Count-1);

                        BroadcastStereoFeature data = new BroadcastStereoFeature();
                        data.x = features[ii].x;
                        data.y = features[ii].y;
                        data.disparity = features[ii].disparity;
                        byte[] serial_data = RawSerialize(data);
                        if (serialized == null)
                        {
                            serialized = new byte[(max * serial_data.Length) + 1];
                            serialized[n++] = 0;  // first byte indicates no colour info
                        }
                        for (int j = 0; j < serial_data.Length; j++, n++)
                            serialized[n] = serial_data[j];
                    }
                }
            }
            return(serialized);
        }

        #region "sockets stuff"

        private const int DATA_BUFFER_SIZE = 4096;

        private delegate void UpdateClientListCallback();
        private AsyncCallback pfnWorkerCallBack;
        private Socket m_mainSocket;
        public bool ServiceRunning;
        private ArrayList m_workerSocketList = ArrayList.Synchronized(new System.Collections.ArrayList()); 

        // total number of clients connected
        private int m_clientCount = 0;

        /// <summary>
        /// start the service listening for clients on the given port number
        /// </summary>
        /// <param name="PortNumber">port number</param>		
        public bool StartService(int PortNumber)
        {
            if (PortNumber > -1)
            {
                if (PortNumber >= 1024)
                {
    			    string IPaddress = GetIP();
    			    return(StartService(IPaddress, PortNumber));
    			}
    			else
    			{
    			    Console.WriteLine("broadcast port should be >= 1024");
    			    return(false);		
                }
            }
            else return (false);
		}
		
        /// <summary>
        /// start the service listening for clients on the given port number
        /// </summary>
        /// <param name="IPaddress">IP address to listen on</param>		
        /// <param name="PortNumber">port number</param>		
        public bool StartService(
            string IPaddress, 
            int PortNumber)
        {
            bool success = true;
            ServiceRunning = false;

            if (PortNumber > -1)
            {
    			if (PortNumber < 1024)
    			{
    				Console.WriteLine("Port number is too low.  Try something >= 1024");
    			}
    			else
    			{
                    m_workerSocketList = ArrayList.Synchronized(new System.Collections.ArrayList()); 
                    
                    try
    	            {
    	                // Create the listening socket...
    	                m_mainSocket = new Socket(AddressFamily.InterNetwork,
    	                    SocketType.Stream,
    	                    ProtocolType.Tcp);
    	                IPEndPoint ipLocal = new IPEndPoint(IPAddress.Parse(IPaddress), PortNumber);
    
    	                Console.WriteLine("Stereo feature broadcast running on " + ipLocal.ToString());
    
    	                // Bind to local IP Address...
    	                m_mainSocket.Bind(ipLocal);
    					
    	                // Start listening...
    	                m_mainSocket.Listen(4);
    					
    	                // Create the call back for any client connections...
    	                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
    
    	                ServiceRunning = true;
    	            }
    	            catch (SocketException se)
    	            {
    	                success = false;
    	                Console.WriteLine("Failed to start stereo feature service: " + se.Message);
    					Console.WriteLine("Check your firewall or iptables");
    					Console.WriteLine("Try:  sudo iptables -A INPUT -p tcp --dport " + PortNumber.ToString() + " -j ACCEPT");
    	            }
    			}
            }
            return (success);
        }

        /// <summary>
        /// returns the number of connected client applications
        /// </summary>
        /// <returns>number of clients</returns>
        public int GetNoOfClients()
        {
            if (m_workerSocketList == null)
                return (0);
            else
                return (m_workerSocketList.Count);
        }

        /// <summary>
        /// This is the call back function, which will be invoked when a client is connected 
        /// </summary>
        /// <param name="asyn"></param>
        private void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                // Here we complete/end the BeginAccept() asynchronous call
                // by calling EndAccept() - which returns the reference to
                // a new Socket object
                Socket workerSocket = m_mainSocket.EndAccept(asyn);

                // Now increment the client count for this client 
                // in a thread safe manner
                Interlocked.Increment(ref m_clientCount);

                // Add the workerSocket reference to our ArrayList
                m_workerSocketList.Add(workerSocket);

                // Let the worker Socket do the further processing for the 
                // just connected client
                WaitForData(workerSocket, m_clientCount);

                // Since the main Socket is now free, it can go back and wait for
                // other clients who are attempting to connect
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);

                Console.WriteLine("Client number " + m_clientCount.ToString()  + " connected at " + DateTime.Now.ToString());
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }

        }

        internal class SocketPacket
        {
            // Constructor which takes a Socket and a client number
            public SocketPacket(System.Net.Sockets.Socket socket, int clientNumber)
            {
                m_currentSocket = socket;
                m_clientNumber = clientNumber;
            }

            public System.Net.Sockets.Socket m_currentSocket;

            public int m_clientNumber;

            // Buffer to store the data sent by the client
            public byte[] dataBuffer = new byte[DATA_BUFFER_SIZE];
        }

        /// <summary>
        /// Start waiting for data from the client
        /// </summary>
        /// <param name="soc"></param>
        /// <param name="clientNumber"></param>
        private void WaitForData(System.Net.Sockets.Socket soc, int clientNumber)
        {			
            try
            {
                if (pfnWorkerCallBack == null)
                {
                    // Specify the call back function which is to be 
                    // invoked when there is any write activity by the 
                    // connected client
                    pfnWorkerCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket(soc, clientNumber);

                soc.BeginReceive(theSocPkt.dataBuffer, 0,
                    theSocPkt.dataBuffer.Length,
                    SocketFlags.None,
                    pfnWorkerCallBack,
                    theSocPkt);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }
		
        /// <summary>
        /// This the call back function which will be invoked when the socket
        /// detects any client writing of data on the stream
        /// </summary>
        /// <param name="asyn"></param>
        private void OnDataReceived(IAsyncResult asyn)
        {
            SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
            int iRx = 0;
            bool success = false;
            try
            {
                iRx = theSockId.m_currentSocket.EndReceive(asyn);
                success = true;
            }
            catch
            {
            }

            if (success)
            {

                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                d.GetChars(theSockId.dataBuffer, 0, iRx, chars, 0);
                System.String szData = new System.String(chars);

                // switch the recording of images on or off
                if (szData.StartsWith("Record"))
                {
                    bool valid_path = false;
                    if (szData.Contains(" "))
                    {
                        string[] parts = szData.Split(' ');
                        if (parts.Length > 1)
                        {
                            if ((parts[1].ToLower() != "false") &&
                                (parts[1].ToLower() != "off"))
                            {
                                string directory_str = "";
                                char[] ch = parts[1].ToCharArray();
                                for (int i = 0; i < ch.Length; i++)
                                {
                                    if ((ch[i] != 10) &&
                                        (ch[i] != 13) &&
                                        (ch[i] != 0))
                                        directory_str += ch[i];
                                }

                                if (directory_str.Contains("/"))
                                {
                                    if (!directory_str.EndsWith("/"))
                                        directory_str += "/";
                                }

                                if (Directory.Exists(directory_str))
                                {
                                    // start recording
                                    if (vision != null)
                                    {
                                        Console.WriteLine("Start recording images in " + parts[1]);
                                        vision.Record = true;
                                        vision.recorded_images_path = directory_str;
                                    }
                                    else Console.WriteLine("No vision object");
                                    valid_path = true;

                                }
                                else Console.WriteLine("Directory " + directory_str + " not found");
                            }
                        }
                    }

                    if (!valid_path)
                    {
                        // stop recording
                        Console.WriteLine("Stop recording images");
                        if (vision != null) vision.Record = false;
                    }
                }
            }

            WaitForData(theSockId.m_currentSocket, theSockId.m_clientNumber);

        }

        /// <summary>
        /// sends stereo features to all connected clients
        /// </summary>
        protected void BroadcastStereoFeatures()
        {
            if (m_workerSocketList != null)
            {            
                if (m_workerSocketList.Count > 0)
                {                
                    byte[] StereoData = SerializedStereoFeatures();  
                    if (StereoData != null)
                    {                  
	                    try
	                    {                    
	                        Socket workerSocket = null;
	                        for (int i = m_workerSocketList.Count-1; i >= 0; i--)
	                        {
	                            workerSocket = (Socket)m_workerSocketList[i];
	                            if (workerSocket != null)
	                            {
	                                if (workerSocket.Connected)
	                                {
	                                    //Console.WriteLine("sending " + StereoData.Length.ToString() + " bytes");
	                                    workerSocket.Send(StereoData);
	                                }
	                                else
	                                {
	                                    Console.WriteLine("Client " + (i+1).ToString() + " disconnected");
	                                    workerSocket.Close();
	                                    m_workerSocketList.RemoveAt(i);
	                                }
	                            }
	                        }
	                    }
	                    catch (SocketException se)
	                    {
	                        Console.WriteLine("Error broadcasting stereo feature data to all clients: " + se.Message);
	                    }
                    }
                }
            }
        }

        public void StopService()
        {
            CloseSockets();
            ServiceRunning = false;
        }

        /// <summary>
        /// get the local IP address
        /// </summary>
        /// <returns></returns>
        private string GetIP()
        {
			string[] localIP = new string[3];
			localIP[0] = "";  // this slot is for 127.x.x.x addresses
			localIP[1] = "";  // this slot is for 192.168.x.x addresses
			localIP[2] = "";  // this is for any other address
			
            String strHostName = Dns.GetHostName();

            // Find host by name
            IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);

            // Grab the first IP addresses
			int i = 0;
			while ((i < iphostentry.AddressList.Length) && (localIP[2] == ""))
            {
			    IPAddress ipaddress = iphostentry.AddressList[i];
                string IPStr = ipaddress.ToString();
                if (!IPStr.Contains(":"))
                {
                    if (!IPStr.StartsWith("0."))
                    {
                        if (IPStr.StartsWith("127."))
                        {
                            localIP[0] = IPStr;
                        }
                        else
                        {
                            if (IPStr.StartsWith("192."))
                                localIP[1] = IPStr;
                            else
                                localIP[2] = IPStr;
                        }
                    }
                }
				i++;
            }
			
			// pick the widest available network
			string IPaddress = "";
			i = 2;
			while ((i >= 0) && (IPaddress == ""))
			{
				if (localIP[i] != "") IPaddress = localIP[i];
				i--;
			}
			
            return(IPaddress);
        }

        /// <summary>
        /// closes all sockets
        /// </summary>
        private void CloseSockets()
        {
            if (m_mainSocket != null)
            {
                m_mainSocket.Close();
            }
            Socket workerSocket = null;
            for (int i = 0; i < m_workerSocketList.Count; i++)
            {
                workerSocket = (Socket)m_workerSocketList[i];
                if (workerSocket != null)
                {
                    workerSocket.Close();
                    workerSocket = null;
                }
            }
        }

        /// <summary>
        /// returns a list of connected clients
        /// </summary>
        /// <returns>list of clients</returns>
        public string[] GetClientList()
        {
            string[] clients = null;
            if (m_workerSocketList.Count > 0)
            {
                ArrayList connected_clients = new ArrayList();
                for (int i = 0; i < m_workerSocketList.Count; i++)
                {
                    string clientKey = Convert.ToString(i + 1);
                    Socket workerSocket = (Socket)m_workerSocketList[i];
                    if (workerSocket != null)
                    {
                        if (workerSocket.Connected)
                        {
                            connected_clients.Add(clientKey);
                        }
                    }
                }
                if (connected_clients.Count > 0)
                {
                    clients = new string[connected_clients.Count];
                    for (int i = 0; i < connected_clients.Count; i++)
                        clients[i] = (string)connected_clients[i];
                }
            }
            return (clients);
        }

        #endregion
        
        #region "platform specific code"

        /// <summary>
        /// Method to detect if runtime platform is a MS Windows or not 
        /// </summary>
        protected bool IsWindows()
        {
            return Path.DirectorySeparatorChar == '\\';
        }

        #endregion        
        
        #endregion
        
        
    }
}
