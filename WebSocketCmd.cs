using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;


namespace RTS_1000_Test_Tool
{
    public class WebSocketCmd
    {

        public enum StateEnum { connected, fail };
        public WebSocketState state;
        public ClientWebSocket? publisher_socket;
        public ClientWebSocket? BarcodeReader_socket;
        public CancellationTokenSource cancels;
        bool WebSocketPresent;

        // costruttore

        public WebSocketCmd()
        {
            cancels = new CancellationTokenSource();
            cancels.CancelAfter(40_000);
            WebSocketPresent = false;
        }

        public async Task WebSocketConnect(string url)
        {

          try
            {
                //
                // create websocket  
                //
                if (!WebSocketPresent)
                {
                    publisher_socket = new ClientWebSocket();

                    //
                    // connection to publisher endpoint 
                    //
                    await publisher_socket.ConnectAsync(new Uri($"{url}"), cancels.Token);

                    state = publisher_socket.State;
                    WebSocketPresent = true;
                }

            }
            
            catch (WebSocketException)
            { }
            catch (System.Net.HttpListenerException)
            { }
            catch (TaskCanceledException)
            { }

        }
        public async Task WebSocketDisConnect()
        {
            //await publisher_socket.CloseAsync(WebSocketCloseStatus.NormalClosure,"", cancels.Token );

        }
        public async Task<int> GetServices(string cmd)
        {
            if(publisher_socket != null)
                await SendAndWaitForCompletionAsync(publisher_socket,cmd);
            return (0);
        }
        public async Task SendCommandToService(string cmd)
        {
            try
            {
                //
                // create websocket  
                //
                BarcodeReader_socket = new ClientWebSocket();

                //
                // connection to publisher endpoint 
                //
                //await BarcodeReader_socket.ConnectAsync(new Uri($"{url}"), cancels.Token);
                await BarcodeReader_socket.ConnectAsync(new Uri($"ws://localhost:5846//xfs4iot/v1.0/barcodereader"), cancels.Token);
                
                state = BarcodeReader_socket.State;

                await SendAndWaitForCompletionAsync(BarcodeReader_socket, cmd);

            }

            catch (WebSocketException)
            { }
            catch (System.Net.HttpListenerException)
            { }
            catch (TaskCanceledException)
            { }

        }
        



    //
    //  Funzioni di comunicazione   
    //
    //


        public async Task<object?> SendAndWaitForCompletionAsync( ClientWebSocket socket, string cmd )
        {
            await SendCommandAsync(socket,cmd);

            object? cmdResponse = await ReceiveMessageAsync(socket);
            if (cmdResponse is Acknowledge)
                cmdResponse = await ReceiveMessageAsync(socket);
            return cmdResponse;
        }
        public async Task SendCommandAsync(ClientWebSocket socket, string cmd)
        {
            if (socket.State != WebSocketState.Open)
                throw new Exception("Attempted to send a command to a WebSocket that wasn't open");
            var JSON = new ArraySegment<byte>(Encoding.UTF8.GetBytes(cmd));
            
            await socket.SendAsync(JSON, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private const int MessageBufferSize = 20 * 1024;
        public async Task<object?> ReceiveMessageAsync(ClientWebSocket socket ,CancellationToken CancelTaken = default)
        {
                 

            try
            {
                var buffer = new ArraySegment<byte>(new byte[MessageBufferSize]);

                // Get the next message
                var rc = await socket.ReceiveAsync(buffer, CancelTaken == default ? CancellationToken.None : CancelTaken);

                if (rc.MessageType == WebSocketMessageType.Text)
                {
                    // trim the incomming message and extract a string
                    var messageString = Encoding.UTF8.GetString(buffer.Take(rc.Count).ToArray());

                    // see if the decoder can decode the message
                    //if (!ResponseDecoder.TryUnserialise(messageString, out object message))
                    //{
                        throw new Exception($"Invalid JSON or unknown response received: {messageString}");
                    //}
                    //if (message == null)
                      //  throw new Exception("Internal error: Unexpected null");
                    //return message;
                }
            }
            catch (WebSocketException ex) //when (ex.InnerException is SocketException)
            {
                // closed connection and create new object
                socket.Dispose();
                socket = new ClientWebSocket();
            }
            catch (Exception ex) when (ex.InnerException is OperationCanceledException || ex is OperationCanceledException)
            {
                // Cancelled by the application but we want to keep connection up until explicitly socket is closed by the overlaying application.
                // throw an exception to the caller
                //await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the cancelled task.", CancellationToken.None);
                //socket.Dispose();
                //socket = new ClientWebSocket();
                //await ConnectAsync();
                //throw;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + " StackTrace:" + ex.StackTrace);
            }

            return null;
        }






        public int ConnectAll()
        {
            return (0);
        }
    }
}
