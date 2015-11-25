﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using InformedProteomics.TopDown.Execution;
using InformedProteomics.Backend.Utils;

namespace ProMex
{
    
    class Program
    {
        public const string Name = "ProMex";
        public static string Version
        {
            get
            {
                var programVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return string.Format("version {0}.{1}.{2} (" + Misc.GetBuildDateTextFromVersion() + ")", programVersion.Major, programVersion.Minor, programVersion.Build);
            }
        }

        private static Dictionary<string, string> _paramDic;

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        private const uint EnableExtendedFlags = 0x0080;

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                var handle = Process.GetCurrentProcess().MainWindowHandle;
                SetConsoleMode(handle, EnableExtendedFlags);

                //args = new string[] {"-i", @"D:\MassSpecFiles\training\raw\QC_Shew_Intact_26Sep14_Bane_C2Column3.pbf", "-minMass", "3000", "-maxMass", "30000"};

                if (args.Length == 0)
                {
                    PrintUsageInfo();
                    return -1;
                }

                if (args.Length % 2 != 0)
                {
                    PrintUsageInfo("The number of arguments must be even.");
                    return -1;
                }

                // initialize parameters
                _paramDic = new Dictionary<string, string>
                {
                    {"-i", null},
                    {"-o", null},
                    {"-minCharge", "1"},
                    {"-maxCharge", "60"},
                    {"-minMass", "2000.0"},
                    {"-maxMass", "50000.0"},
                    {"-score", "n"},
                    {"-csv", "n"},
                    {"-featureMap", "y"},
                    {"-scoreTh", "-10"},
                    {"-maxThreads", "0"},
                };

                for (var i = 0; i < args.Length / 2; i++)
                {
                    var key = args[2 * i];
                    var value = args[2 * i + 1];
                    if (!_paramDic.ContainsKey(key))
                    {
                        PrintUsageInfo("Invalid parameter: " + key);
                        return -1;
                    }
                    _paramDic[key] = value;
                }

                // Parse command line parameters
                var inputFilePath = _paramDic["-i"];

                if (inputFilePath == null)
                {
                    PrintUsageInfo("Missing required parameter -i!");
                    return -1;
                }

                if (!File.Exists(inputFilePath) && !Directory.Exists(inputFilePath))
                {
                    PrintUsageInfo("File not found: " + inputFilePath);
                    return -1;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while parsing the command line parameters: " + ex.Message);
                return -5;
            }

#if (!DEBUG)
            try
            {
#endif
                var param = new LcMsFeatureFinderInputParameter(_paramDic);
                Console.WriteLine("************ {0} {1} ************", Name, Version);
                param.Display();
                var launcher = new LcMsFeatureFinderLauncher(param);
                launcher.Run();
#if (!DEBUG)
            }
            catch (Exception ex)
            {
                // NOTE: The DMS Analysis Manager looks for this text; do not change it
                Console.WriteLine("Exception while processing: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                var errorCode = -Math.Abs(ex.Message.GetHashCode());
                if (errorCode == 0)
                    return -1;
                else
                    return errorCode;
            }
#endif
            return 0;
        }

        private static void PrintUsageInfo(string errorMessage = null)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine("Error: " + errorMessage);
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine();
            }

            Console.WriteLine(
                Name + " " + Version + "\n" +
                "Usage: " + Name + ".exe\n" +
                "\t[-i InputFolder or InputFile]\n" +
                "\t[-o OutFolder (default : InputFolder)]\n" +
                "\t[-minCharge MinCharge] (minimum charge state, default: 1)\n" +
                "\t[-maxCharge MaxCharge] (maximum charge state, default: 60)\n" +
                "\t[-minMass MinMassInDa] (minimum mass in Da, default: 600.0)\n" +
                "\t[-maxMass MaxMassInDa] (maximum mass in Da, default: 50000.0)\n" +
                "\t[-featureMap y or n (default: y)]\n" +
                "\t[-score y or n (default: n)]\n" +
                "\t[-maxThreads 0 (default: 0 (automatic set))]\n"
                );

            // Wait for 1.5 seconds
            System.Threading.Thread.Sleep(1500);
        }

    }

}