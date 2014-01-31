using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZooKeeperConsole
{
    public class DeviceCommandsWatcher : IWatcher
    {
        public void Process(WatchedEvent @event)
        {
            VirtualDevice.ActionCommand();
            VirtualDevice.ResetCommandWatcher();
            VirtualDevice.PrintMainMenu();
        }
    }
}
