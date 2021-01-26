using libCheckAndReconnectUESTC;
using localStorageConfiguration;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace libMantainNetwork
{
    public class MantainNet
    {
        private TimeSpan[] networkCheckTime_round = new TimeSpan[3] {
                                                                TimeSpan.FromSeconds(1),
                                                                TimeSpan.FromSeconds(30),
                                                                TimeSpan.FromMinutes(10)
                                                                };
        private int checkTimePerRound = 10;
        private int networkCheckRound = 0;
        private TimeSpan connectedReportPeriod = TimeSpan.FromSeconds(10);
        private TimeSpan accumateConnectedTime;
        private DateTime startTime;
        private string applicationLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public string logFileName = "mantainNet.log";
        public string logFilePath;

        public MantainNet()
        {
            logFilePath = System.IO.Path.Combine(applicationLocation, logFileName);
            resetLoggingFile();
        }
        public async Task MonitorAndReconnect(LoginConfiguration _loginConfig,CancellationToken stoppingToken)
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
        private void sentLogToLogFile(string _logMessage)
        {
            FileInfo fi = new FileInfo(logFilePath);
            if (fi.Length>1000000)
            {
                startLogging(startTime);
            }
            using (FileStream fs = File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                
                var buffer = _logMessage;
                fs.Write(Encoding.ASCII.GetBytes(buffer), 0, buffer.Length);
            }
        }
        private string genLogLineWithTimeStamp(string _logLine)
        {
            return string.Concat(
                DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss"),
                " : ",
                _logLine, System.Environment.NewLine);
        }
        private string genLogLineWithTimeStamp(DateTime _time,string _logLine)
        {
            return string.Concat(
                _time.ToString("yyyy/MM/dd-HH:mm:ss"),
                " : ",
                _logLine, System.Environment.NewLine);
        }

        private  void startLogging(DateTime _time)
        {
            using (FileStream fs = File.Open(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var buffer = (genLogLineWithTimeStamp(_time,"start"));
                fs.Write(Encoding.ASCII.GetBytes(buffer), 0, buffer.Length);
            }
        }
        private void resetLoggingFile()
        {
            File.Create(logFilePath).Dispose();
        }
    }
}

