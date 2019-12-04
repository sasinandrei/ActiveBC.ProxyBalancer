using ActiveBC.ProxyBalancer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;

namespace ActiveBC.ProxyBalancer.Tests
{
    public class ProxyService
    {
        [Fact]
        public void ProxyService_Connect()
        {
            var mockRepository = new MockRepository(MockBehavior.Default);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:11001"),
                    new KeyValuePair<string, string>("Address", "localhost"),
                    new KeyValuePair<string, string>("Port", "11000"),
                    new KeyValuePair<string, string>("Backlog", "100")
                })
                .Build();
            var balancer = mockRepository.Create<IBalancer>();
            balancer.Setup(x => x.AllocateServer()).Returns("localhost:11001").Verifiable();
            var logger = new Mock<ILogger<Services.ProxyService>>();
            
            var ipHostInfo = Dns.GetHostEntry("localhost");
            var localEndPoint = new IPEndPoint(ipHostInfo.AddressList.First(), 11001);
            using var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(1);

            var proxyService = new Services.ProxyService(configuration, balancer.Object, logger.Object);
            var mainThread = new Thread(proxyService.Run);
            mainThread.Start();
            SpinWait.SpinUntil(() => proxyService.IsReady);

            using (var testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                testSocket.Connect("localhost", 11000);
                Assert.True(testSocket.Connected);
            }

            proxyService.Stop();
            mockRepository.VerifyAll();
        }
    }
}
