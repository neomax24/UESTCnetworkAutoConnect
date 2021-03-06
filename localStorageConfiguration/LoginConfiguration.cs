﻿using System;
using System.IO;
namespace localStorageConfiguration
{
    public class LoginConfiguration
    {
        public bool autoSetup { get; set; }
        public string loginSiteString { get; set; }
        public string usernameString { get; set; }
        public string passwordString { get; set; }
        public LoginConfiguration()
        {
            autoSetup = false;
            loginSiteString = null;
            usernameString = null;
            passwordString = null;
        }
    }
    public class LoginConfigurationHandler
    {
        private const string dataFileName= "loginConfiguration.xml";
        private string applicationLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private string dataFilePath;
        public LoginConfigurationHandler()
        {
            dataFilePath = System.IO.Path.Combine(applicationLocation, dataFileName);
        }
        public LoginConfiguration Load()
        {
            LoginConfiguration data;
            try
            {
                var s = new System.Xml.Serialization.XmlSerializer(typeof(LoginConfiguration));
                using(System.IO.StreamReader dataFile=System.IO.File.OpenText(dataFilePath))
                {
                    data = (LoginConfiguration)s.Deserialize(dataFile);
                    dataFile.Close();
                }
            }
            catch
            {
                data = new LoginConfiguration();
                Save(data);
            }
            return data;
        }
        public void Save(LoginConfiguration loginConfig)
        {
            var s = new System.Xml.Serialization.XmlSerializer(typeof(LoginConfiguration));
            var dataFile = System.IO.File.CreateText(dataFilePath);
            s.Serialize(dataFile, loginConfig);
            dataFile.Close();
        }
    }
}
