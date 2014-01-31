using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZooKeeperConsole
{
    public class ConfigSettingWatcher : IWatcher
    {
        public virtual void Process(WatchedEvent @event)
        {
            if (!VirtualDevice.ReReadSetting(@event.Path))
            {
                string msg = "Error : Event Triggered for Path (" + @event.Path + " not in Device Config";
                VirtualDevice.Log(msg);
                throw new Exception(msg);
            }
        }
    }
}
