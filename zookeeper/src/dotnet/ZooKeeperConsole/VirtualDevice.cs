using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Microsoft.SqlServer.Server;
using Org.Apache.Zookeeper.Data;
using Org.Apache.Zookeeper.Proto;
using ZooKeeperConsole.IniReader;
using ZooKeeperNet;

namespace ZooKeeperConsole
{
    
    public static class VirtualDevice
    {
        // zookeeper vars
        private static ZooKeeper _zkClient = null;
        private static string _connectionString = null;
        private static int _timeout = 0;


        // device vars
        private static string _deviceId;
        private static string _deviceMake;
        private static string _deviceModel;

        // defaults
        private static string _defaultCommandValue = "Command";

        // Nodes
        private static string _root;
        private static string _firmwareRoot;
        private static string _deviceRoot;
        private static string _configRoot;
        private static string _deviceLogNode;
        private static string _deviceCommandsNode;
        

        private static List<ConfigSetting> _deviceConfiguration;


        public static bool ReReadSetting(string node)
        {
            foreach (var dc in _deviceConfiguration)
            {
                if (node.Equals(dc.Path))
                {
                    Console.WriteLine("\nConfig Value Changed on Server : " + node + "\n");

                    IWatcher newWatcher = new ConfigSettingWatcher();
                    
                    string decStr = GetStringFromByteArray(_zkClient.GetData(node, newWatcher, null));
                    dc.Value = decStr;
                    dc.SetNewWatch(newWatcher);
                        
                    Log("DeviceConfig  Setting (" + node + ") Updated");

                    PrintMainMenu();

                    return true;
                }
            }

            return false;
        }

        public static void PrintMainMenu()
        {
            Console.WriteLine();
            Console.WriteLine("What do you want to do?");
            Console.WriteLine("=======================");
            Console.WriteLine("p. Print Config");
            Console.WriteLine("q. Quit");
            Console.WriteLine("=======================");
            Console.Write("Please Select An Option : ");
        }

        public static bool RegisterForCommands()
        {
            string commandAction = GetStringFromByteArray(_zkClient.GetData(_deviceCommandsNode, null, null));
            if (!commandAction.Equals(_defaultCommandValue))
            {
                ActionCommand();
            }

            ResetCommandWatcher();
            return true;
        }

        public static bool ActionCommand()
        {
            bool response = false;
            string commandAction = GetStringFromByteArray(_zkClient.GetData(_deviceCommandsNode, null, null));
            
            if (commandAction == "NOTHING")
            {
                Console.WriteLine("Doing Nothing");
                Log("Command Received to do Nothing");
                response = true;
            }
            else if (commandAction == "UPDATE_FIRMWARE")
            {
                // todo - get firmware filename here
                Console.WriteLine("Firmware Update Avaliable");
                Log("Command Received to Upgrade Firmware to v NUM using FILENAME");
                response = true;
            }
            else
            {
                Log("Unrecognised Command Received (" + commandAction + ")");
                response = false;
            }

            return response;
        }


        /// <summary>
        /// Load Connect and Register to ZooKeeper
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool LoadConnectAndRegister(string filePath)
        {
            return LoadIniFile(filePath) && Connect() && RegisterDevice();
        }


        /// <summary>
        /// Write to Device Log
        /// </summary>
        /// <param name="deviceNode"></param>
        /// <param name="logMessage"></param>
        public static void Log(string logMessage)
        {
            if (_zkClient == null)
            {
                return;
            }

            logMessage = System.Net.WebUtility.UrlEncode(logMessage);
            CreatePermanentSequentialNode(_deviceLogNode, logMessage);
        }


        /// <summary>
        /// Remove Device From ZooKeeper
        /// </summary>
        public static void RemoveDevice()
        {
            Log("Closing ZooKeeper Connection");
            _zkClient.Dispose();
        }


        /// <summary>
        /// Print Loaded Config
        /// </summary>
        public static void PrintConfig()
        {
            Console.WriteLine("Device (" + _deviceId + ") Config ");
            foreach (var setting in _deviceConfiguration)
            {
                setting.Print();
            }
        }


        /// <summary>
        /// Read Config
        /// </summary>
        public static void ReadConfig()
        {
            IEnumerable<string> configSettings;
            configSettings = _zkClient.GetChildren(_configRoot, null);
            _deviceConfiguration = new List<ConfigSetting>();

            foreach (var setting in configSettings)
            {
                IWatcher tmpWatcher = new ConfigSettingWatcher();
                string urlDec = GetStringFromByteArray(_zkClient.GetData(_configRoot + "/" + setting, tmpWatcher, null));
                _deviceConfiguration.Add(new ConfigSetting(_configRoot + "/" + setting, urlDec, tmpWatcher));
            }

            Log("Read Device Configuration");

        }

        
        #region PRINT METHODS

