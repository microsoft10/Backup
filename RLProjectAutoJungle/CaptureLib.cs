using System.Runtime.InteropServices;
using System.Text;
using System;

namespace RLProjectJunglePlay
{
    class CaptureLib
    {
        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string section,
            string key, string def, StringBuilder retVal, int size, string filePath);
 
        [DllImport("kernel32")]
        public static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
 
        public static String GetSettingValue_String(String Section, String Key, String iniPath)
        {
            StringBuilder buffer = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "Error", buffer, 255, iniPath);

            return buffer.ToString();
        }
        public static int GetSettingValue_Int(String Section, String Key, String iniPath)
        {
            String temp = GetSettingValue_String(Section, Key, iniPath);
            try
            {
                return Convert.ToInt32(temp);
            }
            catch
            {
                //Game.PrintChat("Error {0} : Cant load Values", temp.ToString());
                Console.WriteLine("Error : Cant load Values({0},{1})",Key ,temp.ToString());
                return 0;
            }
        }
        public static Double GetSettingValue_Double(String Section, String Key, String iniPath)
        {
            String temp = GetSettingValue_String(Section, Key, iniPath);
            try
            {
                return Convert.ToDouble(temp);
            }
            catch
            {
                //Game.PrintChat("Error {0} : Cant load Values", temp.ToString());
                Console.WriteLine("Error : Cant load Values({0},{1})", Key, temp.ToString());
                return 0f;
            }
        }
        public static Single GetSettingValue_Float(String Section, String Key, String iniPath)
        {
            String temp = GetSettingValue_String(Section, Key, iniPath);
            try
            {
                return Convert.ToSingle(temp);
            }
            catch
            {
                //Game.PrintChat("Error {0} : Cant load Values", temp.ToString());
                Console.WriteLine("Error : Cant load Values({0},{1})", Key, temp.ToString());
                return 0f;
            }
        }
        public static Boolean GetSettingValue_Bool(String Section, String Key, String iniPath)
        {
            String temp = GetSettingValue_String(Section, Key, iniPath);
            try
            {
                return Convert.ToBoolean(temp);
            }
            catch
            {
                if (Convert.ToInt32(temp) == 0)
                    return false;
                else
                    return true;
            }
        }

        public static void SetSettingValue(String Section, String Key, String Value, String iniPath)
        {
            WritePrivateProfileString(Section, Key, Value, iniPath);
        }
    }
}
