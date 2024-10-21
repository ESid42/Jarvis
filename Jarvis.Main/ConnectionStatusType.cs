using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Main
{
    public enum ConnectionStatusType
    {
        Connected,
        Disconnected,
        Connecting,
        Disconnecting,
        WaitingForServer,
        WaitingForClient,
        Closed,
        Closing
    }
}