        /// <summary>
        /// Print Info About ZooKeeper connection
        /// </summary>
        public static void PrintInfo()
        {
            Console.WriteLine("ZooKeeper Client Connected");
            Console.WriteLine("\t - Address : " + _connectionString);
            Console.WriteLine("\t - Timeout : " + _timeout);
        }


        /// <summary>
        /// PrintDeviceInfo
        /// </summary>
        public static void PrintDeviceInfo()
        {
            Console.WriteLine("------------------ Device Info ------------------");
            Console.WriteLine("- Device Id    : \t " + _deviceId);
            Console.WriteLine("- Device Make  : \t " + _deviceMake);
            Console.WriteLine("- Device Model : \t " + _deviceModel);
            Console.WriteLine("-------------------------------------------------");
        }


        #endregion PRINT METHODS


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///     PRIVATE METHODS
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

        #region PRIVATE METHODS

        private static bool LoadIniFile(string filePath)
        {
            // Load The Ini File - Inifile Info http://www.tectite.com/fmhowto/inifile.php
            const string iniFilePath = "D:\\cszk\\deviceConfig.ini";
            IniFile iniFile = new IniFile(iniFilePath);

            if (iniFile == null)
            {
                throw new Exception("Error : IniFile (" + iniFilePath + ") did not open");
            }

            // Device Info
            _deviceMake = iniFile.IniReadValue("DeviceInfo", "DeviceMake");
            _deviceModel = iniFile.IniReadValue("DeviceInfo", "DeviceModel");
            _deviceId = iniFile.IniReadValue("DeviceInfo", "DeviceId");


            // Read Zookeeper Info
            _connectionString = iniFile.IniReadValue("ZooKeeperInfo", "Address");
            string strTimout = iniFile.IniReadValue("ZooKeeperInfo", "Timeout");
            _timeout = Convert.ToInt32(strTimout);

            SetDeviceNodes();

            PrintDeviceInfo();
            
            return true;
        }


        private static void SetDeviceNodes()
        {
            _root = "/Device/" + _deviceMake + "/" + _deviceModel;
            _firmwareRoot = _root + "/Firmware";
            _deviceRoot = _root + "/" + _deviceId;
            _configRoot = _deviceRoot + "/Config";
            _deviceLogNode = _deviceRoot + "/Log/LogMessage";
            _deviceCommandsNode = _deviceRoot + "/Command";
        }


        private static bool Connect()
        {
            if (_zkClient == null)
            {
                _zkClient = new ZooKeeper(_connectionString, new TimeSpan(0, 0, _timeout), null);
                Log("Connected to ZooKeeper");
                Console.WriteLine("Connected to ZooKeeper");
                PrintInfo();
            }
            else
            {
                Console.WriteLine("Connection to ZooKeeper Already Exists");
            }

            return _zkClient != null;
        }


        private static bool RegisterDevice()
        {
            if (_zkClient == null)
            {
                Console.WriteLine("Not yet connected to Zookeeper");
                return false;
            }
            CreateSessionNode();
            Log("Device Connected at " + DateTime.Now.ToString());
            Console.WriteLine("Device (" + _deviceId + ") registered on ZooKeeper");
            return true;
        }

        #endregion PRIVATE METHODS


        private static void CreatePermanentSequentialNode(string nodePath, string data)
        {
            if (data == null || data.IsEmpty())
            {
                _zkClient.Create(nodePath, null, Ids.OPEN_ACL_UNSAFE, CreateMode.PersistentSequential);
            }
            else
            {
                _zkClient.Create(nodePath, Encoding.ASCII.GetBytes(data), Ids.OPEN_ACL_UNSAFE, CreateMode.PersistentSequential);
            }
        }


        private static void CreatePermanentNode(string nodePath, string data)
        {
            if (data == null || data.IsEmpty())
            {
                _zkClient.Create(nodePath, null, Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
            else
            {
                _zkClient.Create(nodePath, Encoding.ASCII.GetBytes(data), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
        }


        private static void CreateSessionNode()
        {
            _zkClient.Create(_deviceRoot + "/Online", Encoding.ASCII.GetBytes(DateTime.Now.ToString()), Ids.OPEN_ACL_UNSAFE, CreateMode.Ephemeral);
        }


        private static string GetStringFromByteArray(byte[] value)
        {
            string strValue = System.Text.Encoding.UTF8.GetString(value);
            string decStrValue = System.Net.WebUtility.UrlDecode(strValue);
            return decStrValue;
        }


        public static void ResetCommandWatcher()
        {
            byte[] bytes = _defaultCommandValue.GetBytes();
            _zkClient.SetData(_deviceCommandsNode, bytes, -1);
            IWatcher commandsWatcher = new DeviceCommandsWatcher();
            GetStringFromByteArray(_zkClient.GetData(_deviceCommandsNode, commandsWatcher, null));
        }

    }
}
