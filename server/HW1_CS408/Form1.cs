using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HW1_CS408
{
    public partial class Form1 : Form
    {
        public struct client
        {
            public Socket clientSockets;
            public string name;
            public int scores;
            public client(string nameI, int scoresI, Socket clientSocketsI)
            {
                name = nameI;
                scores = scoresI;
                clientSockets = clientSocketsI;
            }

        };
        delegate void StringArgReturningVoidDelegate(string text);
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // our server soc.
        static List<client> clientList = new List<client>();
        public static client clientInfo;
        private static Mutex mut = new Mutex();
        static bool terminating = false; // ?
        static bool accept = true; // ?
        static int totalTurn = 0;
        static public bool exit;
        static public int index;
        Thread acceptThread;
        Thread checkLobby;
        Thread receiveThread;
        int portNum = 0; // this is port num for connection.
        bool started = false;
        String ques = "";
        String answer = "";
        String clientAnswer = "";
        static Byte[] buffer = new Byte[64];
        private const int SOCKET_COUNT_MAX = 10000;
        static bool connection = true;

        public Form1()
        {
            InitializeComponent();
            this.richTextBox1.Visible = false;
            this.label3.Visible = false;
            this.label4.Visible = false;
            this.label1.Visible = false;
            this.button2.Visible = false;
            this.textBox2.Visible = false;
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
            this.label1.Visible = true;
            this.button2.Visible = true;
            this.textBox2.Visible = true;
            portNum = int.Parse(textBox1.Text); // taking port num as int
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, portNum); // creating end point.
            serverSocket.Bind(endPoint); // then bind the endpoint with server socket
            serverSocket.Listen(SOCKET_COUNT_MAX); // then listen for 2 client
            this.richTextBox1.AppendText("Listening..." + Environment.NewLine + "Waiting for Players." + Environment.NewLine); // server listening
            this.Text = "SERVER";
            acceptThread = new Thread(Accept); // will accept or not in clients
            acceptThread.Start(); // thread start...
            checkLobby = new Thread(CheckLobbyThread);
            checkLobby.Start();
        }
        void CheckLobbyThread() // the checker thread for number of clients if there is one then game is over.
        {
            while (true)
            {
                int clientSize = 0;
                for (int i = 0; i < clientList.Count(); i++)
                {
                    if (clientList[i].clientSockets.Available == 0)
                    {
                        clientSize++;
                    }
                    else
                    {
                        SetRichText(clientList[i].name + " exit the game" + Environment.NewLine);
                        clientList.Remove(clientList[i]);
                        SetRichText(clientList.Count() + " player left" + Environment.NewLine);
                    }
                }
            }
        }

        void Accept()
        {
            while (accept && !started)
            {
                try
                {
                    Socket newClient = serverSocket.Accept(); // new client socket is created.
                    checkLobby.Suspend();
                    clientInfo.name = "";
                    clientInfo.scores = 0;
                    clientInfo.clientSockets = newClient;
                    clientList.Add(clientInfo);
                    String name = ReceiveName(); // client name.

                    if (clientList.Count() != 0 && (clientList.Any(client => client.name == name))) // if there is same in the list
                    {
                        String reject = "reject"; // reject message to client...
                        byte[] msg = Encoding.ASCII.GetBytes(reject);
                        newClient.Send(Encoding.ASCII.GetBytes(reject.Length.ToString()));
                        Thread.Sleep(1000);
                        newClient.Send(msg);
                        SetRichText("Client attempt to connect server with another currently logged in username." + Environment.NewLine + name + " is already taken!"+ Environment.NewLine);
                        clientList.Remove(clientInfo);
                        newClient.Close(); // close newclient.
                    }
                    else
                    {
                        clientList.Remove(clientInfo);
                        clientInfo.name = name;
                        clientList.Add(clientInfo);
                        String ok = "ok";
                        byte[] msg = Encoding.ASCII.GetBytes(ok);
                        newClient.Send(Encoding.ASCII.GetBytes(ok.Length.ToString()));
                        Thread.Sleep(1000);
                        newClient.Send(msg);
                        String added = name + " is connected." + " Total: " + (clientList.Count()).ToString() + " clients are connected. " + Environment.NewLine;
                        SetRichText(added);
                    }
                    checkLobby.Resume();
                }
                catch
                {
                    if (terminating)
                    {// if server is terminate
                        SetRichText("Server stopped working, all connected clients will be terminated." + Environment.NewLine);
                        accept = false;
                        CloseAllClients();
                    }
                    else
                    { // if just connection lost. // HOW TO SEARCH FOR OTHER CLIENT SOCKET IN CLIENT LIST
                        for (int i = 0; i < clientList.Count(); i++)
                        {
                            if (clientList[i].clientSockets.Poll(1, SelectMode.SelectRead) && clientList[i].clientSockets.Available == 0)
                            {
                                int index = clientList.FindIndex(client => client.clientSockets == clientList[i].clientSockets); // removing the lost connection...
                                clientList.Remove(clientList[index]);
                            }
                        }
                    }
                }
            }
        }

        void GameStart()
        {
            //checkLobby.Abort();
            //acceptThread.Abort();
            Thread checkClientThread = new Thread(checkClientThreadCurr); // will check all clients connection all the time.
            checkClientThread.Start(); // thread start...
            for (int i = 0; i < clientList.Count(); i++)
            {
                clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes("5"));
                Thread.Sleep(1000);
                clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes("start"));
            }
            clientList = clientList.OrderBy(client => client.name).ToList();  // this sort the list in terms of name
            exit = false;
            index = -1; // then the first starter start at 0 index
            while (!exit && index != totalTurn-1) // index start in 0 then total turn -1
            {
                try
                {
                    Thread.Sleep(5000);
                    index++;// changing the turn
                    SetRichText(clientList[index].name + "'s turn" + Environment.NewLine);
                    clientList[index].clientSockets.Send(Encoding.ASCII.GetBytes("27"));
                    Thread.Sleep(1000);
                    clientList[index].clientSockets.Send(Encoding.ASCII.GetBytes("Your Turn create a question"));
                    ques = ReceiveByte(index);
                    if (ques == "Exited")
                        continue;
                    SetRichText(clientList[index].name + "'s question: " + ques + Environment.NewLine);
                    answer = ReceiveByte(index);
                    SetRichText(clientList[index].name + "'s answer: " + answer + Environment.NewLine);
                    List<String> answerList = new List<String>();
                    //mut.WaitOne();
                    checkClientThread.Suspend();
                    for (int i = 1; i < clientList.Count(); i++) // send question all client.
                    {
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes("17"));
                        Thread.Sleep(1000);
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes("Wait for question"));
                        SetRichText(clientList[((index + i) % clientList.Count())].name + " can answer the question. " + Environment.NewLine);
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes(ques.Length.ToString()));//send question to others
                        Thread.Sleep(1000);
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes(ques));//send question to others
                    }            
                    int counter = 0;
                    while (counter != clientList.Count() - 1)
                    {
                        for (int i = 1; i < clientList.Count(); i++)
                        {
                            if (clientList[((index + i) % clientList.Count())].clientSockets.Available != 0)
                            {
                                clientAnswer = ReceiveByte(((index + i) % clientList.Count()));
                                if (clientAnswer != "Exited")
                                    SetRichText(clientList[((index + i) % clientList.Count())].name + " answered the question." + Environment.NewLine);
                            }
                            else
                                continue;
                            if (clientAnswer == "Exited")
                            {
                                counter++;
                                continue;
                            }
                            if (clientAnswer.Contains(answer)) // answer check
                            {
                                SetRichText(clientList[((index + i) % clientList.Count())].name + " answered correctly.So s/he get one point." + Environment.NewLine);
                                client c = clientList[((index + i) % clientList.Count())];
                                clientList.Remove(c);
                                c.scores++;
                                clientList.Add(c);
                                clientList = clientList.OrderBy(client => client.name).ToList();  // this sort the list in terms of name
                                SetRichText( "Current score is: " + clientList[((index + i) % clientList.Count())].scores+ Environment.NewLine);
                                counter++;
                            }
                            else
                            {
                                if (clientAnswer == "Exited")
                                {
                                    counter++;
                                    continue;
                                }
                                SetRichText(clientList[((index + i) % clientList.Count())].name + " couldn't answered correctly. " + Environment.NewLine);
                                counter++;
                            }
                        }
                    }
                    //mut.ReleaseMutex();
                    checkClientThread.Resume();
                }
                catch (SocketException e)
                {

                }
                
            }
            checkClientThread.Suspend();
            if (index == totalTurn-1)//sending results.
            {
                clientList = clientList.OrderByDescending(client => client.scores).ThenBy(client => client.scores).ToList(); // this sort the list in terms of name
                for (int i = 0; i < clientList.Count(); i++) // send question all client.
                {
                    String result = (i+1) + ". place => Your Total Score is " + clientList[i].scores.ToString();
                    clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes(result.Length.ToString()));
                    Thread.Sleep(1000);
                    clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes(result));
                    SetRichText(clientList[i].name + " " + (i+1) + ". place => Total Score is " + clientList[i].scores + Environment.NewLine);
                }
                Thread.Sleep(10000);
                SetRichText("GAME END!" + Environment.NewLine);
                CloseAllClients();
            }
        }

        void checkClientThreadCurr() // the checker thread for number of clients if there is one then game is over.
        {
            while (true)
            {
                int clientSize = 0;
                for (int i = 0; i < clientList.Count(); i++)
                {
                    if (!(clientList[i].clientSockets.Poll(1, SelectMode.SelectRead) && clientList[i].clientSockets.Available == 0))
                    {
                        clientSize++;
                    }
                    else
                    {
                        if (clientList[index].clientSockets == clientList[i].clientSockets)
                            index--;
                        SetRichText(clientList[i].name + " is exit the game" + Environment.NewLine);
                        clientList.Remove(clientList[i]);
                        SetRichText(clientList.Count() + " player is left" + Environment.NewLine);
                    }
                }
                if (clientList.Count() < 2)
                {
                    String result = "You are the winner and game is finished";
                    exit = true; // to finish the loop
                    clientList[0].clientSockets.Send(Encoding.ASCII.GetBytes(result.Length.ToString()));
                    Thread.Sleep(1000);
                    clientList[0].clientSockets.Send(Encoding.ASCII.GetBytes(result));
                    SetRichText(clientList[0].name + " is the winner" + Environment.NewLine);
                    Thread.Sleep(5000);
                    CloseAllClients();
                }
            }
        }

        void CloseAllClients()
        {
            for (int i = 0; i < clientList.Count(); i++)
            {
                Socket thisClient = clientList[i].clientSockets;
                thisClient.Close();
            }
            serverSocket.Close();
            Thread.Sleep(10000);
            System.Windows.Forms.Application.Exit();
        }

        static String ReceiveName() // return name.
        {
            int lenClientSoc = clientList.Count() - 1;
            Socket thisClient = clientList[lenClientSoc].clientSockets;
            Byte[] buffer = new Byte[64];
            thisClient.Receive(buffer);
            int len = int.Parse(Encoding.ASCII.GetString(buffer));
            Byte[] bufferLen = new Byte[len];
            thisClient.Receive(bufferLen);
            String name = Encoding.ASCII.GetString(bufferLen);
            return name;
        }

        static String ReceiveByte(int i) // return name.
        {
            try
            {
                Socket thisClient = clientList[i].clientSockets;
                Byte[] buffer = new Byte[64];
                thisClient.Receive(buffer);
                int len = int.Parse(Encoding.ASCII.GetString(buffer));
                Array.Clear(buffer, 0, buffer.Length);
                Byte[] bufferLen = new Byte[len];
                thisClient.Receive(bufferLen);
                String name = Encoding.ASCII.GetString(bufferLen);
                return name;
            }
            catch
            {
                return "Error";
            }
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
            if (clientList.Count() >= 2)
            {
                this.label3.Visible = false;
                bool IsTextInt = Regex.IsMatch(textBox2.Text, @"^\d+$"); // if it is integer then turn true;
                if (IsTextInt)
                {
                    acceptThread.Suspend();
                    checkLobby.Suspend();
                    this.label4.Visible = false;
                    this.label1.Visible = false;
                    this.button2.Visible = false;
                    this.textBox2.Visible = false;
                    this.richTextBox1.Left -= 100;
                    totalTurn = int.Parse(textBox2.Text);
                    receiveThread = new Thread(GameStart); // thread for new client
                    started = true; // thşs prevent after the game start accept thread is finish.
                    receiveThread.Start();
                }
                else
                {
                    this.label4.Visible = true;
                }

            }
            else
                this.label3.Visible = true;

        }

    }


}
