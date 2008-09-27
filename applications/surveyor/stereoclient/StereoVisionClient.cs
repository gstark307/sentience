/*
    Example client object connecting to a server to receive stereo feature data
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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace surveyor.vision
{
    /// <summary>
    /// base class for receiving stereo feature data
    /// </summary>
    public class StereoVisionClient
    {
        public bool ServiceRunning;
        private bool ColourFeatures;
        public List<StereoFeature> features;

        #region "constructors"
        
        public StereoVisionClient()
        {
        }
        
        #endregion

        #region "sockets stuff"

        private const int DATA_BUFFER_SIZE = 4096;  // this should match the size on the server
        private AsyncCallback m_pfnCallBack;
        private Socket m_clientSocket;
        public DateTime LastConnectionAttempt;      // the time when we last tried to connect to a stereo feature service

        public bool IsConnected()
        {
            bool isConnected = true;
            if (m_clientSocket != null)
            {
                if (!m_clientSocket.Connected)
                {
                    isConnected = false;
                    Dissconnect();
                }
            }
            else isConnected = false;
            return (isConnected);
        }

        /// <summary>
        /// connect to a stereo feature service
        /// </summary>
        /// <param name="serverPort">port number of the stereo feature service</param>
        public void Connect(int serverPort)
        {
            Connect(GetIP(), serverPort);
        }

        /// <summary>
        /// returns true if the given address is an IP address
        /// </summary>
        /// <param name="nameOrAddress">host name or IP address</param>
        /// <returns>true if the given address is an IP address</returns>
        private bool isIPAddress(string nameOrAddress)
        {
            bool isIP = false;
            IPHostEntry hostEntry = Dns.GetHostEntry(nameOrAddress);
            if (hostEntry != null)
            {
                if (hostEntry.AddressList != null)
                {
                    if (hostEntry.AddressList.Length > 0)
                    {
                        if (hostEntry.AddressList[0].ToString() == nameOrAddress)
                            isIP = true;
                    }
                }
            }
            return (isIP);
        }

        /// <summary>
        /// connect to a stereo feature service running remotely
        /// </summary>
        /// <param name="server_address">IP address or host name of the stereo feature service</param>
        /// <param name="serverPort">port number of the stereo feature service</param>
        public void Connect(string server_address, int serverPort)
        {
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
                        serverIP = addresslist[0].ToString();
                        //Console.WriteLine(server_address + " = " + serverIP);
                    }
                }
            }

            if (serverIP != "")
            {
                ServiceRunning = false;

                Console.WriteLine("Connecting to stereo feature service " + serverIP + ":" + serverPort.ToString());

                try
                {
                    // Create the socket instance
                    m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Get the remote IP address
                    IPAddress ip = IPAddress.Parse(serverIP);
                    int iPortNo = System.Convert.ToInt16(serverPort);

                    // Create the end point 
                    IPEndPoint ipEnd = new IPEndPoint(ip, iPortNo);

                    // Connect to the remote host
                    m_clientSocket.Connect(ipEnd);
                    if (m_clientSocket.Connected)
                    {
                        //Wait for data asynchronously 
                        WaitForData();

                        Console.WriteLine("Connected");

                        ServiceRunning = true;
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine("\nConnection failed, is the stereo feature service running?\n" + se.Message);
                }
            }

            // record the time of the connection
            LastConnectionAttempt = DateTime.Now;
        }

        /// <summary>
        /// waits for data to come back from the stereo feature service
        /// </summary>
        private void WaitForData()
        {
            try
            {
                if (m_pfnCallBack == null)
                {
                    m_pfnCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.thisSocket = m_clientSocket;
                // Start listening to the data asynchronously
                m_clientSocket.BeginReceive(theSocPkt.dataBuffer,
                                             0, theSocPkt.dataBuffer.Length,
                                             SocketFlags.None,
                                             m_pfnCallBack,
                                             theSocPkt);
            }
            catch (SocketException se)
            {
                Console.WriteLine("Error whilst waiting for stereo feature service replies: " + se.Message);
            }
        }

        /// <summary>
        /// used for sockets communication with stereo feature service
        /// </summary>
        internal class SocketPacket
        {
            public System.Net.Sockets.Socket thisSocket;
            public byte[] dataBuffer = new byte[DATA_BUFFER_SIZE];
        }

        /// <summary>
        /// callback function for data received from the stereo feature service
        /// </summary>
        /// <param name="asyn"></param>
        private void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
                int iRx = theSockId.thisSocket.EndReceive(asyn);
                //Console.WriteLine("Received1 " + (iRx + 1).ToString() + " bytes");
                Receive(theSockId.dataBuffer, iRx + 1);
                Console.WriteLine("Received " + (iRx + 1).ToString() + " bytes");
                WaitForData();
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine("OnDataReceived: " + se.Message);
            }
        }

        /// <summary>
        /// dissconnects from the stereo feature service
        /// </summary>
        public void Dissconnect()
        {
            if (ServiceRunning)
            {
                if (m_clientSocket != null)
                {
                    m_clientSocket.Close();
                    m_clientSocket = null;
                }
                ServiceRunning = false;
            }
        }

        /// <summary>
        /// get the local IP address
        /// </summary>
        /// <returns></returns>
        private String GetIP()
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

            return (IPaddress);
        }
        
        #endregion
        
        #region "sending data"
        
        /// <summary>
        /// sends a message to the stereo vision server
        /// requesting that images be saved to the given path
        /// </summary>
        /// <param name="path">
        /// A <see cref="System.String"/>
        /// </param>
        public void StartRecordingImages(string path)
        {
            Send("Record " + path);
        }

        /// <summary>
        /// sends a message to the server requesting that no images be saved
        /// </summary>
        public void StopRecordingImages()
        {
            Send("Record");
        }
        
        /// <summary>
        /// send a message to the stereo vision server
        /// </summary>
        /// <param name="msg">message to be sent</param>
        protected void Send(string msg)
		{
            if (m_clientSocket != null)
            {
    			try
    			{
                    // New code to send strings
                    NetworkStream networkStream = new NetworkStream(m_clientSocket);
                    System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(networkStream);
                    streamWriter.WriteLine(msg);
                    streamWriter.Flush();
    			}
    			catch(SocketException se)
    			{
    				Console.WriteLine(se.Message);
    			}
			}
		}
                
        #endregion
        
        #region "receiving data"

        // indicates whether data is currently being received
        private bool receive_busy;
        
        /// <summary>
        /// turns a byte array into an array of floats containing position and disparity of the stereo feature
        /// </summary>
        /// <param name="stereo_data">received data</param>
        /// <param name="no_of_bytes">received number of bytes</param>
        /// <returns>float array containing stereo feature parameters</returns>
        private float[] ReadData(byte[] stereo_data, int no_of_bytes)
        {
            float[] data = new float[(no_of_bytes-1) / 4];
            for (int i = 0; i < data.Length; i++)
                data[i] = BitConverter.ToSingle(stereo_data, (i*4) + 1);
	        return(data);
        }

        /// <summary>
        /// turns a byte array into an array of floats containing position, disparity and colour of the stereo feature
        /// </summary>
        /// <param name="stereo_data">received data</param>
        /// <param name="no_of_bytes">received number of bytes</param>
        /// <param name="data">returned position and disparity as an array of floats</param>
        /// <param name="colour">returned colour for each stereo feature as an array of bytes</param>
        private void ReadDataColour(byte[] stereo_data, int no_of_bytes,
                                    ref float[] data, ref byte[] colour)
        {
            const int bytes_per_feature = 4 * 4;
            int no_of_features = no_of_bytes / bytes_per_feature;
            data = new float[no_of_features * 3];
            colour = new byte[data.Length];
            int n = 0;
            for (int i = 0; i < no_of_features; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int offset = (i * bytes_per_feature) + (4 * j) + 1;
                    data[n] = BitConverter.ToSingle(stereo_data, offset);
                    colour[n] = stereo_data[(i * bytes_per_feature) + (4 * 3) + j + 1];
                    n++;
                }
            }
        }

        /// <summary>
        /// receive incoming stereo feature data and turn it into a list of stereo feature objects
        /// </summary>
        /// <param name="data">received data</param>
        /// <param name="no_of_bytes">received number of bytes</param>
        private void Receive(byte[] data, int no_of_bytes)
        {
            if ((data.Length > 1) && (!receive_busy))
            {
                receive_busy = true;
                
                // create a list to store the features
                List<StereoFeature> new_features = new List<StereoFeature>();
                
                // read the first byte, which determines whether
                // the stereo features have colour info or not
                if (data[0] == 1)
                    ColourFeatures = true;
                else
                    ColourFeatures = false;
                
                if (!ColourFeatures)
                {
                    // stereo features with no colour information
                    float[] feats = ReadData(data, no_of_bytes);
                    for (int i = 0; i < feats.Length; i += 3)
                    {
                        StereoFeature f = 
                            new StereoFeature(feats[i], feats[i + 1], feats[i + 2]);
                        new_features.Add(f);
                    }
                
                }
                else
                {
                    // stereo features with colour information
                    float[] feats = null;
                    byte[] colour = null; 
                    ReadDataColour(data, no_of_bytes, ref feats, ref colour);
                    for (int i = 0; i < feats.Length; i += 3)
                    {
                        StereoFeature f = 
                            new StereoFeature(feats[i], feats[i + 1], feats[i + 2]);
                        f.SetColour(colour[i], colour[i + 1], colour[i + 2]);
                        new_features.Add(f);
                    }
                }
                features = new_features;
                FeaturesArrived(features);
                receive_busy = false;
            }
        }
        
        /// <summary>
        /// You can override this method and insert whatever code is needed
        /// to process the stereo features
        /// </summary>
        /// <param name="features">list of stereo features</param>
        public virtual void FeaturesArrived(List<StereoFeature> features)
        {
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i].colour != null)
                    Console.WriteLine("x: " + features[i].x.ToString() + "  y: " + features[i].y.ToString() + "  disparity: " + features[i].disparity.ToString() + "  red:" + features[i].colour[0].ToString() + "  green:" + features[i].colour[1].ToString() + "  blue:" + features[i].colour[2].ToString());
                else
                    Console.WriteLine("x: " + features[i].x.ToString() + "  y: " + features[i].y.ToString() + "  disparity: " + features[i].disparity.ToString());
            }
        }
        
        #endregion
    }
}
