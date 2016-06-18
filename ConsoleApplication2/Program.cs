using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleApplication2
{
    internal class Program
    {
        static List<string> failList = new List<string>();

        private static void Main()
        {
            bool endFlag = false;
            var readLines = new ConcurrentQueue<string>();
            var resultLines = new ConcurrentQueue<string>();

            

            // ====================== READ ============================================================

            Thread readthread = new Thread(() =>
            {
                List<string> fileNames = GetFileNames();

                for (int k = 0; k < fileNames.Count; k++)
                {
                    string[] lines = GetFile(fileNames[k]).Split('\n');

                    foreach (var line in lines)
                    {
                        readLines.Enqueue(line);
                    }

                    Console.WriteLine('\n' + fileNames[k] + ' ' + k + " / " + fileNames.Count
                                      + "\n readLines count: " + readLines.Count
                                      + "\n resultLines count: " + resultLines.Count);
                }

                endFlag = true;
            });

            readthread.Start();

            // ======================== COUNT =========================================================

            Thread countThread = new Thread(() =>
            {
                Regex[] regexes = new[] {"Madgere3G", "Rex", "Rex_Madgere", "Madgere"}
                    .Select(nick => new Regex(@".*<.*" + nick + @".*>.*rt\.com.*$", RegexOptions.Compiled))
                    .ToArray();

                while (true)
                {
                    string line;
                    if (!readLines.TryDequeue(out line))
                    {
                        if (endFlag)
                        {
                            break;
                        }
                        Thread.Sleep(20);
                        continue;
                    }

                    if (!regexes.Any(regex => regex.IsMatch(line)))
                    {
//                        Console.Write(".");
                        continue;
                    }

                    resultLines.Enqueue(line);
                    Console.WriteLine(line);


                }
            });

            countThread.Start();

            // ======================= WRITE ==========================================================

            Thread writeThread = new Thread(() =>
            {
                string line;

                using (var sw = new StreamWriter("results.txt"))
                {
                    while (true)
                    {
                        if (!resultLines.TryDequeue(out line))
                        {
                            if (endFlag)
                            {
                                break;
                            }
                            Thread.Sleep(20);
                            continue;
                        }

                        sw.WriteLine(line);
                    }

                    foreach (var path in failList)
                    {
                        sw.WriteLine(path);
                        Console.WriteLine(path);
                    }
                }
            });

            writeThread.Start();



        }

        private static List<string> GetFileNames()
        {
            string content = GetFile(@"http://osaka.plus/fansubbers/");

            Regex reg = new Regex(@"\<a href='\.(\/%23fansubbers\.\d+\.log)'\>\#fansubbers", RegexOptions.Compiled);

            MatchCollection matches = reg.Matches(content);

            List<string> fileNames = new List<string>();

            for (int i = 0; i < matches.Count; i++)
            {
                fileNames.Add(@"http://osaka.plus/fansubbers" + matches[i].Groups[1]);
                Console.Write("\b\b\b\b");
                Console.WriteLine(i);
            }

            return fileNames;
        }

        private static string GetFile(string path)
        {
            Stream stream = null;
            try
            {
                stream = WebRequest.Create(new Uri(path))
                                   .GetResponse()
                                   .GetResponseStream();
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\n" + e.Message + "\n");
                failList.Add(path);
            }

            if (stream == null)
                return null;

            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }

        }
    }
}
