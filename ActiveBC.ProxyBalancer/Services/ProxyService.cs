using ActiveBC.ProxyBalancer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ActiveBC.ProxyBalancer.Services
{
    public class ProxyService
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private readonly BalancerConfiguration _configuration;
        private readonly IBalancer _balancer;
        private readonly ILogger<ProxyService> _logger;

        public bool IsReady { get; private set; }
        private bool stopThread;

        public ProxyService(IConfiguration configuration, IBalancer balancer, ILogger<ProxyService> logger)
        {
            _configuration = configuration.Get<BalancerConfiguration>();
            _logger = logger;
            _balancer = balancer;
        }

        public void Run()
        {
            _logger.LogInformation($"Run on {_configuration.Address}:{_configuration.Port}");
            var ipHostInfo = Dns.GetHostEntry(_configuration.Address);
            var localEndPoint = new IPEndPoint(ipHostInfo.AddressList.First(), _configuration.Port);
            using var listener = new Socket(ipHostInfo.AddressList.First().AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(_configuration.Backlog);
                IsReady = true;
                while (!stopThread)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                IsReady = false;
                _logger.LogError(e.ToString());
            }
        }

        public void Stop()
        {
            stopThread = true;
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();

            var handler = ((Socket)ar.AsyncState).EndAccept(ar);
            var state = new StateObject
            {
                ClientSocket = handler,

                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),

                Url = _balancer.AllocateServer()
            };

            state.ServerSocket.Connect(state.Url.Split(":").First(), int.Parse(state.Url.Split(':').Skip(1).FirstOrDefault() ?? "9000"));

            try
            {
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ClientReadCallback), state);
                state.ServerSocket.BeginReceive(state.ServerBuffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ServerReadCallback), state);
            }
            catch (ObjectDisposedException)
            {
                // socket was closed
            }
        }

        public void ClientReadCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            var handler = state.ClientSocket;

            try
            {
                var bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.ServerSocket.Send(state.Buffer, bytesRead, SocketFlags.None);
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ClientReadCallback), state);
                } 
                else
                {
                    _balancer.RemoveConnection(state.Url);
                    CloseConnections(state);
                }
            } 
            catch (Exception)
            {
                _balancer.RemoveConnection(state.Url);
                CloseConnections(state);
            }
        }

        public void ServerReadCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            var handler = state.ServerSocket;

            try
            {
                var bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.ClientSocket.Send(state.ServerBuffer, bytesRead, SocketFlags.None);
                    handler.BeginReceive(state.ServerBuffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ServerReadCallback), state);
                } 
                else
                {
                    CloseConnections(state);
                }
            }
            catch (ObjectDisposedException) 
            {
                // socket was closed
            }
            catch (Exception e)
            {
                CloseConnections(state);
                _logger.LogError(e.ToString());
            }
        }

        private void CloseConnections(StateObject state)
        {
            CloseClientConnection(state);
            CloseServerConnection(state);
        }

        private void CloseClientConnection(StateObject state)
        {
            try
            {
                if (state.ClientSocket != null)
                {
                    state.ClientSocket.Shutdown(SocketShutdown.Both);
                    state.ClientSocket.Close();
                }
            }
            catch (ObjectDisposedException)
            {
                // socket was closed
            }
        }

        private void CloseServerConnection(StateObject state)
        {
            try
            {
                if (state.ServerSocket != null)
                {
                    state.ServerSocket.Shutdown(SocketShutdown.Both);
                    state.ServerSocket.Close();
                }
            }
            catch (ObjectDisposedException)
            {
                // socket was closed
            }
        }
    }
}
