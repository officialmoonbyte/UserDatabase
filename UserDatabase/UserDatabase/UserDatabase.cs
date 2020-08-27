using Moonbyte.UniversalServerAPI;
using Moonbyte.UniversalServerAPI.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Moonbyte.Plugins.userDatabase
{
    public class UserDatabase : IUniversalPlugin
    {

        #region Vars

        public string Name { get { return "UserDatabase"; } }
        //public const string Version = 

        #region Directories

        UniversalPlugin universalAPI;

        string userDirectories = null;
        string globalSettingDirectories = null;
        string adminFileDirectory = null;

        private string getUserDirectory(string username) => Path.Combine(userDirectories, username);
        private string getUserFile(string username) => Path.Combine(getUserDirectory(username), "UserInfo.usr");
        private string getUserSettingsDirectory(string username) => Path.Combine(getUserDirectory(username), "Data");

        #endregion Directories

        #endregion Vars

        #region Initialize

        public bool Initialize(string pluginDataDirectory, UniversalPlugin baseClass)
        {
            universalAPI = baseClass;

            Console.WriteLine(ConsoleColor.Yellow + "Running Universaldatabase by Moonbyte Corporation, Version 1.2.2.3"); // <== add version here in the future
            
            userDirectories = Path.Combine(pluginDataDirectory, "Users"); // PluginDataDirectory + @"\Users\";
            globalSettingDirectories = Path.Combine(pluginDataDirectory, "GlobalSettings"); //PluginDataDirectory + @"\GlobalSettings\";
            adminFileDirectory = Path.Combine(pluginDataDirectory, "Admins.ini");

            if (!Directory.Exists(userDirectories)) Directory.CreateDirectory(userDirectories);
            if (!Directory.Exists(globalSettingDirectories)) Directory.CreateDirectory(globalSettingDirectories);
            if (!File.Exists(adminFileDirectory)) File.Create(adminFileDirectory).Close();

            return true;
        }

        #endregion Initialize

        #region Invoke

        public bool Invoke(ClientWorkObject clientObject, string[] commandArgs)
        {
            try
            {
                if (commandArgs[1].ToUpper() == "ADDUSER")
                { return addUser(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "DELETEUSER")
                { return deleteUser(clientObject, commandArgs[2], commandArgs[3]); }
                else if (commandArgs[1].ToUpper() == "LOGINUSER")
                { return loginUser(clientObject, commandArgs[2], commandArgs[3]); }
                else if (commandArgs[1].ToUpper() == "CHANGEUSER")
                { return changeUsername(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "CHANGEPASSWORD")
                { return changePassword(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "GETVALUE")
                { return getValue(clientObject, commandArgs[2]); }
                else if (commandArgs[1].ToUpper() == "CHECKVALUE")
                { return checkValue(clientObject, commandArgs[2]); }
                else if (commandArgs[1].ToUpper() == "EDITVALUE")
                { return editValue(clientObject, commandArgs[2], commandArgs[3], commandArgs[4], commandArgs[5]); }
                else if (commandArgs[1].ToUpper() == "CHECKUSERSETTING")
                { return checkUserSetting(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "GETUSERSETTING")
                { return getUserSetting(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "EDITUSERSETTING")
                { return editUserSetting(clientObject, commandArgs[2], commandArgs[3], commandArgs[4], commandArgs[5]); }
            }
            catch (Exception e)
            {
                clientObject.clientSender.Send(clientObject, "USRDBS_ERROR");
                ServerAPI.Log.AddToLog("WARN", "Exception occurred in UserDatabase plugin.");
                ServerAPI.Log.LogExceptions(e);
                return true;
            }

            return false;
        }

        #endregion Invoke

        #region ConsoleInvoke

        public void ConsoleInvoke(string[] commandArgs, Logger iLogger)
        {
            if (commandArgs[1].ToUpper() == "OP")
            { OP(commandArgs[2], iLogger); }
        }

        #endregion ConsoleInvoke

        #region Commands

        #region AddUser

        private bool addUser(ClientWorkObject workObject, string username, string password, string Email)
        {
            if (CheckUserInformation(username, password) != "USERDOESNOTEXIST")
            { workObject.clientSender.Send(workObject, "USRDBS_USRADD_USEREXIST"); return true; }

            password = sha512Encryption.Encrypt(password);

            Directory.CreateDirectory(getUserDirectory(username));
            Directory.CreateDirectory(getUserSettingsDirectory(username));
            File.Create(getUserFile(username)).Close();

            string userContent = username + ":" + password + ":" + Email + ":" + false.ToString();

            File.WriteAllText(getUserFile(username), Encrypt(userContent));

            workObject.clientSender.Send(workObject, "USRDBS_USRADD_TRUE");
            return true;
        }

        #endregion AddUser

        #region DeleteUser

        private bool deleteUser(ClientWorkObject workObject, string username, string password)
        {
            string returnedCheckedInformation = CheckUserInformation(username, password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_DELETEUSER_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_DELETEUSER_AUTHERROR"); return true; }

            Directory.Delete(getUserDirectory(username), true);

            workObject.clientSender.Send(workObject, "USRDBS_DELETEUSER_TRUE");
            return true;
        }

        #endregion DeleteUser

        #region LoginUser

        private bool loginUser(ClientWorkObject workObject, string username, string password)
        {
            string returnedCheckedInformation = CheckUserInformation(username, password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_LOGINUSER_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_LOGINUSER_FALSE"); return true; }
            else if (returnedCheckedInformation == true.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_LOGINUSER_TRUE"); return true; }

            return false;
        }

        #endregion LoginUser

        #region ChangeUser

        private bool changeUsername(ClientWorkObject workObject, string username, string password, string newUsername)
        {
            string returnedCheckedInformation = CheckUserInformation(username, password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_CHANGEUSER_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_CHANGEUSER_AUTHERROR"); return true; }
            else if (returnedCheckedInformation == true.ToString()) 
            {
                string[] oldData = Decrypt(File.ReadAllText(getUserFile(username))).Split(':');
                string Email = oldData[2]; bool Verified = bool.Parse(oldData[3]); password = sha512Encryption.Encrypt(password);
                string userContent =newUsername + ":" + password + ":" + Email + ":" + Verified.ToString();

                Directory.CreateDirectory(getUserDirectory(newUsername));
                Directory.CreateDirectory(getUserSettingsDirectory(newUsername));
                File.Create(getUserFile(newUsername)).Close();
                File.WriteAllText(getUserFile(newUsername), Encrypt(userContent));

                foreach(FileInfo fi in new DirectoryInfo(getUserSettingsDirectory(username)).GetFiles())
                { fi.CopyTo(Path.Combine(getUserSettingsDirectory(newUsername), fi.Name)); }

                Directory.Delete(getUserDirectory(username), true);

                workObject.clientSender.Send(workObject, "USRDBS_CHANGEUSER_TRUE"); return true; 
            }

            return false;
        }

        #endregion ChangeUser

        #region ChangePassword

        private bool changePassword(ClientWorkObject workObject, string username, string password, string newPassword)
        {
            string returnedCheckedInformation = CheckUserInformation(username, password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_CHANGEPASSWORD_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_CHANGEPASSWORD_AUTHERROR"); return true; }
            else if (returnedCheckedInformation == true.ToString())
            {
                string[] oldData = Decrypt(File.ReadAllText(getUserFile(username))).Split(':');
                string Email = oldData[2]; bool Verified = bool.Parse(oldData[3]); newPassword = sha512Encryption.Encrypt(newPassword);
                string userContent = username + ":" + newPassword + ":" + Email + ":" + Verified.ToString();

                File.WriteAllText(getUserFile(username), Encrypt(userContent));
                workObject.clientSender.Send(workObject, "USRDBS_CHANGEPASSWORD_TRUE"); return true;
            }

            return false;
        }

        #endregion ChangePassword

        #region GetValue

        private bool getValue(ClientWorkObject workObject, string valueTitle)
        {
            string CheckValue = checkglobalvalue(valueTitle);
            if (CheckValue == false.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_GETVALUE_VALUEEXIST"); return true; }
            else if (CheckValue == true.ToString())
            {
                string ValueFileName = Path.Combine(globalSettingDirectories, valueTitle + ".dat");
                workObject.clientSender.Send(workObject, Decrypt(File.ReadAllText(ValueFileName))); return true;
            }

            return false;
        }

        #endregion GetValue

        #region CheckValue

        private bool checkValue(ClientWorkObject workObject, string valueTitle)
        {
            string CheckValue = checkglobalvalue(valueTitle);
            if (CheckValue == false.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKVALUE_FALSE"); return true; }
            else if (CheckValue == true.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKVALUE_TRUE"); return true; }

            return false;
        }

        #endregion CheckValue

        #region EditValue

        // Won't be added till server admin verification is added
        private bool editValue(ClientWorkObject workObject, string username, string password, string valueTitle, string valueString)
        {
            string returnedCheckedInformation = CheckUserInformation(username, password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_AUTHERROR"); return true; }
            else if (returnedCheckedInformation == true.ToString())
            {
                if (CheckOP(username))
                {
                    string ValueFileName = Path.Combine(globalSettingDirectories, valueTitle + ".dat");
                    File.WriteAllText(ValueFileName, Encrypt(valueString));

                    workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_TRUE");
                }
                else { workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_OPERROR"); return true; }
            }

            return false;
        }

        #endregion EditValue

        #region CheckUserSetting

        private bool checkUserSetting(ClientWorkObject workObject, string username, string password, string userSettingTitle)
        {
            string checkUserSetting = checkusersetting(username, password, userSettingTitle);
            if (checkUserSetting == "USERDOESNOTEXIST") 
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKUSERSETTING_USEREXIST"); return true; }
            else if (checkUserSetting == "AuthError") 
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKUSERSETTING_AUTHERROR"); return true; }
            else if (checkUserSetting == false.ToString()) 
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKUSERSETTING_FALSE"); return true; }
            else if (checkUserSetting == "unknownerror") 
            { return false; }
            else if (checkUserSetting == true.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKUSERSETTING_TRUE"); return true; }

            return false;
        }

        #endregion CheckUserSetting

        #region GetUserSetting

        private bool getUserSetting(ClientWorkObject workObject, string username, string password, string userSettingTitle)
        {
            string checkUserSetting = checkusersetting(username, password, userSettingTitle);
            if (checkUserSetting == "USERDOESNOTEXIST")
            { workObject.clientSender.Send(workObject, "USRDBS_GETUSERSETTING_USEREXIST"); return true; }
            else if (checkUserSetting == "AuthError")
            { workObject.clientSender.Send(workObject, "USRDBS_GETUSERSETTING_AUTHERROR"); return true; }
            else if (checkUserSetting == false.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_GETUSERSETTING_SETTINGEXIST"); return true; }
            else if (checkUserSetting == "unknownerror")
            { return false; }
            else if (checkUserSetting == true.ToString())
            {
                string UserSettingFileDirectory = Path.Combine(getUserSettingsDirectory(username), userSettingTitle + ".dat");
                workObject.clientSender.Send(workObject, Decrypt(File.ReadAllText(UserSettingFileDirectory))); return true; 
            }

            return false;
        }

        #endregion GetUserSetting

        #region EditUserSetting

        private bool editUserSetting(ClientWorkObject workObject, string username, string password, string userSettingTitle, string userSettingValue)
        {
            string userInformation = CheckUserInformation(username, password);
            if (userInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_EDITUSERSETTING_USEREXIST"); return true; }
            else if (userInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_EDITUSERSETTING_AUTHERROR"); return true; }
            else if (userInformation == true.ToString())
            {
                string UserSettingFileDirectory = Path.Combine(getUserSettingsDirectory(username), userSettingTitle + ".dat");

                if (!File.Exists(UserSettingFileDirectory)) { File.Create(UserSettingFileDirectory).Close(); }
                File.WriteAllText(UserSettingFileDirectory, Encrypt(userSettingValue));

                workObject.clientSender.Send(workObject, "USRDBS_EDITUSERSETTING_TRUE"); return true;
            }

            return false;
        }

        #endregion EditUserSetting

        #endregion Commands

        #region ConsoleCommands

        private void OP(string Username, Logger iLogger)
        {
            List<string> ops = File.ReadAllLines(adminFileDirectory).ToList();
            ops.Add(Username); File.WriteAllLines(adminFileDirectory, ops);
            ops = null; iLogger.AddToLog("INFO", "Added [" + Username + "] to the ops file!");
        }

        #endregion ConsoleCommands

        #region Private Method's

        #region Check OP

        private bool CheckOP(string Username)
        {
            List<string> ops = File.ReadAllLines(adminFileDirectory).ToList();

            foreach (string s in ops)
            { if (s == Username) { return true; } }
            return false;
        }

        #endregion Check OP

        #region CheckValue

        private string checkglobalvalue(string ValueTitle)
        {
            string ValueFileName = Path.Combine(globalSettingDirectories, ValueTitle + ".dat");
            if (File.Exists(ValueFileName))
            { return true.ToString(); } else { return false.ToString(); }
        }

        #endregion CheckValue

        #region CheckUserSetting

        private string checkusersetting(string Username, string Password, string UserSettingTitle)
        {
            string userInformation = CheckUserInformation(Username, Password);
            if(userInformation == "USERDOESNOTEXIST") { return userInformation; }
            else if(userInformation == false.ToString()) { return "AuthError"; }
            else if (userInformation == true.ToString())
            {
                string UserSettingFileDirectory = Path.Combine(getUserSettingsDirectory(Username), UserSettingTitle + ".dat");

                if (File.Exists(UserSettingFileDirectory))
                { return true.ToString(); }
                else { return false.ToString(); }
            }

            return "unknownerror";
        }

        #endregion CheckUserSetting

        #region CheckUserInformation

        private string CheckUserInformation(string Username, string Password)
        {
            string UserFile = getUserFile(Username);

            if (!File.Exists(UserFile))
            { return "USERDOESNOTEXIST"; }

            Password = sha512Encryption.Encrypt(Password);
            string FileData = Decrypt(File.ReadAllText(UserFile)); string[] FileArgs = FileData.Split(':');
            string compareData = FileArgs[0] + ":" + FileArgs[1];

            if (compareData != Username + ":" + Password)
            { return false.ToString(); }
            else { return true.ToString(); }
        }

        #endregion CheckUserInformation

        #endregion Private Method's

        #region UniversalServerAPI

        UniversalPlugin ServerAPI;
        public UniversalPlugin GetUniversalPluginAPI()
        { return ServerAPI; }

        public void SetUniversalPluginAPI(UniversalPlugin Plugin)
        { ServerAPI = Plugin; }

        #endregion UniversalServerAPI

        #region Encrypting

        #region Get Mac Address

        private string GetClientMacAddress()
        { return NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback).Select(nic => nic.GetPhysicalAddress().ToString()).FirstOrDefault(); }

        #endregion Get Mac Address

        #region Encrypt

        private string Encrypt(string encryptString)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(encryptString);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(GetClientMacAddress(), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    { cs.Write(clearBytes, 0, clearBytes.Length); cs.Close(); }

                    encryptString = Convert.ToBase64String(ms.ToArray());
                }
            }

            return encryptString;
        }

        #endregion Encrypt

        #region Decrypt

        private string Decrypt(string input)
        {
            input = input.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(input);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(GetClientMacAddress(), new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    { cs.Write(cipherBytes, 0, cipherBytes.Length); cs.Close(); }

                    input = Encoding.Unicode.GetString(ms.ToArray());
                }
            }

            return input;
        }

        #endregion Decrypt

        #region SHA512Encryption

        class sha512Encryption
        {

            public static string Encrypt(string value)
            {
                try
                {
                    SHA512 sha512 = SHA512Managed.Create();
                    byte[] bytes = Encoding.UTF8.GetBytes(value);
                    byte[] hash = sha512.ComputeHash(bytes);
                    return GetStringFromHash(hash);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Error] UserDatabase - Failed to encrypt a value : " + e.Message);
                    return null;
                }
            }

            private static string GetStringFromHash(byte[] hash)
            {
                StringBuilder result = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    result.Append(hash[i].ToString("X2"));
                }
                return result.ToString();
            }

        }

        #endregion SHA512Encryption

        #endregion

    }
}
