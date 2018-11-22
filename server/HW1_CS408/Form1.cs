using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HW1_CS408
{
    public partial class Form1 : Form
    {
        delegate void StringArgReturningVoidDelegate(string text);
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // our server soc.
        static List<Socket> clientSockets = new List<Socket>(); // these are client sockets list.
        static List<String> nameList = new List<String>(); // these are names which we will check for uniquenesss
        static bool terminating = false; // ?
        static bool accept = true; // ?
        int portNum = 0; // this is port num for connection.
        String ques = "";
        String answer = "";
        String clientAnswer = "";
        Byte[] buffer = new Byte[64];
        private const int SOCKET_COUNT_MAX = 2;
        static bool connection = true;

        //public static bool isConnected() // if some player is gone then remove client... Before Game
        //{
        //    try
        //    {
        //        foreach (Socket socket in clientSockets)
        //        {
        //            if (socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0)
        //            {
        //                int index = clientSockets.IndexOf(socket); // removing the lost connection...
        //                clientSockets.Remove(socket);
        //                nameList.Remove(nameList[index]);
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    catch (SocketException)
        //    {
        //        return false;
        //    }

        //}

        public Form1()
        {
            // form initializer
            InitializeComponent();
            this.richTextBox1.Visible = false;
            this.button2.Visible = false;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Visible = false; // after starting listening hide all first 3 button,label,textbox.
            this.label2.Visible = false;
            this.textBox1.Visible = false;
            this.richTextBox1.Visible = true; // show richtextBox to analyze the results.
            // server decide port number.
            portNum = int.Parse(textBox1.Text); // taking port num as int
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, portNum); // creating end point.
            serverSocket.Bind(endPoint); // then bind the endpoint with server socket
            serverSocket.Listen(SOCKET_COUNT_MAX); // then listen for 2 client
            this.richTextBox1.AppendText("Listening..." + Environment.NewLine + "Waiting for 2 Players." + Environment.NewLine); // server listening
            this.Text = "SERVER";
            Thread acceptThread = new Thread(Accept); // will accept or not in clients
            acceptThread.Start(); // thread start...

        }

        public bool StillConnect()
        {

            return false;
        }

        void Accept()
        {
            while (accept)
            {
                try
                {
                    Socket newClient = serverSocket.Accept(); // new client socket is created.
                    clientSockets.Add(newClient); // add the client list   
                    String name = ReceiveName(); // client name.

                    if (nameList.Count() != 0 && nameList.Contains(name)) // if there is same in the list
                    {
                        clientSockets.Remove(newClient); // remove the list of client socket.
                        String reject = "reject"; // reject message to client...
                        byte[] msg = Encoding.ASCII.GetBytes(reject);
                        newClient.Send(msg);
                        SetRichText("Client attempt to connect server with another currently logged in username." + Environment.NewLine + name + " is already taken!");
                        newClient.Close(); // close newclient.
                    }
                    else
                    {
                        String ok = "ok"; // reject message to client...
                        byte[] msg = Encoding.ASCII.GetBytes(ok);
                        newClient.Send(msg);
                        nameList.Add(name); // first add name in the list

                        String added = name + " is connected." + " Total: " + (clientSockets.Count()).ToString() + " clients are connected. " + Environment.NewLine;
                        SetRichText(added);
                        if (clientSockets.Count() == 2) // when two player is active. Dont receive now.
                        {
                            SetRichText("Game started!" + Environment.NewLine);
                            SetVisibility(button2, true);
                            Thread receiveThread = new Thread(GameStart); // thread for new client
                            receiveThread.Start();
                        }
                    }
                }
                catch
                {
                    if (terminating)
                    {// if server is terminate
                        SetRichText("Server stopped working, all connected clients will be terminated.");
                        accept = false;
                        CloseAllClients();
                    }
                    else
                    { // if just connection lost.
                        foreach (Socket socket in clientSockets)
                        {
                            if (socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0)
                            {
                                int index = clientSockets.IndexOf(socket); // removing the lost connection...
                                clientSockets.Remove(socket);
                                nameList.Remove(nameList[index]);
                            }
                        }
                    }
                }
            }
        }

        void GameStart()
        {
            for (int i = 0; i < clientSockets.Count(); i++)
            {
                clientSockets[i].Send(Encoding.ASCII.GetBytes("start"));
            }
            String minElement = nameList.Min();  // minNumber: 2
            int index = nameList.IndexOf(minElement);
            bool exit = false;
            while (clientSockets[0].Connected && clientSockets[1].Connected && !exit)
            {
                try
                {
                    int turnSocketIndex = index % clientSockets.Count();

                    SetRichText(nameList[turnSocketIndex] + "'s turn" + Environment.NewLine);
                    clientSockets[turnSocketIndex].Send(Encoding.ASCII.GetBytes("Your Turn create a question"));
                    clientSockets[turnSocketIndex].Receive(buffer);
                    ques = Encoding.ASCII.GetString(buffer);
                    Array.Clear(buffer, 0, buffer.Length);
                    int indexx = ques.IndexOf("\0");
                    ques = ques.Substring(0, indexx);
                    SetRichText(nameList[turnSocketIndex] + "'s question: " + ques + Environment.NewLine);
                    clientSockets[turnSocketIndex].Receive(buffer);
                    answer = Encoding.ASCII.GetString(buffer);
                    Array.Clear(buffer, 0, buffer.Length);
                    indexx = answer.IndexOf("\0");
                    answer = answer.Substring(0, indexx);
                    SetRichText(nameList[turnSocketIndex] + "'s answer: " + answer + Environment.NewLine);
                    /* STEP 2 DE KULLAN
                    for (int i = 1; i < clientSockets.Count() ; i++) // send question all client.
                    {
                        clientSockets[index+i % clientSockets.Count()].Send(Encoding.ASCII.GetBytes(ques));
                        // BURAYA GELDİGİ GİBİ DİNLESİN CEVABI
                    }
                     */
                    int otherSocket = (index + 1) % clientSockets.Count();
                    SetRichText(nameList[otherSocket] + "'s turn to answer. " + Environment.NewLine);
                    clientSockets[otherSocket].Send(Encoding.ASCII.GetBytes("Wait for question"));
                    clientSockets[otherSocket].Send(Encoding.ASCII.GetBytes(ques));
                    clientSockets[otherSocket].Receive(buffer);
                    clientAnswer = Encoding.ASCII.GetString(buffer);
                    Array.Clear(buffer, 0, buffer.Length);
                    indexx = clientAnswer.IndexOf("\0");
                    clientAnswer = clientAnswer.Substring(0, indexx);
                    if (clientAnswer.Contains(answer)) // answer check
                    {
                        SetRichText(nameList[otherSocket] + " answered correctly. " + Environment.NewLine);
                    }
                    else
                    {
                        SetRichText(nameList[otherSocket] + " couldn't answered correctly. " + Environment.NewLine);
                    }
                }
                catch (SocketException e)
                {
                    if (SocketError.ConnectionReset == e.SocketErrorCode)
                    {
                        SetRichText("One of the client left, so game is over bye..." + Environment.NewLine); // if is not still connected or some problem with connection.
                        CloseAllClients();
                    }  
                    exit = true;
                }
                index++;// changing the turn
            }
        }
        void CloseAllClients()
        {
            for (int i = 0; i < clientSockets.Count(); i++)
            {
                Socket thisClient = clientSockets[i];
                thisClient.Close();
            }
            serverSocket.Close();
            Thread.Sleep(5000);
            System.Windows.Forms.Application.Exit();
        }

        void Receive()
        {
            bool connected = true;
            int lenClientSoc = clientSockets.Count();
            Socket thisClient = clientSockets[lenClientSoc - 1]; // create thisClient which is connected last one
            String clientName = nameList[lenClientSoc - 1]; // client name also.

            while (connected && !terminating)
            {
                try
                { // taking client strings .
                    Byte[] buffer = new Byte[64];
                    thisClient.Receive(buffer);
                    SetRichText("Client: " + Encoding.Default.GetString(buffer));

                }
                catch
                {
                    connected = false; // clients down.
                    if (!terminating)
                    {
                        SetRichText("Client has disconnected...");
                    }
                    nameList.Remove(clientName);
                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                }
            }


        }
        static String ReceiveName() // return name.
        {
            int lenClientSoc = clientSockets.Count();
            Socket thisClient = clientSockets[lenClientSoc - 1];
            Byte[] buffer = new Byte[64];
            thisClient.Receive(buffer);
            string name = Encoding.ASCII.GetString(buffer);
            Array.Clear(buffer, 0, buffer.Length);
            int index = name.IndexOf("\0");
            name = name.Substring(0, index);
            return name;
        }

        public static void SetVisibility(Button t, bool v)// changes the visibility of the given 
        {
            t.Invoke((MethodInvoker)delegate
            {
                // Running on the UI thread
                t.Visible = v;
            });

        }

        private void SetRichText(string text)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetRichText);
                this.richTextBox1.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox1.AppendText(text);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CloseAllClients();
        }


    }
}
