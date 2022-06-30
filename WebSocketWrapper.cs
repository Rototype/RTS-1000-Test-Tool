using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RTS_1000_Test_Tool
{
    public class WebSocketWrapper
    {
        private const int ReceiveChunkSize = 4096;
        private const int SendChunkSize = 1024;

        private readonly ClientWebSocket _ws;
        private readonly Uri _uri;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        private Action<int> _onConnected;
        private Action<int> _onDisconnected;
        private Action<int,string> _onMessage;
        private Action<int,string> _onException;
        private int _id;

        protected WebSocketWrapper(string uri, int id)
        {
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _uri = new Uri(uri);
            _cancellationToken = _cancellationTokenSource.Token;
            _id = id;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server.</param>
        /// <returns></returns>
        public static WebSocketWrapper Create(string uri, int id)
        {
            return new WebSocketWrapper(uri,id);
        }

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <returns></returns>
        public WebSocketWrapper Connect()
        {
            ConnectAsync();
            return this;
        }

        /// <summary>
        /// Information on the WebSocket SubProtocol.
        /// </summary>
        /// <returns></returns>
        public string SubProtocol
        {
            get
            {
                if (_ws != null)
                    return _ws.SubProtocol;
                else
                    return "Not available";
            }
        }

        /// <summary>
        /// Information on the WebSocket Status.
        /// </summary>
        /// <returns></returns>
        public WebSocketState SocketStatus
        {
            get
            {
                if (_ws != null)
                    return _ws.State;
                else
                    return WebSocketState.None;
            }
        }

        public ClientWebSocket InstanceObject
        {
            get
            {
                if (_ws != null)
                    return _ws;
                else
                    return null;
            }
        }

        /// <summary>
        /// Set the Action to call when the connection has been established.
        /// </summary>
        /// <param name="onConnect">The Action to call.</param>
        /// <returns></returns>
        public void OnConnect(Action<int> onConnect)
        {
            _onConnected = onConnect;
        }

        /// <summary>
        /// Set the Action to call when the connection has been terminated.
        /// </summary>
        /// <param name="onDisconnect">The Action to call</param>
        /// <returns></returns>
        public void OnDisconnect(Action<int> onDisconnect)
        {
            _onDisconnected = onDisconnect;
        }

        /// <summary>
        /// Set the Action to call when a messages has been received.
        /// </summary>
        /// <param name="onMessage">The Action to call.</param>
        /// <returns></returns>
        public void OnMessage(Action<int,string> onMessage)
        {
            _onMessage = onMessage;
        }

        /// <summary>
        /// Set the Action to call when an exception has been received.
        /// </summary>
        /// <param name="onException">The Action to call.</param>
        /// <returns></returns>
        public void OnException(Action<int,string> onException)
        {
            _onException = onException;
        }

        /// <summary>
        /// Send a message to the WebSocket server.
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendMessage(string message)
        {
            SendMessageAsync(message);
        }

        private async void SendMessageAsync(string message)
        {
            if (_ws.State != WebSocketState.Open)
            {
                CallOnException("WebSocket is not open");
            }
            else
            {
                var messageBuffer = Encoding.UTF8.GetBytes(message);

                var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

                for (var i = 0; i < messagesCount; i++)
                {
                    var offset = (SendChunkSize * i);
                    var count = SendChunkSize;
                    var lastMessage = ((i + 1) == messagesCount);

                    if ((count * (i + 1)) > messageBuffer.Length)
                    {
                        count = messageBuffer.Length - offset;
                    }

                    await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
                }
            }
        }

        private async void ConnectAsync()
        {
            bool onerror = false;
            try
            {
                await _ws.ConnectAsync(_uri, _cancellationToken);
                CallOnConnected();
                StartListen();
            }
            catch (Exception conex)
            {
                onerror = true;
                string excinfo = String.Format("\"{0}\"", conex.Message);
                CallOnException(excinfo);
            }
            finally
            {
                if (onerror)
                    _ws.Dispose();
            }
        }

        private async void StartListen()
        {
            var buffer = new byte[ReceiveChunkSize];
            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // In case of server-initiated closing request, WebSocketState is marked "CloseReceived" 
                            //  and CloseAsync method sends a message to the client to close the connection, 
                            // waits for a response, and then returns. 
                            // The server does not wait for any additional data sent by the client.
                            if (_ws.State != WebSocketState.Closed)
                            {
                                await
                                    _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            }
                            CallOnDisconnected();
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }
                    } while (!result.EndOfMessage);

                    CallOnMessage(stringResult);
                }
            }
            catch (Exception)
            {
                CallOnDisconnected();
            }
            finally
            {
                _ws.Dispose();
            }
        }

        private void CallOnConnected()
        {
            if (_onConnected != null)
                RunInTask(() => _onConnected(_id));
        }

        private void CallOnDisconnected()
        {
            if (_onDisconnected != null)
                RunInTask(() => _onDisconnected(_id));
        }

        private void CallOnMessage(StringBuilder stringResult)
        {
            if (_onMessage != null)
                RunInTask(() => _onMessage(_id,stringResult.ToString()));
        }

        private void CallOnException(string info)
        {
            if (_onException != null)
                RunInTask(() => _onException(_id,info));
        }

        private static void RunInTask(Action action)
        {
            Task.Factory.StartNew(action);
        }
    }
}

