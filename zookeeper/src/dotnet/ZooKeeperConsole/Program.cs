﻿using System;
using System.Linq;


namespace ZooKeeperConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("NUM ARGS : " + args.Count());

            string iniFilePath;
            if (!args.Any())
            {
                Console.WriteLine("Loading Default ini file");
                iniFilePath = "D:\\cszk\\deviceConfig.ini";
            }
            else
            {
                Console.WriteLine("Loading ini file :" + args[0]);
                iniFilePath = args[0];
            }

            // Load Ini and Connect to ZooKeeper
            if (!VirtualDevice.LoadConnectAndRegister(iniFilePath))
            {
                throw new Exception("Failed to Load, Connect and Register");
            }


            VirtualDevice.ReadConfig();
            VirtualDevice.RegisterForCommands();


            bool quit = false;

            {
                while (!quit)
                {
                    // Print User Menu
                    VirtualDevice.PrintMainMenu();
                    var userInput = Console.ReadLine();

                    // todo : Asser Key Press
                    if (userInput == "q")
                    {
                        quit = true;
                    }
                    else if (userInput == "p")
                    {
                        VirtualDevice.PrintConfig();
                    }
                    else
                    {
                        Console.WriteLine("Invlaid Entry. Please Make Another Selection");
                    }
                    
                }

                VirtualDevice.RemoveDevice();
                Console.WriteLine("Goodbye");
            }
            
            Console.WriteLine("Press a key to exit");
            Console.ReadKey();

        }
    }
}
