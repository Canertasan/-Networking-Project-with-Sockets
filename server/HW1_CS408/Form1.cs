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
        static bool finished = false; // ?
        static bool accept = true; // ?
        static double totalTurn = 0;
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
        { // this is just for the lobby part. When the game start it will suspend
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
                    { // exit when the client socket is not connected!
                        SetRichText(clientList[i].name + " exit the game" + Environment.NewLine);
                        clientList.Remove(clientList[i]);
                        SetRichText(clientList.Count() + " player left" + Environment.NewLine);
                    }
                }
            }
        }

        void Accept() // this is thread when the listen button clicked! This is basically accept ! When the start button is clicked then
        { // accept button still going but we cannot take any client after that! We just show error to client!
            while (accept)
            {
                try
                {
                    Socket newClient = serverSocket.Accept(); // new client socket is created.
                    checkLobby.Suspend(); // when the new client is on the way checkLobby thread is suspended! We dont wanna mess up the availablity in there
                    clientInfo.name = "";
                    clientInfo.scores = 0;
                    clientInfo.clientSockets = newClient; // our new client
                    clientList.Add(clientInfo); // add list because we use Receive name function as added way. We dont add the name of it yet!
                    String name = ReceiveName(); // client name.
                    if (clientList.Count() != 0 && (clientList.Any(client => client.name == name))) // if there is same in the list
                    {
                        String reject = "reject"; // reject message to client...
                        byte[] msg = Encoding.ASCII.GetBytes(reject);
                        newClient.Send(Encoding.ASCII.GetBytes(reject.Length.ToString()));
                        Thread.Sleep(1000);
                        newClient.Send(msg);
                        SetRichText("Client attempt to connect server with another currently logged in username." + Environment.NewLine + name + " is already taken!"+ Environment.NewLine);
                        clientList.Remove(clientInfo); // We remove because we dont accept it 
                        newClient.Close(); // close newclient.
                    }
                    else if (started) // If game is started then client shouldnt be entered. We just simply warn the client!
                    {
                        SetRichText(name + " try to connect after the game started!" + Environment.NewLine); //
                        String rejectExit = "game is already started"; // reject message to client...
                        byte[] msg = Encoding.ASCII.GetBytes(rejectExit);
                        newClient.Send(Encoding.ASCII.GetBytes(rejectExit.Length.ToString()));
                        Thread.Sleep(1000);
                        newClient.Send(msg);
                        clientList.Remove(clientInfo); // remove that client
                        newClient.Close(); // close the client
                        continue;
                    }
                    else
                    {
                        clientList.Remove(clientInfo); // just remove because we wanna add name to client
                        clientInfo.name = name;
                        clientList.Add(clientInfo); // then added again
                        String ok = "ok";
                        byte[] msg = Encoding.ASCII.GetBytes(ok);
                        newClient.Send(Encoding.ASCII.GetBytes(ok.Length.ToString())); // send is it ok. You accept mean
                        Thread.Sleep(1000);
                        newClient.Send(msg);
                        String added = name + " is connected." + " Total: " + (clientList.Count()).ToString() + " clients are connected. " + Environment.NewLine;
                        SetRichText(added);
                    }
                    checkLobby.Resume(); // then the thread working on! 
                }
                catch
                {
                    if (terminating) // If there is a problem server then terminated!
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

        void GameStart() // When game start this thread will execute
        {
            Thread checkClientThread = new Thread(checkClientThreadCurr); // will check all clients connection all the time.
            checkClientThread.Start(); // thread start...
            for (int i = 0; i < clientList.Count(); i++) // send all client game is started. 
            {
                clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes("5")); // we send lenght 
                Thread.Sleep(1000);
                clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes("start")); // then we send actual string!
            }
            clientList = clientList.OrderBy(client => client.name).ToList();  // this sort the list in terms of name
            exit = false;
            index = -1; // then the first starter start at 0 index
            while (!exit && index != totalTurn-1) // index start in 0 then total turn -1
            {
                try
                {
                    Thread.Sleep(5000); // this isfor waiting other client! It prevent mixed !
                    index++;// changing the turn
                    SetRichText(clientList[index%clientList.Count()].name + "'s turn" + Environment.NewLine);
                    clientList[index % clientList.Count()].clientSockets.Send(Encoding.ASCII.GetBytes("27"));// send client who the turn has.
                    Thread.Sleep(1000);
                    clientList[index % clientList.Count()].clientSockets.Send(Encoding.ASCII.GetBytes("Your Turn create a question"));
                    checkClientThread.Suspend(); // suspend that thread because we dont wanna update the current client soc count! It will prevent that!
                    ques = ReceiveByte(index % clientList.Count()); // take question! 
                    if (ques == "Exited") // when client exited then this ques return!
                        continue;
                    SetRichText(clientList[index % clientList.Count()].name + "'s question: " + ques + Environment.NewLine);
                    answer = ReceiveByte(index % clientList.Count()); // take answer
                    SetRichText(clientList[index % clientList.Count()].name + "'s answer: " + answer + Environment.NewLine);
                    List<String> answerList = new List<String>();
                    //mut.WaitOne();
                    for (int i = 1; i < clientList.Count(); i++) // send question all client.
                    {
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes("17"));
                        Thread.Sleep(1000);
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes("Wait for question")); // send waiting that question
                        SetRichText(clientList[((index + i) % clientList.Count())].name + " can answer the question. " + Environment.NewLine);
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes(ques.Length.ToString()));//send question to others
                        Thread.Sleep(1000);
                        clientList[((index + i) % clientList.Count())].clientSockets.Send(Encoding.ASCII.GetBytes(ques));//send question to others
                    }            
                    int counter = 0; // counter for the while!
                    while (counter != clientList.Count() - 1) // This is just simply busy waiting! 
                    {
                        for (int i = 1; i < clientList.Count(); i++) // looking for all client except the asker!
                        {
                            if (clientList[((index + i) % clientList.Count())].clientSockets.Available != 0)// take the first sender ! 
                            {
                                clientAnswer = ReceiveByte(((index + i) % clientList.Count()));
                                if (clientAnswer != "Exited") // if exited then no update on rich text box!
                                    SetRichText(clientList[((index + i) % clientList.Count())].name + " answered the question." + Environment.NewLine);
                            }
                            else
                                continue;
                            if (clientAnswer == "Exited") // client send exited then It will exit but end of the turn
                            {
                                counter++;
                                continue;
                            }
                            if (clientAnswer == answer) // answer check 
                            {
                                SetRichText(clientList[((index + i) % clientList.Count())].name + " answered correctly.So s/he get one point." + Environment.NewLine);
                                client c = clientList[((index + i) % clientList.Count())];
                                clientList.Remove(c);
                                c.scores++; // When we update the client we remove and add simultionusly
                                clientList.Add(c);
                                clientList = clientList.OrderBy(client => client.name).ToList();  // this sort the list in terms of name
                                SetRichText( "Current score is: " + clientList[((index + i) % clientList.Count())].scores+ Environment.NewLine);
                                counter++; // Then update counter for the while loop
                            }
                            else
                            {
                                if (clientAnswer == "Exited") // When the player exited then no give cout the about couldnt answer correctly!
                                {
                                    counter++;
                                    continue;
                                }
                                else
                                {
                                    SetRichText(clientList[((index + i) % clientList.Count())].name + " couldn't answered correctly. " + Environment.NewLine);
                                    counter++;
                                }
                            }
                        }
                    }
                    checkClientThread.Resume(); // then we updated exited client and update the list
                }
                catch (SocketException e)
                {

                }
                
            }
            checkClientThread.Suspend(); // we suspend it bcs we will exit all client we dont wanna mess up here!
            if (index == totalTurn-1 )//sending results.
            {
                clientList = clientList.OrderByDescending(client => client.scores).ThenBy(client => client.scores).ToList(); // this sort the list in terms of name
                for (int i = 0; i < clientList.Count(); i++) // send question all client.
                {
                    String result = (i+1) + ". place => Your Total Score is " + clientList[i].scores.ToString(); //result!
                    clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes(result.Length.ToString()));
                    Thread.Sleep(1000);
                    clientList[i].clientSockets.Send(Encoding.ASCII.GetBytes(result));
                    SetRichText(clientList[i].name + " " + (i+1) + ". place => Total Score is " + clientList[i].scores + Environment.NewLine);
                }
                finished = true;
                SetRichText("GAME END!" + Environment.NewLine);
                System.Windows.Forms.Application.Exit();
            }
        }

        void checkClientThreadCurr() // the checker thread for number of clients if there is one then game is over.
        {
            while (true)
            {
                int clientSize = 0;
                for (int i = 0; i < clientList.Count(); i++) // ıf there is exited client then update it!
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
                if (clientList.Count() < 2) // when client count is smaller then 2 then there is a winner!
                {
                    String result = "You are the winner and game is finished";
                    exit = true; // to finish the loop
                    clientList[0].clientSockets.Send(Encoding.ASCII.GetBytes(result.Length.ToString())); // sendid you are a winner
                    Thread.Sleep(1000);
                    clientList[0].clientSockets.Send(Encoding.ASCII.GetBytes(result));
                    SetRichText(clientList[0].name + " is the winner" + Environment.NewLine);
                    Thread.Sleep(2000);
                    CloseAllClients();

                }
            }
        }

        void CloseAllClients()
        {
            for (int i = 0; i < clientList.Count(); i++)
            {
                Socket thisClient = clientList[i].clientSockets;
                if(!finished)
                    thisClient.Close();
            }
            serverSocket.Close();
            Thread.Sleep(2000);
            Environment.Exit(Environment.ExitCode); // kill all threads.
            System.Windows.Forms.Application.Exit(); // exit the GUI
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
                Socket thisClient = clientList[i].clientSockets; // receive byte
                Byte[] buffer = new Byte[64];
                thisClient.Receive(buffer);
                int len = int.Parse(Encoding.ASCII.GetString(buffer)); // take lenght
                Array.Clear(buffer, 0, buffer.Length);
                Byte[] bufferLen = new Byte[len]; // create a new buffer in that lenght 
                thisClient.Receive(bufferLen);
                String name = Encoding.ASCII.GetString(bufferLen); //take the question or name 
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

        private void SetRichText(string text) // this is delegate rich text box! 
        {   //We will use it when the access in threads!
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

        private void button2_Click(object sender, EventArgs e) // when game is started button actived 
        {
            if (clientList.Count() >= 2) // if client number is 2 or more
            {
                this.label3.Visible = false;
                bool IsTextInt = Regex.IsMatch(textBox2.Text, @"^\d+$"); // if it is integer then turn true;
                if (IsTextInt)
                {
                    started = true; // thşs prevent after the game start accept thread is finish.
                    checkLobby.Suspend();
                    this.label4.Visible = false;
                    this.label1.Visible = false;
                    this.button2.Visible = false;
                    this.textBox2.Visible = false;
                    this.richTextBox1.Left -= 100;
                    totalTurn = double.Parse(textBox2.Text);
                    receiveThread = new Thread(GameStart); // thread for new client
                    
                    receiveThread.Start();
                    
                }
                else // then give an error it!
                {
                    this.label4.Visible = true;
                }

            }
            else
                this.label3.Visible = true;

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            accept = false;
            CloseAllClients();
        }

    }


}
