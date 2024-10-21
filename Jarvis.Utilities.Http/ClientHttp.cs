namespace Jaris.Utilities.Http
{
    /// <summary>
    /// HTTP client for basic operations
    /// </summary>
    public class ClientHttp : IDisposable
    {
        private readonly HttpClient _client;
        private bool disposedValue;

        /// <summary>
        ///
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        ///
        public ClientHttp(string ip) : this(new ClientSettingsBase(ip))
        {
        }

        public ClientHttp(IClientHttpSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = new HttpClient();

            Init();
        }

        /// <summary>
        /// client settings
        /// </summary>
        public IClientHttpSettings Settings { get; set; }

        /// <summary>
        /// Base URL
        /// </summary>
        public string URL => Settings.EndpointURL;

        /// <summary>
        /// Deletes request.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> DeleteRequest(string url)
        {
            string endpointPath = URL + url;
            HttpResponseMessage res = await _client.DeleteAsync(url);
            if (!res.IsSuccessStatusCode) throw new ArgumentException($"The path {endpointPath} gets the following status code: " + res.StatusCode);
            return await res.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Disposes instance.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets request.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> GetRequest(string url)
        {
            string endpointPath = URL + url;

            HttpResponseMessage res = await _client.GetAsync(endpointPath);
            if (!res.IsSuccessStatusCode) throw new ArgumentException($"The path {endpointPath} gets the following status code: " + res.StatusCode);
            return await res.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Post request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> PostRequest(string url, HttpContent? content = null)
        {
            string endpointPath = URL + url;
            HttpResponseMessage res = await _client.PostAsync(url, content);
            if (!res.IsSuccessStatusCode) throw new ArgumentException($"The path {endpointPath} gets the following status code: " + res.StatusCode);
            return await res.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Puts request.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<string> PutRequest(string url, HttpContent? content = null)
        {
            string endpointPath = URL + url;

            HttpResponseMessage res = await _client.PutAsync(url, content);
            if (!res.IsSuccessStatusCode) throw new ArgumentException($"The path {endpointPath} gets the following status code: " + res.StatusCode);
            return await res.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Diposes instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected virtual void Ini()
        {
        }

        private void Init()
        {
            _client.BaseAddress = new(Settings.EndpointURL);

            Ini();
        }
    }
}