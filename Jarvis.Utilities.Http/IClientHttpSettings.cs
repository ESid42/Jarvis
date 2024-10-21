namespace Jaris.Utilities.Http
{
    /// <summary>
    /// ClientHttpSettings interface.
    /// </summary>
    public interface IClientHttpSettings
    {
        /// <summary>
        /// Gets or sets EndpointURL.
        /// </summary>
        public string EndpointURL { get; set; }
    }

    public class ClientSettingsBase : IClientHttpSettings
    {
        public string EndpointURL { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ClientSettingsBase(string endpointURL)
        {
            EndpointURL = endpointURL;
        }
    }
}