using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using System.Net;
using System.Net.Sockets;



namespace SocketTestClient
{
    class MutualCommunication
    {
        public StateObject serverStateObject;
        public StateObject clientStateObject;
        IPAddress serverIP;
        int serverPort = 12347;
        //string currentMessage = "";
        public delegate void MessageReceivedEventHandler(object sender, MessageReceivedArgs msgRcvArgs);
        public event MessageReceivedEventHandler RaiseMsgRcvEvent;

        public MutualCommunication(string ipAddrStr, int port)
        {
            serverIP = IPAddress.Parse(ipAddrStr);
            serverPort = port;
        }

        public void ServerListen()
        {
            IPEndPoint localEndPoint = new IPEndPoint(serverIP, serverPort);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            listener.BeginAccept(
                   new AsyncCallback(AcceptCallback),
                   listener);
        }

        private void AcceptCallback(IAsyncResult ar)
        {

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            serverStateObject = new StateObject();
            serverStateObject.workSocket = handler;
            handler.BeginReceive(serverStateObject.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), serverStateObject);
            Console.WriteLine("mutual channel established!");
        }


        public void ClientConnect()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(serverIP, serverPort);
            Socket client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.
            client.BeginConnect(serverEndPoint,
                new AsyncCallback(ConnectCallback), client);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                clientStateObject = new StateObject();
                clientStateObject.workSocket = client;

                client.NoDelay = true;

                client.BeginReceive(clientStateObject.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), clientStateObject);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                //Console.WriteLine("read {0} bytes", bytesRead);
                string dataStr = Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead);
                MessageReceivedArgs args = new MessageReceivedArgs(dataStr);
                MessageReceivedEventHandler raiseEvent = RaiseMsgRcvEvent;
                if (raiseEvent != null) raiseEvent(this, args);

                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }

        public void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            if (handler != null)
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void shutDown()
        {

        }
    }

    #region message received event argument class
    public class MessageReceivedArgs : EventArgs
    {
        public MessageReceivedArgs(string s)
        {
            msg = s;
        }
        private string msg;
        public string Message
        {
            get { return msg; }
        }
    }
    #endregion

}
