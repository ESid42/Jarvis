using Jarvis.Common;
using Jarvis.Utils;

namespace Jarvis.COM
{
    public interface IConnectable
    {
        #region Events

        event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

        #endregion Events

        #region Definitions

        bool IsConnected { get; }

        #endregion Definitions

        #region Methods

        Task<bool> Close();

        Task<bool> Start();

        Task<bool> Stop();

        #endregion Methods
    }
}