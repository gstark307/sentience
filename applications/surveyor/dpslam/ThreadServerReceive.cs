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
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace dpslam.core
{
    public class ThreadServerReceive
    {
        private WaitCallback _callback;
        private dpslamServer _server;
        private ArrayList _buffer;

        /// <summary>
        /// constructor
        /// </summary>
        public ThreadServerReceive(WaitCallback callback, dpslamServer server, ArrayList receive_buffer)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _callback = callback;
            _server = server;
            _buffer = receive_buffer;
        }

        /// <summary>
        /// ThreadStart delegate
        /// </summary>
        public void Execute()
        {
            Update(_server, _buffer);
            _callback(_server);
        }

        /// <summary>
        /// consolidates the data received so far from a particular client
        /// </summary>
        /// <param name="client_number"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string ConsolidateBuffer(int client_number, ArrayList buffer)
        {
            string buffer_str = "";

            for (int i = 0; i < buffer.Count; i += 2)
            {
                int client_no = (int)buffer[i + 1];
                if (client_no == client_number)
                {
                    buffer_str += (string)buffer[i];
                }
            }

            return (buffer_str);
        }
        
        /// <summary>
        /// server receives data
        /// </summary>
        /// <param name="server">server object</param>
        private static void Update(dpslamServer server, ArrayList buffer)
        {
            while (server.GetNoOfConnectedClients() > 0)
            {            
	            if (buffer.Count > 1)
	            {
	                int client_number = (int)buffer[1];
	                
	                // process the receive buffer	                    
                    if (dpslamServer.EndOfReceive((string)buffer[0]))
                    {
                        server.ProcessReceiveBuffer(client_number, buffer);
                    }
                    else
                    {
                        string buffer_str = ConsolidateBuffer(client_number, buffer);
                        if (dpslamServer.EndOfReceive(buffer_str))
                        {
                            server.ProcessReceiveBuffer(client_number, buffer);
                        }
                    }
	                
	                //Console.WriteLine("buffer size = " + buffer.Count.ToString());
	            }
	            System.Threading.Thread.Sleep(5);
            }
        }
    }
}
