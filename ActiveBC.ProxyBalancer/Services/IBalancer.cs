namespace ActiveBC.ProxyBalancer.Services
{
    public interface IBalancer
    {
        string AllocateServer();
        void RemoveConnection(string url);
    }
}
