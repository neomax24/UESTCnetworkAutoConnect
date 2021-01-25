using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using localStorageConfiguration;
using libCheckAndReconnectUESTC;
using System.IO;

namespace libMantainNetwork
{
    public class MantainNet
    {
        private static TimeSpan[] networkCheckTime_round = new TimeSpan[3] {
                                                                TimeSpan.FromSeconds(1),
                                                                TimeSpan.FromSeconds(30),
                                                                TimeSpan.FromMinutes(10)
                                                                };
        private static int checkTimePerRound = 10;
        private static int networkCheckRound = 0;
        private static TimeSpan connectedReportPeriod = TimeSpan.FromSeconds(10);
        private static TimeSpan accumateConnectedTime;
        private static DateTime startTime;
        public const string logFileName= "mantainNet.log";
        public MantainNet()
        {
        }
        public static async Task MonitorAndReconnect(LoginConfiguration _loginConfig,CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
            {
                sentLogToLogFile(genLogLineWithTimeStamp("quit"));
                Debug.WriteLine(genLogLineWithTimeStamp("quit"));
            });
            startTime = DateTime.Now;

            startLogging(startTime);
            int iNetworkCheck = 0;
            accumateConnectedTime = connectedReportPeriod + TimeSpan.FromSeconds(1);
            UestcNetworkHandler uestcNetworkHandler = new UestcNetworkHandler();
            Debug.WriteLine(genLogLineWithTimeStamp("backgorund  thread : " + Thread.CurrentThread.ManagedThreadId));


            bool isNetworkConnect = await uestcNetworkHandler.CheckNetwork();
            if(isNetworkConnect)
            { sentLogToLogFile(genLogLineWithTimeStamp("network connected")); }
            else
            { sentLogToLogFile(genLogLineWithTimeStamp("network disconnected")); }

            while (!stoppingToken.IsCancellationRequested)
            {
                iNetworkCheck++;
                if (iNetworkCheck > checkTimePerRound)
                {
                    networkCheckRound = Math.Max(
                        networkCheckTime_round.Length,
                        networkCheckRound++);
                }
                if (isNetworkConnect)
                {
                    iNetworkCheck = 0;
                    networkCheckRound = 0;
                    accumateConnectedTime += networkCheckTime_round[networkCheckRound];
                    
                    //if (accumateConnectedTime >= connectedReportPeriod)
                    //{
                    //    sentLogToLogFile(genLogLineWithTimeStamp("network connected"));
                    //    accumateConnectedTime = TimeSpan.Zero;
                    //}
                    //Debug.WriteLine(genLogLineWithTimeStamp("network connected"));
                }
                else
                {
                    sentLogToLogFile(genLogLineWithTimeStamp("network disconnected, reconnecting"));
                    Debug.WriteLine(genLogLineWithTimeStamp("network disconnected, reconnecting"));
                    uestcNetworkHandler.reconnectNetworkUestc(
                        _loginConfig.loginSiteString,
                        _loginConfig.usernameString,
                        _loginConfig.passwordString, stargPageOnlyNoConnect: false);

                    isNetworkConnect = await uestcNetworkHandler.CheckNetwork();
                    if (isNetworkConnect)
                    { sentLogToLogFile(genLogLineWithTimeStamp("network connected")); }
                    else
                    { sentLogToLogFile(genLogLineWithTimeStamp("network disconnected")); }
                }
                await Task.Delay(networkCheckTime_round[networkCheckRound], stoppingToken);
                isNetworkConnect = await uestcNetworkHandler.CheckNetwork();
            }
        }
        private static void sentLogToLogFile(string _logMessage)
        {
            FileInfo fi = new FileInfo(logFileName);
            if (fi.Length>1000000)
            {
                startLogging(startTime);
            }
            using (FileStream fs = File.Open(logFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                
                var buffer = _logMessage;
                fs.Write(Encoding.ASCII.GetBytes(buffer), 0, buffer.Length);
            }
        }
        private static string genLogLineWithTimeStamp(string _logLine)
        {
            return string.Concat(
                DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss"),
                " : ",
                _logLine, System.Environment.NewLine);
        }
        private static string genLogLineWithTimeStamp(DateTime _time,string _logLine)
        {
            return string.Concat(
                _time.ToString("yyyy/MM/dd-HH:mm:ss"),
                " : ",
                _logLine, System.Environment.NewLine);
        }

        private static void startLogging(DateTime _time)
        {
            using (FileStream fs = File.Open(logFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var buffer = (genLogLineWithTimeStamp(_time,"start"));
                fs.Write(Encoding.ASCII.GetBytes(buffer), 0, buffer.Length);
            }
        }
    }
}

