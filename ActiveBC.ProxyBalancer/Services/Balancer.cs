using ActiveBC.ProxyBalancer.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ActiveBC.ProxyBalancer.Services
{
    public class Balancer : IBalancer
    {
        private readonly IReadOnlyList<ServerState> _servers;
        private readonly object _serversLocker = new object();

        public Balancer(IConfiguration configuration)
        {
            var conf = configuration.Get<BalancerConfiguration>();
            var servers = new List<ServerState>();
            foreach (var server in conf.Servers)
            {
                servers.Add(new ServerState()
                {
                    Url = server,
                    ConnectionsCount = 0
                });
            }
            _servers = servers;
        }

        public string AllocateServer()
        {
            ServerState server = null;
            lock (_serversLocker)
            {
                server = _servers.First(x => x.ConnectionsCount == _servers.Min(x => x.ConnectionsCount));
                server.ConnectionsCount++;
            }
            return server.Url;
        }

        public void RemoveConnection(string url)
        {
            var server = _servers.First(x => x.Url == url);
            lock (_serversLocker)
            {
                server.ConnectionsCount--;
            }
        }

        public int GetConnectionsCount(string url) => _servers.First(x => x.Url == url).ConnectionsCount;
    }
}
