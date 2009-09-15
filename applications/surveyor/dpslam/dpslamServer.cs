/*
    SLAM server
    Copyright (C) 2009 Bob Mottram
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
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using dpslam.core.tests;

namespace dpslam.core
{
    public class dpslamServer
    {
        public bool kill;
		public bool Running;
		
        // list of client numbers from which data is currently being received
        protected List<int> receiving_data = new List<int>();		
		
		protected const int DATA_BUFFER_SIZE = 4096 * 2;
		
		// the type of xml encoding used
        public const string XML_ENCODING = "ISO-8859-1";		
		
		// recognised xml node types
        public const string STATUS_REQUEST = "HardwareDeviceStatusRequest";
        public const string STATUS_REPLY = "HardwareDeviceStatus";
        public const string STATUS_UPDATE = "HardwareDeviceUpdate";
        public const string STATUS_BROADCAST = "HardwareDeviceBroadcast";
        public const string STATUS_DISCONNECT = "HardwareDeviceDisconnect";
		
		public int PortNumber;
        public ProtocolType protocol = ProtocolType.Tcp;                
        protected bool NoDelay = false;		// used to disable Nagle's algorithm
		
        // timeouts
		public int ReceiveTimeoutMilliseconds = 5000;
		public int SendTimeoutMilliseconds = 5000;

        // list of clients pending disconnection				
		private List<int> disconnect_client;
		
		robot rob;
				    
        #region "constructors"

        public dpslamServer(int no_of_stereo_cameras)
        {
			rob = new robot(no_of_stereo_cameras);
			
			dpslam_tests.CreateSim();
        }

        #endregion
        
        #region "buffer storing data recently received"
        
		// a buffer used to store the data recently received for
		// debugging purposes
		const int MAX_RECENT_DATA = 10;
		private List<int> data_recently_received_client_number;
		private List<string> data_recently_received;
		
		/// <summary>
		/// updates the buffer storing recently received data
		/// This is typically used for debugging purposes
		/// </summary>
		/// <param name="client_number">client number which teh data was received from</param>
		/// <param name="data_received">data content</param>
		private static void UpdateDataRecentlyReceived(
		    int client_number, 
		    string data_received,
		    ref List<string> data_recently_received, 
		    ref List<int> data_recently_received_client_number)
		{
		    // create lists
		    if (data_recently_received_client_number == null)
		    {
		        data_recently_received_client_number = new List<int>();
		        data_recently_received = new List<string>();
		    }
		    
		    // store the receipt
		    data_recently_received_client_number.Add(client_number);
		    data_recently_received.Add(data_received);
		    
		    //Console.WriteLine("Data received: " + data_recently_received.Count.ToString());
		    //Console.WriteLine("Data received: " + data_received);
		    
		    // only store a limited number of recent receipts
		    if (data_recently_received.Count >= MAX_RECENT_DATA)
		    {
		        data_recently_received.RemoveAt(0);
		        data_recently_received_client_number.RemoveAt(0);
		    }
		}
		
		/// <summary>
		/// clears the recently received data buffer
		/// </summary>
		public void ClearDataRecentlyReceived()
		{
		    if (data_recently_received != null)
		    {
		        data_recently_received.Clear();
		        data_recently_received_client_number.Clear();
		    }
		}
		
		/// <summary>
		/// Returns data recently received from teh given client number
		/// This is typically used for debugging purposes
		/// </summary>
		/// <param name="client_number">client number from which the data was received</param>
		/// <returns>data received, or empty string</returns>
		public string GetDataRecentlyReceived(int client_number)
		{
		    string data = "";
		    
		    if (data_recently_received != null)
		    {
		        int i = data_recently_received.Count-1;
		        while ((i >= 0) && (data == ""))
		        {
		            if (data_recently_received_client_number[i] == client_number)
		                data = data_recently_received[i];
		            i--;
		        }
		    }
		    
		    return(data);
		}
		
		
                
        #endregion

        #region "sockets stuff"

        public delegate void UpdateRichEditCallback(string text);
		public delegate void UpdateClientListCallback();
				
		public AsyncCallback pfnWorkerCallBack;
		private  Socket m_mainSocket;

		// An ArrayList is used to keep track of worker sockets that are designed
		// to communicate with each connected client. Make it a synchronized ArrayList
		// For thread safety
		private ArrayList m_workerSocketList = 
				ArrayList.Synchronized(new System.Collections.ArrayList());

		// The following variable will keep track of the cumulative 
		// total number of clients connected at any time. Since multiple threads
		// can access this variable, modifying this variable should be done
		// in a thread safe manner
		private int m_clientCount = 0;

		/// <summary>
		/// start the server listening on the given port number
		/// </summary>
		/// <param name="PortNumber">port number</param>		
		public void Start(int PortNumber)
		{
			Running = false;
			this.PortNumber = PortNumber;
			
			try
			{				
                // Create the listening socket...
				m_mainSocket = new Socket(AddressFamily.InterNetwork, 
					SocketType.Stream, 
					protocol);
			    
			    m_mainSocket.NoDelay = NoDelay;
					
                IPEndPoint ipLocal = new IPEndPoint(IPAddress.Parse(GetIP()), PortNumber);

                Console.WriteLine("Server running on " + ipLocal.ToString());

                // Bind to local IP Address...
				m_mainSocket.Bind( ipLocal );
				
                // Start listening...
				m_mainSocket.Listen(4);
				
                // Create the call back for any client connections...
				m_mainSocket.BeginAccept(new AsyncCallback (OnClientConnect), null);
				//m_mainSocket.BeginDisconnect(new AsyncCallback (OnClientDisconnect), null);
				
				Running = true;
			}
			catch(SocketException se)
			{
				Console.WriteLine("dpslamServer/Start(" + PortNumber.ToString() + ")/" + se.Message);
			}

		}

        /// <summary>
        /// This is the call back function, which will be invoked when a client is disconnected 
        /// </summary>
        /// <param name="asyn"></param>
        private static void OnClientDisconnect(IAsyncResult asyn)
		{
			Console.WriteLine("Client disconnected");
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
				Socket workerSocket = m_mainSocket.EndAccept (asyn);
				
				workerSocket.NoDelay = NoDelay;

				// Now increment the client count for this client 
				// in a thread safe manner
				Interlocked.Increment(ref m_clientCount);
			
			    // set timeouts
			    workerSocket.ReceiveTimeout = ReceiveTimeoutMilliseconds;
				workerSocket.SendTimeout = SendTimeoutMilliseconds;				
				
				// Add the workerSocket reference to our ArrayList
				m_workerSocketList.Add(workerSocket);

				// Send a welcome message to client
				Console.WriteLine("Welcome client " + m_clientCount);
                //msg += getDeviceStatusAll();
				//SendToClient(msg, m_clientCount);

				// Let the worker Socket do the further processing for the 
				// just connected client
				WaitForData(workerSocket, m_clientCount);
							
				// Since the main Socket is now free, it can go back and wait for
				// other clients who are attempting to connect
				m_mainSocket.BeginAccept(new AsyncCallback ( OnClientConnect ),null);				
			}
			catch(ObjectDisposedException)
			{
				Console.WriteLine("dpslamServer/OnClientConnect/Socket has been closed");
				System.Diagnostics.Debugger.Log(0,"1","\n OnClientConnection: Socket (" + PortNumber.ToString() + ") has been closed\n");
			}
			catch(SocketException se)
			{
				Console.WriteLine("dpslamServer/OnClientConnect(" + PortNumber.ToString() + ")/" + se.Message);
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

		// Start waiting for data from the client
		private void WaitForData(System.Net.Sockets.Socket soc, int clientNumber)
		{
			try
			{
				if  ( pfnWorkerCallBack == null )
				{		
					// Specify the call back function which is to be 
					// invoked when there is any write activity by the 
					// connected client
					pfnWorkerCallBack = new AsyncCallback (OnDataReceived);
				}
				SocketPacket theSocPkt = new SocketPacket (soc, clientNumber);
				
				soc.BeginReceive (theSocPkt.dataBuffer, 0, 
					theSocPkt.dataBuffer.Length,
					SocketFlags.None,
					pfnWorkerCallBack,
					theSocPkt);
			}
			catch(SocketException se)
			{
				Console.WriteLine("dpslamServer/WaitForData(" + PortNumber.ToString() + ")/" + se.Message);
			}
		}

        private ArrayList receive_buffer;
        
        public static bool EndOfReceive(string received_text)
        {
            bool end_of_data = false;
            received_text = received_text.Trim();
            if ((received_text.Contains("</" + STATUS_REQUEST + ">")) ||
                (received_text.Contains("</" + STATUS_UPDATE + ">")) ||
                (received_text.Contains("</" + STATUS_DISCONNECT + ">")))
            {
                end_of_data = true;
            }
            return(end_of_data);
        }        
        
        List<int> disconnect_now = new List<int>();
        public bool processing_receive_buffer;

        public void ProcessReceiveBuffer(
            int client_number,
            ArrayList receive_buffer)
        {
            processing_receive_buffer = true;
            
            dpslamServer.ProcessReceiveBuffer(
                client_number,
                receive_buffer,
                ref data_recently_received, 
                ref data_recently_received_client_number,
                ref m_workerSocketList,
                ref kill,
                ref disconnect_client,
                ref disconnect_now);
            processing_receive_buffer = false;
        }

        /// <summary>
        /// if the received text contains multiple xml documents
        /// this splits it up ready for subsequent parsing
        /// </summary>
        /// <param name="received_text">text received</param>
        /// <returns>list containing xml documents</returns>        
        public static List<string> SplitReceive(string received_text)
        {
            List<string> receipts = new List<string>();
            
            int prev_pos = 0;
            int start_pos, pos = 1;
            while (pos > -1)
            {
                pos = received_text.IndexOf("<?xml", prev_pos);
                if (pos > -1)
                {
                    start_pos = prev_pos;
                    if (start_pos > 0) start_pos--;
                    string xml_str = received_text.Substring(start_pos, pos - start_pos);
                    if (xml_str.Trim() != "") receipts.Add(xml_str);
                    prev_pos = pos+1;
                }
            }
            start_pos = prev_pos;
            if (start_pos > 0) start_pos--;
            receipts.Add(received_text.Substring(start_pos, received_text.Length - start_pos));
            
            return(receipts);
        }		
		
        public static void ProcessReceiveBuffer(
            int client_number,
            ArrayList receive_buffer,
            ref List<string> data_recently_received, 
            ref List<int> data_recently_received_client_number,
            ref ArrayList m_workerSocketList,
            ref bool kill,
            ref List<int> disconnect_client,
            ref List<int> disconnect_now)
        {
            if (receive_buffer != null)
            {
                string data = "";
                List<int> removals = new List<int>();
                for (int i = 0; i < receive_buffer.Count; i += 2)
                {
                    int client_no = (int)receive_buffer[i + 1];
                    if (client_no == client_number)
                    {
                        data += (string)receive_buffer[i];
                        removals.Add(i);
                    }
                }
                
                if (data != "")
                {
                    //Console.WriteLine("data = " + data);
                    
                    List<string> data_str = dpslamServer.SplitReceive(data);
                
                    for (int i = 0; i < data_str.Count; i++)
                    {
	                    ReceiveXmlMessageFromClient(
	                        data_str[i], 
	                        client_number,
	                        ref data_recently_received, 
	                        ref data_recently_received_client_number,
	                        ref m_workerSocketList,
	                        ref kill,
	                        ref disconnect_client,
	                        ref disconnect_now);
                    }
                    
                    for (int i = removals.Count-1; i >= 0; i--)
                    {
                        receive_buffer.RemoveAt(removals[i] + 1);
                        receive_buffer.RemoveAt(removals[i]);
                    }
                }
                else
                {
                    Console.WriteLine("ProcessReceiveBuffer/No data received");
                }                
            }
            else
            {
                Console.WriteLine("Receive buffer is null");
            }
        }
        
        /// <summary>
        /// a thread has been created to process incoming requests
        /// </summary>
        /// <param name="state"></param>
        private void OnDataReceivedCallback(object state)
        {
        }
        
        /// <summary>
        /// This the call back function which will be invoked when the socket
        /// detects any client writing of data on the stream
        /// </summary>
        /// <param name="asyn"></param>
        public void OnDataReceived(IAsyncResult asyn)
		{
		    SocketPacket socketData = (SocketPacket)asyn.AsyncState ;
		    
		    if (!receiving_data.Contains(socketData.m_clientNumber))
		    {		    
			    receiving_data.Add(socketData.m_clientNumber);		
				
				try
				{
					// Complete the BeginReceive() asynchronous call by EndReceive() method
					// which will return the number of characters written to the stream 
					// by the client
					int iRx  = socketData.m_currentSocket.EndReceive (asyn);
					char[] chars = new char[iRx +  1];
					
					// Extract the characters as a buffer
					System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
					d.GetChars(socketData.dataBuffer, 0, iRx, chars, 0);
	
	                if (chars.Length > 1)
	                {	                	                
	                    string szData = "";
	                    for (int ch = 0; ch < chars.Length; ch++)
	                    {
	                        if (chars[ch] != 0) szData += chars[ch];
	                    }
	
	                    // add the data to the receive buffer
	                    if (receive_buffer == null)
	                    {
	                        receive_buffer = new ArrayList();

                            // create a thread which will process incoming receipts
                            // in an organised fashion	                        	                        
                            ThreadServerReceive receive = new ThreadServerReceive(new WaitCallback(OnDataReceivedCallback), this, receive_buffer);
                            Thread receive_thread = new Thread(new ThreadStart(receive.Execute));
                            receive_thread.Priority = ThreadPriority.Normal;
                            receive_thread.Start();
                        }
	                    
	                    // push data into the receive buffer	                    	                    	                    	                    }
	                    receive_buffer.Add(szData);
	                    receive_buffer.Add(socketData.m_clientNumber);
	                    	
	                }
	
					// Continue the waiting for data on the Socket
					if (!disconnect_now.Contains(socketData.m_clientNumber))
					{
					    WaitForData(socketData.m_currentSocket, socketData.m_clientNumber );
					}
					else
					{
					    disconnect_now.Remove(socketData.m_clientNumber);
					}
				}
				catch (ObjectDisposedException )
				{
					System.Diagnostics.Debugger.Log(0,"1","\nOnDataReceived: Socket has been closed\n");
				}
				catch(SocketException se)
				{
					if(se.ErrorCode == 10054) // Error code for Connection reset by peer
					{	
						string msg = "Goodbye client " + socketData.m_clientNumber.ToString();
						Console.WriteLine(msg);
	
						// Remove the reference to the worker socket of the closed client
						// so that this object will get garbage collected
                        int index = socketData.m_clientNumber - 1;
                        if ((index > -1) && (index < m_workerSocketList.Count)) m_workerSocketList[index] = null;
					}
					else
					{
						Console.WriteLine("dpslamServer/OnDataReceived(" + PortNumber.ToString() + ")/" + se.Message);
					}
				}
				
				receiving_data.Remove(socketData.m_clientNumber);
			}
			else
			{
			    Console.WriteLine("Receive conflict: Data already being received from client " + socketData.m_clientNumber.ToString());
				disconnect_client.Add(socketData.m_clientNumber);
			}
		}
		
		/// <summary>
		/// broadcast a set of devices and their properties
		/// </summary>
		/// <param name="broadcast_devices">list containing device Ids and property names</param>
		/// <param name="quiet">report the boradcast xml to the console or not</param>
        public void Broadcast(
            ArrayList broadcast_devices,
            bool quiet)
        {
            if (broadcast_devices.Count > 0)
            {
                // get the changed state information as xml
                XmlDocument doc = GetDeviceStatus(broadcast_devices, STATUS_BROADCAST);
                string statusStr = doc.InnerXml;

                // send the xml to connected clients
                Send(statusStr);

                if (!quiet)
                {
                    Console.WriteLine("Broadcasting:");
                    Console.WriteLine(statusStr);
                }
            }
        }

        /// <summary>
        /// safely remove a connected client
        /// </summary>
        /// <param name="clientnumber">index number of the client to be removed</param>
        /// <param name="m_workerSocketList">list of open sockets</param>
        /// <param name="disconnect_client">list if client numbers to be disconnected</param>
        /// <param name="usage">usage model</param>
        protected static void RemoveClient(
            int clientnumber, 
            ArrayList m_workerSocketList,
            List<int> disconnect_client)
        {
            if ((clientnumber - 1 > -1) && (clientnumber - 1 < m_workerSocketList.Count))
            {
                Socket workerSocket = (Socket)m_workerSocketList[clientnumber - 1];
                if (workerSocket != null)
                {                
                    workerSocket.BeginDisconnect(true, new AsyncCallback(OnClientDisconnect), null);
                    m_workerSocketList.RemoveAt(clientnumber - 1);
                }
            }
            
            if (disconnect_client != null)
                if (disconnect_client.Contains(clientnumber)) 
                    disconnect_client.Remove(clientnumber);
        }
        
        /// <summary>
        /// returns the number of connected clients
        /// </summary>
        /// <returns>number of connected clients</returns>
        public int GetNoOfConnectedClients()
        {
            if (m_workerSocketList != null)
                return(m_workerSocketList.Count);
            else
                return(0);
        }

        // list of client numbers currently sending data
        protected List<int> sending_data = new List<int>();

        /// <summary>
        /// sends a message to all connected clients
        /// </summary>
        /// <param name="msg"></param>
		public void Send(string msg)
		{		    
		    Socket workerSocket = null;
		    		    
			//msg = "dpslamServer: " + msg + "\n";
			byte[] byData = System.Text.Encoding.ASCII.GetBytes(msg);				
			for(int i = m_workerSocketList.Count - 1; i >= 0; i--)
			{
			    workerSocket = (Socket)m_workerSocketList[i];
			    bool disconnect = false;
			    if (disconnect_client != null) disconnect = disconnect_client.Contains(i);
			    if (!disconnect)
			    {
					if(workerSocket!= null)
					{
						if(workerSocket.Connected)
						{
						    // if not already sending data to this client
						    if (!sending_data.Contains(i))
						    {
						        sending_data.Add(i);
						        
							    try
							    {
								    workerSocket.Send(byData);
								}
								catch(SocketException se)
								{
					                Console.WriteLine("dpslamServer/Send(" + PortNumber.ToString() + ")/" + se.Message);
								    RemoveClient(i, m_workerSocketList, disconnect_client);
								}
								
								sending_data.Remove(i);
							}
						}
					}
				}
				else
				{
				    RemoveClient(i, m_workerSocketList, disconnect_client);
				}
			}
		}
		
		public void Stop()
		{
			CloseSockets();		
			Running = false;
		}
	
        /// <summary>
        /// returns the local IP address
        /// </summary>
        /// <returns></returns>
		protected string GetIP()
		{	   		
			string strHostName = Dns.GetHostName();
			
			// Find host by name
			IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
		
			// Grab the first IP addresses
			string IPStr = "";
			foreach(IPAddress ipaddress in iphostentry.AddressList)
			{
				IPStr = ipaddress.ToString();
                if (!IPStr.Contains(":"))
                {
                    return IPStr;
                }
			}
			return IPStr;
		}

        /// <summary>
        /// closes all sockets
        /// </summary>
		private void CloseSockets()
		{
		    Console.WriteLine("Closing sockets");
		    
		    DateTime start_time = DateTime.Now;
		    while (receiving_data.Count > 0)
		    {
		        System.Threading.Thread.Sleep(10);
		        TimeSpan diff = DateTime.Now.Subtract(start_time);
		        if (diff.TotalSeconds > 5)
		        {
		            Console.WriteLine("WARNING: timeout waiting for sockets to close in dpslamServer");
		            break;
		        }
		    }
		    
			if (m_mainSocket != null)
			{
                if (m_mainSocket.Connected)
			        m_mainSocket.Shutdown(SocketShutdown.Both);
				m_mainSocket.Close();
			}
			Socket workerSocket = null;
			for(int i = 0; i < m_workerSocketList.Count; i++)
			{
				workerSocket = (Socket)m_workerSocketList[i];
				if(workerSocket != null)
				{
				    workerSocket.Shutdown(SocketShutdown.Both);
					workerSocket.Close();
					workerSocket = null;
				}
			}	
		}

        /// <summary>
        /// returns a list of connected clients 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// sends a message to a connected client
        /// </summary>
        /// <param name="msg">the message to be sent</param>
        /// <param name="clientNumber">index number of the client</param>
        /// <param name="m_workerSocketList"></param>
        /// <param name="usage">usage model</param>
		protected static void SendToClient(
		    string msg, int clientNumber,
		    ArrayList m_workerSocketList)
		{
			// Convert the reply to byte array
			byte[] byData = System.Text.Encoding.ASCII.GetBytes(msg);

			Socket workerSocket = (Socket)m_workerSocketList[clientNumber - 1];
			if (workerSocket != null) workerSocket.Send(byData);
        }

        #endregion

        #region "processing a received status or update request"

        /// <summary>
        /// parses a received xml string
        /// </summary>
        /// <param name="xml_str">string containing xml</param>
        /// <param name="clientnumber">number of the client from which the message was received</param>
        /// <param name="data_recently_received"></param>
        /// <param name="data_recently_received_client_number"></param>
        /// <param name="m_workerSocketList"></param>
        /// <param name="Devices"></param>
        /// <param name="kill"></param>
        /// <param name="disconnect_client"></param>
        /// <param name="disconnect_now"></param>
        /// <param name="usage">usage model</param>
        protected static void ReceiveXmlMessageFromClient(
            string xml_str, 
            int clientnumber,
            ref List<string> data_recently_received, 
            ref List<int> data_recently_received_client_number,
            ref ArrayList m_workerSocketList,
            ref bool kill,
            ref List<int> disconnect_client,
            ref List<int> disconnect_now)
        {
            //Console.WriteLine("client + " + clientnumber.ToString() + " data1 = " + xml_str);
        
            if (xml_str != "")
            {
                //Console.WriteLine(xml_str);                
                
                xml_str = xml_str.Trim();

                // load the file into an XmlDocuent
                XmlDocument xd = new XmlDocument();
                bool valid_xml = false;
                try
                {
                    xd.LoadXml(xml_str);
                    valid_xml = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid XML received:");
                    Console.WriteLine("***");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(xml_str);
                    Console.WriteLine("***");
                    
                    // store the receipt as invalid
                    UpdateDataRecentlyReceived(
                        clientnumber, "Invalid",
                        ref data_recently_received, 
                        ref data_recently_received_client_number);
                }

                if (valid_xml)
                {
                    //Console.WriteLine("client + " + clientnumber.ToString() + " data2 = " + xml_str);
                
                    // store the receipt
                    UpdateDataRecentlyReceived(
                        clientnumber, xml_str,
                        ref data_recently_received, 
                        ref data_recently_received_client_number);
                
                    // get the document root node
                    XmlNode xnodDE = xd.DocumentElement;

                    // recursively walk the node tree
                    string receivedXmlType = "";
                    string currentDeviceId = "";
                    ArrayList devs = new ArrayList();
                    List<string> props = null;

                    // parse the xml and extract information
                    ReceiveXmlFromClient(
                        xnodDE, 0, 
                        ref receivedXmlType, 
                        ref currentDeviceId, 
                        devs, 
                        ref props, 
                        clientnumber,
                        ref kill,
                        disconnect_client,
                        disconnect_now,
                        m_workerSocketList);

                    // if this is a status request send a reply back to the client
                    if (devs.Count > 0)
                    {
                        if (receivedXmlType == STATUS_REQUEST)
                        {
                            if (currentDeviceId.ToLower() == "all")
                            {
                                // status of all devices
                                //SendToClient(getDeviceStatusAll(Devices, usage), clientnumber, m_workerSocketList, usage);
                            }
                            else
                            {
                                // return the status of the requested device properties
                                XmlDocument reply = GetDeviceStatus(devs);
                                SendToClient(reply.InnerXml, clientnumber, m_workerSocketList);
                            }
                        }
                    }

                    if (receivedXmlType == STATUS_UPDATE)
                    {
                        // return the status of the requested device properties
                        XmlDocument update_reply = GetDeviceUpdateReply();
                        SendToClient(update_reply.InnerXml, clientnumber, m_workerSocketList);
                    }
                }

            }
        }

        /// <summary>
        /// read an xml file
        /// </summary>
        /// <param name="xnod"></param>
        /// <param name="level"></param>
        private static void ReceiveXmlFromClient(
            XmlNode xnod, 
            int level, 
            ref string receivedXmlType,
            ref string currentDeviceId,
            ArrayList devs,
            ref List<string> props,
            int clientnumber,
            ref bool kill,
            List<int> disconnect_client,
            List<int> disconnect_now,
            ArrayList m_workerSocketList)
        {
            XmlNode xnodWorking;

            if ((receivedXmlType == "") &&
                ((xnod.Name == STATUS_REQUEST) ||
                 (xnod.Name == STATUS_UPDATE) ||
                 (xnod.Name == STATUS_DISCONNECT)))
            {
                receivedXmlType = xnod.Name;
            }

            if (xnod.Name == "KillServer")
            {
                kill = true;
            }

            if (receivedXmlType == STATUS_DISCONNECT)
            {
                if (disconnect_client == null)
                    disconnect_client = new List<int>();
                    
                if (!disconnect_client.Contains(clientnumber))
                {
                    disconnect_client.Add(clientnumber);
                    RemoveClient(clientnumber, m_workerSocketList, disconnect_client);
                    disconnect_now.Add(clientnumber);
                }
            }

            if (receivedXmlType == STATUS_REQUEST)
            {
                if (xnod.Name == "Property")
                {
                    if ((props != null) && (currentDeviceId != ""))
                        props.Add(xnod.InnerText);
                }
            }

            if (receivedXmlType == STATUS_UPDATE)
            {
                if (currentDeviceId != "")
                {
                }
            }

            // call recursively on all children of the current node
            if (xnod.HasChildNodes)
            {
                xnodWorking = xnod.FirstChild;
                DateTime start_time = DateTime.Now;
                const int timeout_sec = 2;
                int time_elapsed_sec = 0;
                while ((xnodWorking != null) &&
                       (time_elapsed_sec < timeout_sec))
                {
                    ReceiveXmlFromClient(
                        xnodWorking, level + 1, 
                        ref receivedXmlType, 
                        ref currentDeviceId, 
                        devs, 
                        ref props, 
                        clientnumber, 
                        ref kill,
                        disconnect_client,
                        disconnect_now,
                        m_workerSocketList);
                    xnodWorking = xnodWorking.NextSibling;
                    TimeSpan diff = DateTime.Now.Subtract(start_time);
                    time_elapsed_sec = (int)diff.TotalSeconds;
                }
                if (time_elapsed_sec >= timeout_sec)
                    Console.WriteLine("WARNING: timed out within dpslamServer/ReceiveXmlFromClient");
            }
            
        }


        #endregion

        #region "getting the status of a hardware device"

        /// <summary>
        /// returns the status of the given device as xml
        /// </summary>
        /// <param name="Id">unique reference for the device</param>
        /// <param name="doc">xml document</param>
        /// <param name="parent">parent element within the xml document</param>
        /// <param name="required_properties">a list of the device properties which we wish to know the status of</param>
        /// <param name="Devices">list of devices</param>
        /// <returns>xml element</returns>
        protected static XmlElement GetDeviceStatus(
            string Id, 
            XmlDocument doc, 
            XmlElement parent, 
            List<string> required_properties)
        {
            XmlElement status = null;

            return (status);
        }

        /// <summary>
        /// returns the status of all devices
        /// </summary>
        /// <param name="doc">xml document</param>
        /// <param name="parent">parent element within the xml document</param>
        /// <returns>xml element containing the status of all devices</returns>
        protected static XmlElement GetDeviceStatusAll(
            XmlDocument doc,
            XmlElement parent)
        {
            XmlElement devs = doc.CreateElement("Devices");
            return (devs);
        }

        /// <summary>
        /// returns the status of all devices as a string
        /// </summary>
        /// <param name="Devices"></param>
        /// <param name="usage"></param>
        /// <returns></returns>
        protected static string getDeviceStatusAll()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", XML_ENCODING, null);
            doc.PrependChild(dec);

            XmlElement bridgeware = doc.CreateElement(STATUS_REPLY);
            doc.AppendChild(bridgeware);

            XmlElement status = GetDeviceStatusAll(doc, bridgeware);
            bridgeware.AppendChild(status);

            string statusStr = doc.InnerXml;

            //doc.Save("StatusAll.xml");

            return (statusStr);
        }

        /// <summary>
        /// return an Xml document containing the status of the given list of devices
        /// </summary>
        /// <returns></returns>
        protected static XmlDocument GetDeviceStatus(
            ArrayList devs)
        {
            return (GetDeviceStatus(devs, STATUS_REPLY));
        }

        /// <summary>
        /// return an Xml document containing the status of the given list of devices
        /// </summary>
        /// <returns></returns>
        protected static XmlDocument GetDeviceStatus(
            ArrayList devs, 
            string status_type)
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", XML_ENCODING, null);
            doc.PrependChild(dec);

            XmlElement statusreply = doc.CreateElement(status_type);
            doc.AppendChild(statusreply);

            for (int i = 0; i < devs.Count; i += 2)
            {
                string Id = (string)devs[i];
                List<string> required_properties = (List<string>)devs[i + 1];
                XmlElement status = GetDeviceStatus(Id, doc, statusreply, required_properties);
                if (status != null) statusreply.AppendChild(status);
            }

            //doc.Save("DeviceStatus.xml");

            return (doc);
        }

        /// <summary>
        /// return an Xml document used as a reply to the client after an update of parameters has been received and carried out
        /// </summary>
        /// <returns>reply xml document</returns>
        protected static XmlDocument GetDeviceUpdateReply()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", XML_ENCODING, null);
            doc.PrependChild(dec);

            XmlElement statusreply = doc.CreateElement(STATUS_REPLY);
            doc.AppendChild(statusreply);
            
            //doc.Save("DeviceUpdateReply.xml");

            return (doc);
        }

        #endregion

    }
}
