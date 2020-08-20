using Moonbyte.UniversalServerAPI;
using Moonbyte.UniversalServerAPI.Interface;
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

        private string Version = "1.3.2";

        #region Directories

        string UserDirectories = null;
        string GlobalSettingDirectories = null;
        string AdminFileDirectory = null;

        private string GetUserDirectory(string Username) { return Path.Combine(UserDirectories, Username); }
        private string GetUserFile(string Username) { return Path.Combine(GetUserDirectory(Username), "UserInfo.usr"); }
        private string GetUserSettingsDirectory(string Username) { return Path.Combine(GetUserDirectory(Username), "Data"); }

        #endregion Directories

        #endregion Vars

        #region Initialize

        public bool Initialize(string PluginDataDirectory)
        {

            Console.WriteLine(ConsoleColor.Yellow + "Running Universaldatabase by Moonbyte Corporation, Version " + Version);

            UserDirectories = Path.Combine(PluginDataDirectory, "Users"); // PluginDataDirectory + @"\Users\";
            GlobalSettingDirectories = Path.Combine(PluginDataDirectory, "GlobalSettings"); //PluginDataDirectory + @"\GlobalSettings\";
            AdminFileDirectory = Path.Combine(PluginDataDirectory, "Admins.ini");

            if (!Directory.Exists(UserDirectories)) Directory.CreateDirectory(UserDirectories);
            if (!Directory.Exists(GlobalSettingDirectories)) Directory.CreateDirectory(GlobalSettingDirectories);
            if (!File.Exists(AdminFileDirectory)) File.Create(AdminFileDirectory).Close();

            return true;
        }

        #endregion Initialize

        #region Invoke

        public bool Invoke(ClientWorkObject clientObject, string[] commandArgs)
        {
            try
            {
                if (commandArgs[1].ToUpper() == "ADDUSER")
                { return AddUser(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "DELETEUSER")
                { return DeleteUser(clientObject, commandArgs[2], commandArgs[3]); }
                else if (commandArgs[1].ToUpper() == "LOGINUSER")
                { return LoginUser(clientObject, commandArgs[2], commandArgs[3]); }
                else if (commandArgs[1].ToUpper() == "CHANGEUSER")
                { return ChangeUsername(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "CHANGEPASSWORD")
                { return ChangePassword(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "GETVALUE")
                { return GetValue(clientObject, commandArgs[2]); }
                else if (commandArgs[1].ToUpper() == "CHECKVALUE")
                { return CheckValue(clientObject, commandArgs[2]); }
                else if (commandArgs[1].ToUpper() == "EDITVALUE")
                { return EditValue(clientObject, commandArgs[2], commandArgs[3], commandArgs[4], commandArgs[5]); }
                else if (commandArgs[1].ToUpper() == "CHECKUSERSETTING")
                { return CheckUserSetting(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "GETUSERSETTING")
                { return GetUserSetting(clientObject, commandArgs[2], commandArgs[3], commandArgs[4]); }
                else if (commandArgs[1].ToUpper() == "EDITUSERSETTING")
                { return EditUserSetting(clientObject, commandArgs[2], commandArgs[3], commandArgs[4], commandArgs[5]); }
            }
            catch (Exception e)
            {
                clientObject.clientSender.Send(clientObject, "USRDBS_ERROR");
                clientObject.AddToLog("WARN", "Exception occurred in UserDatabase plugin.");
                clientObject.LogExceptions(e);
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

        private bool AddUser(ClientWorkObject workObject, string Username, string Password, string Email)
        {
            if (CheckUserInformation(Username, Password) != "USERDOESNOTEXIST")
            { workObject.clientSender.Send(workObject, "USRDBS_USRADD_USEREXIST"); return true; }

            Password = sha512Encryption.Encrypt(Password);

            Directory.CreateDirectory(GetUserDirectory(Username));
            Directory.CreateDirectory(GetUserSettingsDirectory(Username));
            File.Create(GetUserFile(Username)).Close();

            string userContent = Username + ":" + Password + ":" + Email + ":" + false.ToString();

            File.WriteAllText(GetUserFile(Username), Encrypt(userContent));

            workObject.clientSender.Send(workObject, "USRDBS_USRADD_TRUE");
            return true;
        }

        #endregion AddUser

        #region DeleteUser

        private bool DeleteUser(ClientWorkObject workObject, string Username, string Password)
        {
            string returnedCheckedInformation = CheckUserInformation(Username, Password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_DELETEUSER_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_DELETEUSER_AUTHERROR"); return true; }

            Directory.Delete(GetUserDirectory(Username), true);

            workObject.clientSender.Send(workObject, "USRDBS_DELETEUSER_TRUE");
            return true;
        }

        #endregion DeleteUser

        #region LoginUser

        private bool LoginUser(ClientWorkObject workObject, string Username, string Password)
        {
            string returnedCheckedInformation = CheckUserInformation(Username, Password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_LOGINUSER_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_LOGINUSER_FALSE"); return true; }
            else if (returnedCheckedInformation == true.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_LOGINUSER_TRUE"); return true; }

            return false;
        }

        #endregion LoginUser

        #region ChangeUser

        private bool ChangeUsername(ClientWorkObject workObject, string Username, string Password, string NewUsername)
        {
            string returnedCheckedInformation = CheckUserInformation(Username, Password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_CHANGEUSER_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_CHANGEUSER_AUTHERROR"); return true; }
            else if (returnedCheckedInformation == true.ToString()) 
            {
                string[] oldData = Decrypt(File.ReadAllText(GetUserFile(Username))).Split(':');
                string Email = oldData[2]; bool Verified = bool.Parse(oldData[3]); Password = sha512Encryption.Encrypt(Password);
                string userContent = NewUsername + ":" + Password + ":" + Email + ":" + Verified.ToString();

                Directory.CreateDirectory(GetUserDirectory(NewUsername));
                Directory.CreateDirectory(GetUserSettingsDirectory(NewUsername));
                File.Create(GetUserFile(NewUsername)).Close();
                File.WriteAllText(GetUserFile(NewUsername), Encrypt(userContent));

                foreach(FileInfo fi in new DirectoryInfo(GetUserSettingsDirectory(Username)).GetFiles())
                { fi.CopyTo(Path.Combine(GetUserSettingsDirectory(NewUsername), fi.Name)); }

                Directory.Delete(GetUserDirectory(Username), true);

                workObject.clientSender.Send(workObject, "USRDBS_CHANGEUSER_TRUE"); return true; 
            }

            return false;
        }

        #endregion ChangeUser

        #region ChangePassword

        private bool ChangePassword(ClientWorkObject workObject, string Username, string Password, string NewPassword)
        {
            string returnedCheckedInformation = CheckUserInformation(Username, Password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_CHANGEPASSWORD_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_CHANGEPASSWORD_AUTHERROR"); return true; }
            else if (returnedCheckedInformation == true.ToString())
            {
                string[] oldData = Decrypt(File.ReadAllText(GetUserFile(Username))).Split(':');
                string Email = oldData[2]; bool Verified = bool.Parse(oldData[3]); NewPassword = sha512Encryption.Encrypt(NewPassword);
                string userContent = Username + ":" + NewPassword + ":" + Email + ":" + Verified.ToString();

                File.WriteAllText(GetUserFile(Username), Encrypt(userContent));
                workObject.clientSender.Send(workObject, "USRDBS_CHANGEPASSWORD_TRUE"); return true;
            }

            return false;
        }

        #endregion ChangePassword

        #region GetValue

        private bool GetValue(ClientWorkObject workObject, string ValueTitle)
        {
            string CheckValue = checkglobalvalue(ValueTitle);
            if (CheckValue == false.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_GETVALUE_VALUEEXIST"); return true; }
            else if (CheckValue == true.ToString())
            {
                string ValueFileName = Path.Combine(GlobalSettingDirectories, ValueTitle + ".dat");
                workObject.clientSender.Send(workObject, Decrypt(File.ReadAllText(ValueFileName))); return true;
            }

            return false;
        }

        #endregion GetValue

        #region CheckValue

        private bool CheckValue(ClientWorkObject workObject, string ValueTitle)
        {
            string CheckValue = checkglobalvalue(ValueTitle);
            if (CheckValue == false.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKVALUE_FALSE"); return true; }
            else if (CheckValue == true.ToString())
            { workObject.clientSender.Send(workObject, "USRDBS_CHECKVALUE_TRUE"); return true; }

            return false;
        }

        #endregion CheckValue

        #region EditValue

        // Won't be added till server admin verification is added
        private bool EditValue(ClientWorkObject workObject, string Username, string Password, string ValueTitle, string ValueString)
        {
            string returnedCheckedInformation = CheckUserInformation(Username, Password);

            if (returnedCheckedInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_USEREXIST"); return true; }
            else if (returnedCheckedInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_AUTHERROR"); return true; }
            else if (returnedCheckedInformation == true.ToString())
            {
                if (CheckOP(Username))
                {
                    string ValueFileName = Path.Combine(GlobalSettingDirectories, ValueTitle + ".dat");
                    File.WriteAllText(ValueFileName, Encrypt(ValueString));

                    workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_TRUE");
                }
                else { workObject.clientSender.Send(workObject, "USRDBS_EDITVALUE_OPERROR"); return true; }
            }

            return false;
        }

        #endregion EditValue

        #region CheckUserSetting

        private bool CheckUserSetting(ClientWorkObject workObject, string Username, string Password, string UserSettingTitle)
        {
            string checkUserSetting = checkusersetting(Username, Password, UserSettingTitle);
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

        private bool GetUserSetting(ClientWorkObject workObject, string Username, string Password, string UserSettingTitle)
        {
            string checkUserSetting = checkusersetting(Username, Password, UserSettingTitle);
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
                string UserSettingFileDirectory = Path.Combine(GetUserSettingsDirectory(Username), UserSettingTitle + ".dat");
                workObject.clientSender.Send(workObject, Decrypt(File.ReadAllText(UserSettingFileDirectory))); return true; 
            }

            return false;
        }

        #endregion GetUserSetting

        #region EditUserSetting

        private bool EditUserSetting(ClientWorkObject workObject, string Username, string Password, string UserSettingTitle, string UserSettingValue)
        {
            string userInformation = CheckUserInformation(Username, Password);
            if (userInformation == "USERDOESNOTEXIST") { workObject.clientSender.Send(workObject, "USRDBS_EDITUSERSETTING_USEREXIST"); return true; }
            else if (userInformation == false.ToString()) { workObject.clientSender.Send(workObject, "USRDBS_EDITUSERSETTING_AUTHERROR"); return true; }
            else if (userInformation == true.ToString())
            {
                string UserSettingFileDirectory = Path.Combine(GetUserSettingsDirectory(Username), UserSettingTitle + ".dat");

                if (!File.Exists(UserSettingFileDirectory)) { File.Create(UserSettingFileDirectory).Close(); }
                File.WriteAllText(UserSettingFileDirectory, Encrypt(UserSettingValue));

                workObject.clientSender.Send(workObject, "USRDBS_EDITUSERSETTING_TRUE"); return true;
            }

            return false;
        }

        #endregion EditUserSetting

        #endregion Commands

        #region ConsoleCommands

        private void OP(string Username, Logger iLogger)
        {
            List<string> ops = File.ReadAllLines(AdminFileDirectory).ToList();
            ops.Add(Username); File.WriteAllLines(AdminFileDirectory, ops);
            ops = null; iLogger.AddToLog("INFO", "Added [" + Username + "] to the ops file!");
        }

        #endregion ConsoleCommands

        #region Private Method's

        #region Check OP

        private bool CheckOP(string Username)
        {
            List<string> ops = File.ReadAllLines(AdminFileDirectory).ToList();

            foreach (string s in ops)
            { if (s == Username) { return true; } }
            return false;
        }

        #endregion Check OP

        #region CheckValue

        private string checkglobalvalue(string ValueTitle)
        {
            string ValueFileName = Path.Combine(GlobalSettingDirectories, ValueTitle + ".dat");
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
                string UserSettingFileDirectory = Path.Combine(GetUserSettingsDirectory(Username), UserSettingTitle + ".dat");

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
            string UserFile = GetUserFile(Username);

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
