﻿using Constellation;
using Constellation.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bombi
{
    public class Program : PackageBase
    {
        private string MacAddress { get; set; }
        private int Rssi { get; set; }
        private string State { get; set; }
        private long TimeInMs { get; set; }
        private long TotalTimeInMs { get; set; }
        private List<Object> Components { get; set; }
        private List<Object> Instructions { get; set; }
        private string ConfigType { get; set; }
        private string Puzzle { get; set; }

        private static string[] allState = { "Idle", "Activated", "Paused", "Exploded", "Deactivated" };
        private static string ALL_MAC_ADDRESSES = "FF:FF:FF:FF:FF:FF";

        static void Main(string[] args)
        {
            PackageHost.Start<Program>(args);
        }

        public override void OnStart()
        {
            PackageHost.SubscribeStateObjects("*", "BombPartyServer", "*");

            PackageHost.StateObjectUpdated += PackageHost_StateObjectUpdated;

            Random random = new Random();

            this.MacAddress = macPart(random.Next(0, 16)) + ":" + macPart(random.Next(0, 16)) + ":" +
                macPart(random.Next(0, 16)) + ":" + macPart(random.Next(0, 16)) + ":" + macPart(random.Next(0, 16));
            this.Rssi = -random.Next(20, 90);
            this.State = allState[random.Next(0, allState.Length)];
            this.TotalTimeInMs = random.Next(100000, 500000);
            this.TimeInMs = this.TotalTimeInMs;
            this.Components = new List<Object>();
            this.Instructions = new List<Object>();
            this.ConfigType = "Handmade";
            this.Puzzle = "";


            for (int i = 0; i < 4; i++)
            {
                this.Components.Add(new
                {
                    Name = "Button_" + i,
                    Type = "Button",
                    State = "Down"
                });    
            }

            for (int i = 0; i < 4; i++)
            {
                this.Components.Add(new
                {
                    Name = "Green_Led_" + i,
                    Type = "Led",
                    State = "Low"
                });

                this.Components.Add(new
                {
                    Name = "Red_Led_" + i,
                    Type = "Led",
                    State = "High"
                });
            }

            //this.Instructions.Add(new
            //    {
            //        ComponentName = ((dynamic) this.Components[1]).Name,
            //        ComponentState = ((dynamic)this.Components[1]).State,
            //        MinDuration = 0,
            //        MaxDuration = 0
            //    });

            //this.Instructions.Add(new
            //{
            //    ComponentName = ((dynamic) this.Components[0]).Name,
            //    ComponentState = ((dynamic)this.Components[0]).State,
            //    MinDuration = 1000,
            //    MaxDuration = 0
            //});

            Task.Factory.StartNew(() =>
            {
                while (PackageHost.IsRunning)
                {
                    PackageHost.PushStateObject("BombInfo", new
                    {
                        MacAddress = this.MacAddress,
                        Rssi = this.Rssi,
                        State = this.State,
                        TimeInMs = this.TimeInMs,
                        Components = this.Components,
                        ConfigType = this.ConfigType,
                        Instructions = this.Instructions,
                        Puzzle = this.Puzzle
                    });

                    Thread.Sleep(1000);

                    if (this.State == "Activated")
                    {
                        this.TimeInMs -= 1000;

                        if (this.TimeInMs <= 0)
                        {
                            this.State = "Exploded";
                            this.TimeInMs = 0;
                        }
                    }
                }
            });
        }

        private string macPart(int i)
        {
            if (i >= 10)
            {
                return "0" + ((char) ('a' + i - 10));
            }

            return "0" + i;
        }

        void PackageHost_StateObjectUpdated(object sender, StateObjectUpdatedEventArgs e)
        {
            string destinationMacAddress = e.StateObject.DynamicValue.MacAddress;

            if ((destinationMacAddress != ALL_MAC_ADDRESSES) &&
                (destinationMacAddress != this.MacAddress))
            {
                return;
            }

            PackageHost.WriteInfo("StateObject received: {0}", e.StateObject.Name);

            switch (e.StateObject.Name)
            {
                case "ActivateBomb":
                    {
                        this.State = "Activated";
                        break;
                    }
                case "PauseBomb":
                    {
                        this.State = "Paused";
                        break;
                    }
                case "ResumeBomb":
                    {
                        this.State = "Activated";
                        break;
                    }
                case "ResetBomb":
                    {
                        this.TimeInMs = this.TotalTimeInMs;
                        this.State = "Idle";
                        break;
                    }
                case "DeactivateBomb":
                    {
                        this.State = "Deactivated";
                        break;
                    }
                case "BombConfiguration":
                    {
                        this.TotalTimeInMs = e.StateObject.DynamicValue.Configuration.TimeInMs;
                        this.TimeInMs = this.TotalTimeInMs;
                        this.ConfigType = e.StateObject.DynamicValue.Configuration.Type;
                        this.Instructions.Clear();
                        this.Instructions.AddRange(e.StateObject.DynamicValue.Configuration.AllInstructions);
                        this.Puzzle = e.StateObject.DynamicValue.Configuration.Puzzle;
                        break;
                    }
                default:
                    break;
            }
        }
    }
}
