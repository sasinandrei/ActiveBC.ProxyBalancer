using System.Collections.Generic;

namespace ActiveBC.ProxyBalancer.Models
{
    public class BalancerConfiguration
    {
        public int Backlog { get; set; }

        public int Port { get; set; }

        public string Address { get; set; }

        public ICollection<string> Servers { get; set; }
    }
}
