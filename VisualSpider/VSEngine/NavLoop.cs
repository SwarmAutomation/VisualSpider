﻿using System;
using System.Collections.Generic;
using System.Threading;
using Thought.Terminals;
using VSEngine.Data;
using VSEngine.Integration;

namespace VSEngine
{
    /// <summary>
    /// Coordinates threads and processes results
    /// </summary>
    public class NavLoop
    {
        ConsoleColor LabelColor = ConsoleColor.Magenta;
        ConsoleColor FieldColor = ConsoleColor.White;
        ConsoleColor TitleColor = ConsoleColor.Green;
        ConsoleColor LinkColor = ConsoleColor.DarkGray;
        ConsoleColor DeviderColor = ConsoleColor.Cyan;

        bool WorkToDo = true;
        // check if there is work to do
        // queue up URls / Scripts to run
        // new up treads based on max thread count and work left
        // collect results from finished treads
        // store navigation results in db
        DBAccess DB;
        Config CFG;
        Terminal Console;

        public void Loop(DBAccess db, Config cfg, Terminal console, EngineState state)
        {
            DB = db;
            CFG = cfg;
            Console = console;
            Console.ClearScreen();
            WriteScreen();

            int linkcount = 0;

            while (WorkToDo)
            {
                List<NavUnit> queuedUnits = db.RetriveUnitSet(cfg.MaxThreads);
                List<Thread> threads = new List<Thread>();
                List<NavThread> navThreads = new List<NavThread>();

                if(queuedUnits.Count < 1)
                {
                    WorkToDo = false;
                    break;
                }

                foreach(NavUnit currentUnit in queuedUnits)
                {
                    NavThread tempNavThread = new NavThread(currentUnit);
                    tempNavThread.configRef = cfg;
                    if (state == EngineState.LinkCheck) tempNavThread.CollectLinks = false;
                    Thread tempThread = new Thread(tempNavThread.Navigate);
                    tempThread.Start();

                    threads.Add(tempThread);
                    navThreads.Add(tempNavThread);
                }
                

                bool threadIsAlive = true;

                while(threadIsAlive)
                {
                    Thread.Sleep(1000);

                    threadIsAlive = false;

                    foreach(Thread currentThread in threads)
                    {
                        if (currentThread.IsAlive) threadIsAlive = true;
                    }

                    if (state == EngineState.LinkCheck)
                    {
                        UpdateScreen(queuedUnits, threads, linkcount, "Link Check");
                    }
                    else
                    {
                        UpdateScreen(queuedUnits, threads, linkcount, "Crawl");
                    }
                }

                foreach(NavThread currentNavTh in navThreads)
                {
                    db.StoreResolvedNavUnit(currentNavTh.UnitToPassBack, cfg);
                    linkcount++;
                }

                if (cfg.MaxLinkCount > 0)
                {
                    if (db.ResolvedNavUnitCount() > cfg.MaxLinkCount) WorkToDo = false;
                }

                //WriteScreen();

            }
        }

        private void WriteScreen()
        {
            Console.ClearScreen();
            Console.SetForeground(TitleColor);
            Console.WritePadded("Visual Spider", System.Console.WindowWidth, JustifyText.Center);
            Console.SetForeground(DeviderColor);
            for(int i=0; i < System.Console.WindowWidth; i++)
            {
                Console.Write('=');
            }
            Console.Write("\n");
        }

        private void UpdateScreen(List<NavUnit> units, List<Thread> threds, int linkCount, string mode)
        {
            WriteScreen();
            Console.SetForeground(LabelColor);
            Console.Write("\tMode: ");
            Console.SetForeground(FieldColor);
            Console.Write(mode + "\n");

            Console.SetForeground(LabelColor);
            Console.Write("\tThread Count: ");
            Console.SetForeground(FieldColor);
            Console.Write(threds.Count);
            Console.SetForeground(LabelColor);
            Console.Write("\t\tLink Count: ");
            Console.SetForeground(FieldColor);
            Console.Write(linkCount + "\n\n");

            Console.SetForeground(LinkColor);
            foreach (NavUnit currentUnit in units)
            {
                Console.WriteLine("\t" + currentUnit.Address.ToString());
            }
        }
    }
}
