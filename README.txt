Server:
	Accept() :
	For accepting client sockets and it controls number of sockets to find the exact number (For this step its 2) of needed clients. When the number of the client is 2, then GameStart() will be called in another thread. Also,1 in this thread, we check username duplicate error. Also, we surround this part with try catch to handle Exceptions.
	GameStart():
	This thread initiates the game. Sends all sockets a Start message. The client who has the alphabetically first is selected as first player. First player send server their question and answer. Server receives the question and answer and sends the question to the other client. Then waits for the answer of this client. When server receives the answer, checks whether it is correct or not. Prints the corresponding result to the its rich textbox. Then server swaps the turn of the clients. In this thread also, the program controls whether a client or clients disconnected or not. If one of the client is disconnected, server closes all the connected clients and terminates itself.(You can see all in the GUI part, And if there is a connection lost then or catch GUI will terminate it self. For your control we put some sleep func for thread). 
Client:
	ReceiveName() :
	This function takes message from server parses it in order to parse the used part of the bytes. We clear buffer to be used for another message.
	Receive () :
	It’s the thread where the game starts . Waits for the message from server to start the game .
	When the client receives “ Your Turn create a question “ we enabled GUI boxes for client to insert question and answer . We checked for both boxes to be filled up by the client. It gives error message if one of the boxes is empty.

	button2_click () //Send button for question
	Checks the turn of the client whether it is turn to send question and answer or its turn to answer the question that sent by the server. Ýf its turn to send question and answer, sends the question and answer to the server then makes question and answer boxes invisible.
	If its turn to answer the question sends the answer to the server and makes invisible necessary boxes.

	button1_click() //Send button for connecting to server
	Checks the port number box, IP box and names box to be filled up . If they are empty gives warning. If it successfully connects to the server send the name to the server in order to check whether is there a same named person connected to the server. If there is person with same name ask for chose different name if there is not calls Receive thread.
