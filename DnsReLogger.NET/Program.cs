using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsReLogger.NET
{
    enum Mode
    {
        Service,
        Logger
    }
    enum Action
    {
        Install,
        Uninstall,
        StartLog,
        StopLog
    }
    enum Option
    {
        Live,
        Log,
        Setup
    }
    class Config
    {
        public Mode mode;
        public Action action;
        public Option option;
        public Config(string[] args)
        {
            foreach(var arg in args)
            {
                if (arg == "--service")
                    mode = Mode.Service;
                else if (arg == "--logger")
                    mode = Mode.Logger;
                else if (arg == "--start")
                    action = Action.StartLog;
                else if (arg == "--stop")
                    action = Action.StopLog;
                else if (arg == "--live")
                    option = Option.Live;
                else if (arg == "--log")
                    option = Option.Log;
                else if (arg == "--setup")
                    option = Option.Setup;
                else
                    throw new ArgumentException("Unknown argument");
            }
        }
    }
    internal class Program
    {
        
        static void Main(string[] args)
        {
            Config config = new Config(args);
            if (config.mode == Mode.Service)
            {
                if (config.action == Action.Install)
                    DnsReloggerService.Install();
                else if (config.action == Action.Uninstall)
                    DnsReloggerService.Uninstall();
                else
                    Help();
            }
            else if(config.mode == Mode.Logger)
            {
                switch(config.action)
                {
                    case Action.StartLog:
                        break;
                    case Action.StopLog:
                        break;
                    default:
                        Help();
                        break;
                }
            }
            // Setup the args
            // install/uninstall the service

        }

        static void Help()
        {
            // TODO(will): Finish this
            throw new NotImplementedException();
        }

    }
}
