using ActiveBC.ProxyBalancer.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace ActiveBC.ProxyBalancer.Tests
{
    public class BalancerTests
    {
        [Fact]
        public void Balancer_AllocateServer()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:9000")
                })
                .Build();

            var balancer = new Balancer(configuration);

            var url = balancer.AllocateServer();
            Assert.Equal(1, balancer.GetConnectionsCount(url));
        }

        [Fact]
        public void Balancer_RemoveConnection()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:9000")
                })
                .Build();

            var balancer = new Balancer(configuration);

            var url = balancer.AllocateServer();
            Assert.Equal(1, balancer.GetConnectionsCount(url));
            balancer.RemoveConnection(url);
            Assert.Equal(0, balancer.GetConnectionsCount(url));
        }

        [Fact]
        public void Balancer_Multithread()
        {
            var configuration = new ConfigurationBuilder()
                   .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:9000")
                   })
                   .Build();

            var balancer = new Balancer(configuration);

            var pool = new List<Thread>();
            for (var i = 0; i < 1500; i++)
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(new Random().Next(0, 50));
                    var url = balancer.AllocateServer();
                });
                thread.Start();
                pool.Add(thread);
            }
            SpinWait.SpinUntil(() => pool.All(x => !x.IsAlive));

            Assert.Equal(1500, balancer.GetConnectionsCount("localhost:9000"));
        }

        [Fact]
        public void Balancer_Multithread_Disconnect()
        {
            var configuration = new ConfigurationBuilder()
                   .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:9000")
                   })
                   .Build();

            var balancer = new Balancer(configuration);

            var pool = new List<Thread>();
            for (var i = 0; i < 1500; i++)
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(new Random().Next(0, 150));
                    var url = balancer.AllocateServer();
                });
                thread.Start();
                pool.Add(thread);
            }

            for (var i = 0; i < 1500; i++)
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(new Random().Next(0, 50));
                    balancer.RemoveConnection("localhost:9000");
                });
                thread.Start();
                pool.Add(thread);
            }
            SpinWait.SpinUntil(() => pool.All(x => !x.IsAlive));

            Assert.Equal(0, balancer.GetConnectionsCount("localhost:9000"));
        }

        [Fact]
        public void Balancer_Multithread_MultipleServers()
        {
            var configuration = new ConfigurationBuilder()
                   .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:9000"),
                    new KeyValuePair<string, string>("Servers:1", "localhost:9001")
                   })
                   .Build();

            var balancer = new Balancer(configuration);

            var pool = new List<Thread>();
            for (var i = 0; i < 1500; i++)
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(new Random().Next(0, 50));
                    var url = balancer.AllocateServer();
                });
                thread.Start();
                pool.Add(thread);
            }
            SpinWait.SpinUntil(() => pool.All(x => !x.IsAlive));

            Assert.Equal(750, balancer.GetConnectionsCount("localhost:9000"));
            Assert.Equal(750, balancer.GetConnectionsCount("localhost:9001"));
        }

        [Fact]
        public void Balancer_Multithread_Disconnect_MultipleServers()
        {
            var configuration = new ConfigurationBuilder()
                   .AddInMemoryCollection(new[] {
                    new KeyValuePair<string, string>("Servers:0", "localhost:9000"),
                    new KeyValuePair<string, string>("Servers:1", "localhost:9001")
                   })
                   .Build();

            var balancer = new Balancer(configuration);

            var pool = new List<Thread>();
            for (var i = 0; i < 1500; i++)
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(new Random().Next(0, 150));
                    var url = balancer.AllocateServer();
                    Thread.Sleep(new Random().Next(0, 150));
                    balancer.RemoveConnection(url);
                });
                thread.Start();
                pool.Add(thread);
            }

            SpinWait.SpinUntil(() => pool.All(x => !x.IsAlive));

            Assert.Equal(0, balancer.GetConnectionsCount("localhost:9000"));
            Assert.Equal(0, balancer.GetConnectionsCount("localhost:9000"));
        }

    }
}
