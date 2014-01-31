using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZooKeeperConsole
{
    public class ConfigSetting
    {
        // vars
        public string Path { get; set; }
        public string Value { get; set; }
        private IWatcher _watcher;

        // Ctr
        public ConfigSetting(string path, string value, IWatcher watcher)
        {
            Path = path;
            Value = value;
            _watcher = watcher;
        }

        // Print
        public void Print()
        {
            Console.WriteLine(this.ToString());
        }

        // ToString
        public override string ToString()
        {
            string ret = this.Path + " : " + this.Value;
            return ret;
        }

        // Apply New Watch
        public void SetNewWatch(IWatcher watch)
        {
            _watcher = watch;
        }
    }
}
