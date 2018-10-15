using IndieGoat.UniversalServer.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace vortexstudio.universalserver.userdatabase
{
    public class UserDatabase : IServerPlugin
    {
        public string Name { get { return "userdatabase"; } }

        #region Vars

        //All default directories
        string UserDirectory = null;
        string SettingDirectory = null;
        string ValueDirectory = null;

        //Server settings
        TcpListener ServerSocket;
        TcpClient ClientSocket;

        //Private Vars
        string _ServerDirectory = null;

        event EventHandler<SendMessageEventArgs> IServerPlugin.SendMessage
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region OnLoad

        //Activates when the server loads the plugin
        public void onLoad(string ServerDirectory)
        {
            //Set all of the directories
            UserDirectory = ServerDirectory + @"Users\";
            SettingDirectory = ServerDirectory + @"Setting\";
            ValueDirectory = ServerDirectory + @"Values\";

            //Sets the server directory
            _ServerDirectory = ServerDirectory;

            //Create the directories if it does not exist
            if (!Directory.Exists(UserDirectory)) Directory.CreateDirectory(UserDirectory);
            if (!Directory.Exists(SettingDirectory)) Directory.CreateDirectory(SettingDirectory);
            if (!Directory.Exists(ValueDirectory)) Directory.CreateDirectory(ValueDirectory);
        }

        #endregion

        #region OnInvoke

        ClientSocketWorkload Workload; ClientContext Context;

        //Activates when the plugin has been invoked
        public void Invoke(ClientSocketWorkload workload, ClientContext context, int port, List<string> Args, string ServerDirectory)
        {
            Workload = workload; Context = context;
            //Processes the command.
            try
            {
                if (Args[1].ToUpper() == "ADDUSER")
                {
                    if (AddUser(Args[2], Args[3])) Console.WriteLine("[UserDatabase] Created a new user! Username : " + Args[2] + ", Password : " + Args[3]);
                }
                else if (Args[1].ToUpper() == "DELETEUSER")
                {
                    if (DeleteUser(Args[2], Args[3])) Console.WriteLine("[UserDatabase] Delete a user! Username : " + Args[2] + ", Password : " + Args[3]);
                }
                else if (Args[1].ToUpper() == "LOGINUSER")
                {
                    if (UserLogin(Args[2], Args[3])) Console.WriteLine("[UserDatabase] User has logged in! Username : " + Args[2] + ".");
                }
                else if (Args[1].ToUpper() == "CHANGEUSER")
                {
                    if (ChangeUsername(Args[2], Args[3], Args[4])) Console.WriteLine("[UserDatabase] User has changed there username! Username : " + Args[2] + ", New Username : " + Args[4]);
                }
                else if (Args[1].ToUpper() == "GETVALUE")
                {
                    if (GetServerValue(Args[2])) Console.WriteLine("[UserDatabase] User has collected a server value! Server value name : " + Args[2] + ".");
                }
                else if (Args[1].ToUpper() == "CHECKVALUE")
                {
                    if (CheckServerValue(Args[2])) Console.WriteLine("[UserDatabase] User has check if a setting exist! Server value name : " + Args[2] + ".");
                }
                else if (Args[1].ToUpper() == "EDITVALUE")
                {
                    if (EditServerValue(Args[2], Args[3])) Console.WriteLine("[UserDatabase] User has edit a value! Server value name : " + Args[2] + ", server value contents : " + Args[3]);
                }
                else if (Args[1].ToUpper() == "CHECKUSERSETTING")
                {
                    if (CheckUserSetting(Args[2], Args[3], Args[4])) Console.WriteLine("[UserDatabase] " + Args[2] + " has checked if " + Args[4] + " existed!");
                }
                else if (Args[1].ToUpper() == "GETUSERSETTING")
                {
                    if (GetUserSetting(Args[2], Args[3], Args[4])) Console.WriteLine("[UserDatabase] " + Args[2] + " has gathered " + Args[4] + ".");
                }
                else if (Args[1].ToUpper() == "EDITUSERSETTING")
                {
                    if (EditUserSetting(Args[2], Args[3], Args[4], Args[5])) Console.WriteLine("[UserDatabase " + Args[2] + " has changed " + Args[4] + ", new value : " + Args[5]);
                }
                else
                {
                    SendMessage("USRDBS_UNKNOWN");
                }
            }
            catch
            {
                SendMessage("USRDBS_SPLIT");
                Console.WriteLine("[UserDatabase] Failed to split the user request.");
            }
        }

        #endregion

        #region Processing

        /// <summary>
        /// Edits the setting for the user
        /// </summary>
        /// <param name="Username">Username of the user</param>
        /// <param name="Password">Password of the user</param>
        /// <param name="SettingTitle">Title of the setting</param>
        /// <param name="SettingContents">Value of the setting</param>
        /// <returns></returns>
        private bool EditUserSetting(string Username, string Password, string SettingTitle, string SettingContents)
        {
            try
            {
                //Encrypts the password of the user
                Password = sha512Encryption.Encrypt(Password);

                //A new list to get all values for a setting
                List<string> allSettingValue = new List<string>();

                //The user file directory
                string usrFile = UserDirectory + Username + ".usr";

                //Check if the user exist
                if (!File.Exists(usrFile))
                {
                    //Send a UserDoesNotExist message
                    SendMessage("EDTSET_UDOESNOTEXIST");
                    return false;
                }

                //Reads all of the text of the client
                string m = File.ReadAllText(usrFile);

                //Check if the given password is correct or not
                if (m != Username + ":" + Password)
                {
                    //Sends a AUTHERROR message
                    SendMessage("EDTSET_AUTHE");
                    return false;
                }
                
                //Set the settings directory
                string usrSettingFileDirectory = SettingDirectory + Username + ".dat";

                //Get all of the setting values in the textbox
                allSettingValue = File.ReadAllLines(usrSettingFileDirectory).ToList();

                //Loops all of the values in the list
                for (int i = 0; i < allSettingValue.Count(); i++)
                {
                    //Splits the setting
                    string[] tmpSplitString = allSettingValue[i].Split(':');

                    //Checks if the setting title is equal to the one we need
                    if (tmpSplitString[0] == SettingTitle)
                    {
                        //Changes the temp split string to the new server content
                        tmpSplitString[1] = SettingContents;

                        //Joins the edited string
                        allSettingValue[i] = string.Join(":", tmpSplitString);

                        //Writes to disk of the changes.
                        File.WriteAllLines(usrSettingFileDirectory, allSettingValue);

                        //If true, sends a sucess message.
                        SendMessage("EDTSET_TRUE");

                        return true;
                    }
                }

                //Setting does not exist, adding one
                allSettingValue.Add(SettingTitle + ":" + SettingContents);

                //Writes changes to the disk
                File.WriteAllLines(usrSettingFileDirectory, allSettingValue);

                //If true, sends a sucess message
                SendMessage("EDTSET_TRUE");
                return true;
            }
            catch (Exception e)
            {
                //Sends a error message
                SendMessage("EDTSET_FALSE");

                //Logs the information in the console
                Console.WriteLine("[UserDatabase] ERROR : Method EditUserSetting has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Returns the client the requested setting
        /// </summary>
        /// <param name="Username">Username of the client</param>
        /// <param name="Password">Password of the client</param>
        /// <param name="SettingTitle">Requested setting title</param>
        /// <returns></returns>
        private bool GetUserSetting(string Username, string Password, string SettingTitle)
        {
            try
            {
                //Encrypts the password using SHA512
                Password = sha512Encryption.Encrypt(Password);
                
                //Gets the user file
                string usrFile = UserDirectory + Username + ".usr";

                //Check if the user exist
                if (!File.Exists(usrFile))
                {
                    //Send a user does not exist error
                    SendMessage("GETSET_UDOESNOTEXIST");
                    return false;
                }

                //Check if the password is correct
                string fileInfo = File.ReadAllText(usrFile);
                if (fileInfo != Username + ":" + Password)
                {
                    //Send a AUTHERROR message
                    SendMessage("GETSET_AUTHE");
                    return false;
                }

                //Check if the settings directory exist for the user
                if (File.Exists(_ServerDirectory + @"Settings\" + Username + ".dat"))
                {
                    //Get the contents of the setting file
                    string[] settingFileContents = File.ReadAllLines(_ServerDirectory + @"Settings\" + Username + ".dat");
                    for (int i = 0; i < settingFileContents.Count(); i++)
                    {
                        //Splits the setting into the title and the value
                        string[] tmpSplit = settingFileContents[i].Split(':');

                        //If the title is equal to the same title, returns the contents of the file
                        if (tmpSplit[0] == SettingTitle)
                        {
                            //Sends a return message
                            SendMessage(tmpSplit[1]);
                            return true;
                        }
                    }

                    //Sends the message that the setting does not exist.
                    SendMessage("GETSET_DOESNOTEXIST");
                    return false;
                }
                else
                {
                    //Sends the message that the get setting does not exist
                    SendMessage("GETSET_FDOESNOTEXIST");
                    return false;
                }
            }
            catch (Exception e)
            {
                //Sends a false error message
                SendMessage("GETSET_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method GetUserSetting has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Checks if a user setting exist's
        /// </summary>
        /// <param name="Username">Username of the client</param>
        /// <param name="Password">Password of the client</param>
        /// <param name="SettingTitle">Desired setting title</param>
        /// <returns></returns>
        private bool CheckUserSetting(string Username, string Password, string SettingTitle)
        {
            try
            {
                //Encrypts the password using SHA512
                Password = sha512Encryption.Encrypt(Password);

                //Get the directory of the user file
                string usrFile = UserDirectory + Username + ".usr";

                //Checks if the user file exist or not
                if (!File.Exists(usrFile))
                {
                    //Sends a UserDoesNotExist message
                    SendMessage("CEKSET_UDOESNOTEXIST");
                    return false;
                }

                //Check if the username and password is correct
                string m = File.ReadAllText(usrFile);
                if (m != Username + ":" + Password)
                {
                    //Sends a auth error
                    SendMessage("CEKSET_AUTHE");
                    return false;
                }

                //Get the user setting directory and read that file into an array
                string usrSettingFile = _ServerDirectory + @"Settings\" + Username + ".dat";
                List<string> settingListArray = new List<string>();
                settingListArray = File.ReadAllLines(usrSettingFile).ToList();

                //Loop through the array
                for (int i = 0; i < settingListArray.Count(); i++)
                {
                    //Split the array into the title and value
                    string[] tmpSplit = settingListArray[i].Split(':');

                    //Check if the title exist's
                    if (tmpSplit[0] == SettingTitle)
                    {
                        //Send a true message
                        SendMessage("CEKSET_TRUE");
                        return true;
                    }
                }

                //Send a false message
                SendMessage("CETSET_TRUEF");
                return false;
            }
            catch (Exception e)
            {
                //Sends a false error message
                SendMessage("CHKSET_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method CheckUserSetting has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Edit a global server value
        /// </summary>
        /// <param name="ValueTitle">Title of the global value</param>
        /// <param name="ValueContents">Content of the global value</param>
        /// <returns></returns>
        private bool EditServerValue(string ValueTitle, string ValueContents)
        {
            try
            {
                //Check all vars
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("SVREDT_VALUER"); return false; }

                //Get the value directory
                string valueDirectory = _ServerDirectory + @"Values\";

                //Check if the value exist
                if (!File.Exists(valueDirectory + ValueTitle + ".val"))
                {
                    //Creates the value
                    File.Create(valueDirectory + ValueTitle + ".val").Close();
                }

                //Writes all content to the file
                File.WriteAllText(valueDirectory + ValueTitle + ".val", ValueContents);

                //Sends a true message
                SendMessage("SVREDT_TRUE");

                return true;
            }
            catch (Exception e)
            {
                //Sends a false error message
                SendMessage("SVREDT_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method EditServerValue has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Check if the server value exist or not
        /// </summary>
        /// <param name="ValueTitle">Title of the global value</param>
        /// <returns></returns>
        private bool CheckServerValue(string ValueTitle)
        {
            try
            {
                //Check all local vars
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("SVRCEK_VALUER"); return false; }

                //Initialize value directory
                string valueDirectory = _ServerDirectory + @"Values\";

                //Check if the setting file exist
                if (File.Exists(valueDirectory + ValueTitle + ".val"))
                {
                    //Send a true message
                    SendMessage("SVRCEK_TRUE");
                    return true;
                }
                else
                {
                    //Send a false message
                    SendMessage("SVRCEK_TRUEF");
                    return true;
                }
            }
            catch (Exception e)
            {
                //Sends a false error message
                SendMessage("SVRCEK_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method GetServerValue has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Sends a server value to the client
        /// </summary>
        /// <param name="ValueTitle">Title of the global value</param>
        /// <returns></returns>
        private bool GetServerValue(string ValueTitle)
        {
            try
            {
                //Check all vars
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("SVRGET_VALUER"); return false; }

                //Initialize value directory
                string valueDirectory = _ServerDirectory + @"Values\";

                //Get the value file
                string valueFile = valueDirectory + ValueTitle + ".val";

                //Check if the value file exist's
                if (!File.Exists(valueFile))
                {
                    //Sends a setting does not exist message
                    SendMessage("SVRGET_DOESNOTEXIST");
                    return false;
                }

                //Read the file content of the setting
                string fileContents = File.ReadAllText(valueFile);

                //Sends the content to the client
                SendMessage(fileContents);
                return true;

            }
            catch (Exception e)
            {
                //Send a false error message
                SendMessage("SVRGET_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method GetServerValue has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Changes the stored password in the user file
        /// </summary>
        /// <param name="Username">username of the client</param>
        /// <param name="Password">password of the client</param>
        /// <param name="NewPassword">new password to add to the client</param>
        /// <returns></returns>
        private bool ChangePassword(string Username, string Password, string NewPassword)
        {
            try
            {

                //Encrypt both the password and the new password
                Password = sha512Encryption.Encrypt(Password);
                NewPassword = sha512Encryption.Encrypt(NewPassword);

                //Check the vars
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("CNGPSD_VALUER"); return false; }

                //Get the user file directory
                string usrFile = UserDirectory + Username + ".usr";

                //Check if the user file exist
                if (!File.Exists(usrFile))
                {
                    //Send a user does not exist message
                    SendMessage("CNGPSD_DOESNOTEXIST");
                    return false;
                }

                //Edit the password in the file
                File.WriteAllText(usrFile, Username + ":" + NewPassword);

                //Send a true message
                SendMessage("CNGPSD_TRUE");
                return true;
            }
            catch (Exception e)
            {
                //Send a false error message
                SendMessage("CNGPSD_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method ChangePassword has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Changes the username of the user
        /// </summary>
        /// <param name="Username">Username of the client</param>
        /// <param name="Password">Password of the client</param>
        /// <param name="NewUsername">New username of the client</param>
        /// <returns></returns>
        private bool ChangeUsername(string Username, string Password, string NewUsername)
        {
            try
            {

                //Encrypts the user file
                Password = sha512Encryption.Encrypt(Password);

                //Check if the varables are ok
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("CNGUSR_VALUER"); return false; }
                
                //Initialize the file directory for the user file
                string usrFile = UserDirectory + Username + ".usr";

                //Check if the use file exist
                if (!File.Exists(usrFile))
                {
                    //Send a user does not exist error
                    SendMessage("CNGUSR_DOESNOTEXIST");
                    return false;
                }

                //Read the content of the file
                string fileRead = File.ReadAllText(usrFile);

                //Check if the username and password is correct
                if (fileRead != Username + ":" + Password)
                {
                    //Send a wrong password event
                    SendMessage("CNGUSR_WNGCRED");
                    return false;
                }

                //Write the new data into the file
                File.WriteAllText(usrFile, NewUsername + ":" + Password);

                //Changes the user file name
                File.Move(usrFile, UserDirectory + NewUsername + ".usr");

                //Changes the settings file name to the new user
                File.Move(_ServerDirectory + @"Settings\" + Username + ".dat", _ServerDirectory + @"Settings\" + NewUsername + ".dat");
                SendMessage("CNGUSR_TRUE");
                return true;
            }
            catch (Exception e)
            {
                //Sends a false error message
                SendMessage("CNGUSR_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method ChangeUser has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Check if the given username and password is correct
        /// </summary>
        /// <param name="Username">Username of the client</param>
        /// <param name="Password">Password of the client</param>
        /// <returns></returns>
        private bool UserLogin(string Username, string Password)
        {
            try
            {

                //Gets the encrypted password
                Password = sha512Encryption.Encrypt(Password);

                //Check if all vars are correct
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("USRLOG_VALUER"); return false; }

                //Gets the user directory
                string usrDirectory = _ServerDirectory + @"Users\";

                //Check if the user file exist's
                if (!File.Exists(usrDirectory + Username + ".usr"))
                {
                    //Returns user does not exist
                    SendMessage("USRLOG_DOESNOTEXIST");
                    return false;
                }
                else
                {
                    //Read the user file
                    string fileRead = File.ReadAllText(usrDirectory + Username + ".usr");

                    //Check if the readed data is equal to the given username and password
                    if (fileRead == Username + ":" + Password)
                    {
                        //Returns the true message
                        SendMessage("USRLOG_TRUE");
                        return true;
                    }
                    else
                    {
                        //Returns the false message
                        SendMessage("USRLOG_WRONG");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                //Returns the false message
                SendMessage("USRLOG_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method UserLogin has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Delete the user, and the user file according to the user
        /// </summary>
        /// <param name="Username">Username of the client</param>
        /// <param name="Password">Password of the client</param>
        /// <returns></returns>
        private bool DeleteUser(string Username, string Password)
        {
            try
            {
                
                //Encrypts the password using SHA512
                Password = sha512Encryption.Encrypt(Password);

                //Check all of the vars
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("DELUSR_VALUER"); return false; }

                //Get the user file directory
                string userFile = UserDirectory + Username + ".usr";

                //Check if the user file exist
                if (!File.Exists(userFile))
                {
                    //Sends a user does not exist message
                    SendMessage("DELUSR_DOESNOTEXIST");
                    return false;
                }

                //Read the lines in the user file
                string userFileContent = File.ReadAllText(userFile);

                //Check if the given username and password was correct
                if (userFileContent == Username + ":" + Password)
                {
                    //Delete both the user file, and the settings file
                    File.Delete(userFile);
                    File.Delete(_ServerDirectory + @"Settings\" + Username + ".dat");

                    //Sends a true message
                    SendMessage("DELUSR_TRUE");
                    return true;
                }
                else
                {
                    //Sends a autherror message
                    SendMessage("DELUSR_PSDWNG");
                    return false;
                }
            }
            catch (Exception e)
            {
                SendMessage("DELUSR_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method DeleteUser has encounter a error : " + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Adds a user, and a user settings file with a username and password
        /// </summary>
        /// <param name="Username">Username of the client</param>
        /// <param name="Password">Password of the client</param>
        /// <returns></returns>
        private bool AddUser(string Username, string Password)
        {
            try
            {
                
                //Encrypt the password using a SHA512 method
                Password = sha512Encryption.Encrypt(Password);

                //Check if the vars are correct
                if (!CheckVarables()) { Console.WriteLine("[UserDatabase] ERROR : Required varables are equal to null."); SendMessage("USRADD_VALUER"); return false; }

                //Get the directory for the user
                string userFile = UserDirectory + Username + ".usr";

                //Check if the file exist
                if (File.Exists(userFile))
                {
                    //Send a user does not exist message
                    SendMessage("USRADD_EXIST"); return false;
                }

                //Create both the user file and the setting file
                File.Create(userFile).Close();
                File.Create(_ServerDirectory + @"Settings\" + Username + ".dat");
                File.WriteAllText(userFile, Username + ":" + Password);
                
                //Sends a true message
                SendMessage("USRADD_TRUE"); return true;
            }
            catch (Exception e)
            {
                //Sends a false error message
                SendMessage("USRADD_FALSE");
                Console.WriteLine("[UserDatabase] ERROR : Method AddUser has encounter a error : " + e.ToString());
                return false;
            }
        }

        #endregion

        #region Custom Method's

        /// <summary>
        /// Check if all of the local vars are null or not
        /// </summary>
        /// <returns></returns>
        private bool CheckVarables()
        {
            //Check all local vars if it is null
            if (ServerSocket == null || ClientSocket == null || _ServerDirectory == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends a message to the client
        /// </summary>
        /// <param name="client">TCP client to send the value to</param>
        /// <param name="value">String of text</param>
        private void SendMessage(string value)
        {
            //Sends a string to a client using UTF8
            Workload.SendMessage(Context, value);
        }

        #endregion

        public void Unload()
        {

        }
    }

    #region Encrypting

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

    #endregion
}
