using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client_CS408
{
    public partial class Form1 : Form
    {
        delegate void StringArgReturningVoidDelegate(string text);
        static String serverIP = "";
        static Socket clientSoc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // out client socket.
        static bool connected = false;
        static String gameCond = "";
        static String turn = "";
        static bool isTurn = false;
        int portNum = 0;
        Byte[] buffer = new Byte[64];

        public Form1()
        {
            InitializeComponent();
            this.textBox3.Visible = false;
            this.textBox4.Visible = false;
            this.label3.Visible = false;
            this.label4.Visible = false;
            this.button2.Visible = false;
            this.richTextBox1.Visible = false;
            this.label6.Visible = false;
            this.label7.Visible = false;
            this.exitButton.Visible = false;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            serverIP = textBox1.Text;
            if (serverIP == "" || textBox2.Text == "" || textBox5.Text == "")
            {
                this.label6.Visible = true; // WARNING FOR CORRECT INPUT.
                this.label6.Text = "You cannot leave it as blank!";
                button1.BackColor = Color.Red;
            }
            else
            {
                portNum = int.Parse(textBox2.Text); // our port num
                try
                {
                    clientSoc.Connect(serverIP, portNum); //  create connection for request to the server.
                    clientSoc.Send(Encoding.Default.GetBytes(textBox5.Text)); // send name to the server for request.
                    if (ReceiveName().Contains("reject")) // reject or not.
                    {
                        this.label6.Visible = true;
                        this.label6.Text = "There is a same name user. Chose different one."; // if is not still connected or some problem with connection.
                        connected = false;
                        clientSoc.Close();
                    }
                    else
                    {
                        connected = true; // if there is no name duplicate then connected. 
                        this.label6.Visible = false; // these visibilities for some cool GUI :D 
                        this.textBox3.Visible = false;
                        this.textBox4.Visible = false;
                        this.label3.Visible = true;
                        this.label4.Visible = true;
                        this.button2.Visible = true;
                        this.exitButton.Visible = true;
                        this.richTextBox1.Visible = true;
                        this.textBox5.Visible = false;
                        this.textBox2.Visible = false;
                        this.textBox1.Visible = false;
                        this.button1.Visible = false;
                        this.label5.Visible = false;
                        this.label2.Visible = false;
                        this.label1.Visible = false;
                        this.richTextBox1.AppendText("Welcome to Our Question - Answer Game" + Environment.NewLine);
                        this.Text = textBox5.Text;

                        Thread receiveThread = new Thread(Receive); // start receive thread for taking inputs from server.
                        receiveThread.Start();
                    }
                }
                catch
                {
                    this.richTextBox1.AppendText("There is a problem! Check the connection...");
                    connected = false;
                    clientSoc.Close();
                }
            }
        }

        void Receive()
        {
            while (connected && !(clientSoc.Poll(1, SelectMode.SelectRead) && clientSoc.Available == 0))
            {
                try
                {
                    if (gameCond == "")
                    {
                        SetRichText("Ready waiting for opponent." + Environment.NewLine);

                        gameCond = ReceiveName();

                        SetRichText("Game is started!" + Environment.NewLine);
                    }
                    if (gameCond == "start")
                    {
                        turn = ReceiveName();
                        if (turn.Contains("Your Turn create a question"))
                        {
                            SetVisibility(textBox3, true);//visibility changes...
                            SetVisibility(textBox4, true);//visibility changes...
                            isTurn = true;
                            SetRichText("Server: " + turn + Environment.NewLine); // when server send message.
                        }
                        else
                        {
                            isTurn = false;
                            SetVisibility(textBox3, true);//visibility changes...
                            String question = ReceiveName();
                            SetRichText("Server: your question: " + question + Environment.NewLine); // when server send message.

                        }
                    }
                }
                catch (SocketException e)
                {
                    SetRichText("Server is down!" + Environment.NewLine); // if is not still connected or some problem with connection.
                    connected = false;
                    clientSoc.Close();
                }
            }
            if (clientSoc.Poll(1, SelectMode.SelectRead) && clientSoc.Available == 0)
            {
                SetRichText("Game is over!" + Environment.NewLine); // if is not still connected or some problem with connection.
                clientSoc.Close();
                Thread.Sleep(5000);
                System.Windows.Forms.Application.Exit();
            }
        }

        String ReceiveName() // return name.
        {
            try
            {
                clientSoc.Receive(buffer);
                string name = Encoding.ASCII.GetString(buffer);
                Array.Clear(buffer, 0, buffer.Length);
                int index = name.IndexOf("\0");
                name = name.Substring(0, index);
                return name;
            }
            catch
            {
                SetRichText("Server is down! Sorry for that :(");
                Thread.Sleep(5000);
                clientSoc.Close();
                System.Windows.Forms.Application.Exit();
            }
            return "exception";
        }


        private void textBox5_TextChanged(object sender, EventArgs e)
        {

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

        public static void SetVisibility(TextBox t, bool v)// changes the visibility of the given 
        {
            t.Invoke((MethodInvoker)delegate
            {
                // Running on the UI thread
                t.Visible = v;
            });

        }

        public static void textMakeEmpty(TextBox t)// changes the visibility of the given 
        {
            t.Invoke((MethodInvoker)delegate
            {
                // Running on the UI thread
                t.Text = "";
            });

        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (isTurn && this.textBox4.Text != "" && this.textBox3.Text != "")
            {
                clientSoc.Send(Encoding.Default.GetBytes(this.textBox4.Text)); // send question.
                clientSoc.Send(Encoding.Default.GetBytes(this.textBox3.Text));//send answer
                SetVisibility(textBox4, false); // question.
                SetVisibility(textBox3, false);
                textMakeEmpty(textBox4);
                textMakeEmpty(textBox3);
                this.label6.Visible = false;
                SetRichText("Server: Your question and answer received to your opponent." + Environment.NewLine);
            }
            else if (!isTurn && this.textBox3.Text != "")
            {
                clientSoc.Send(Encoding.Default.GetBytes(this.textBox3.Text));//send answer
                SetVisibility(textBox3, false);
                textMakeEmpty(textBox3);
                this.label6.Visible = false;
                SetRichText("Server: Your answer received to your opponent" + Environment.NewLine);
            }
            else
            {
                this.label7.Visible = true;
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            clientSoc.Close();
            Close();
        }
    }
}
