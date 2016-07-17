using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.IO;                //FileStream,StreamWriter, StreamReader
using System.Windows.Forms;     //MessageBox


namespace spectrometer_device
{
    class IniFile
    {
		public string path;
		public IniFile(string strFilePath)
		{
			path=strFilePath;
		}
        /************************************************************************/
        /* Write Ini
         * strSection   :Section
         * strKey       :Key
         * strValue     :Write Value
         * strFilePath  :Ini File Path (wince using Absolute whole path)
         */
        /************************************************************************/
        
        public void WriteString(string strSection, string strKey, string strValue)
        {
            INICommon(false, strSection, strKey, strValue, path);
        }


        /************************************************************************/
        /* Read Ini
         * strSection   :Section
         * strKey       :Key
         * strDefault   :default value if not found associate value
         * strFilePath  :Ini File Path (wince using Absolute whole path)
         * Returnï¼?    :Value for associate key
        /************************************************************************/
        public string ReadString(string strSection, string strKey, string strDefault)
        {
            return INICommon(true, strSection, strKey, strDefault, path);
        }

        private static string[] Split(string input, string pattern)
        {
            string[] arr = System.Text.RegularExpressions.Regex.Split(input, pattern);
            return arr;
        }
        private static void AppendToFile(string strPath, string strContent)
        {
            FileStream fs = new FileStream(strPath, FileMode.Append);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine(strContent);
            sw.Flush();
            sw.Close();
            fs.Close();
        }
        private static void WriteArray(string strPath, string[] strContent)
        {
            FileStream fs = new FileStream(strPath, FileMode.Truncate);
            StreamWriter sw= new StreamWriter(fs, System.Text.Encoding.Default);
            sw.BaseStream.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < strContent.Length; i++)
            {
                if (strContent[i].Trim() == "\r\n")
                    continue;
                sw.WriteLine(strContent[i].Trim());
            }
            sw.Flush();
            sw.Close();
            fs.Close();
        }                

        private static string INICommon(bool isRead, string ApplicationName, string KeyName, string Default, string FileName)
        {
            string strSection = "[" + ApplicationName + "]";
            string strBuf;
            try
            {
                //(1) if file do'nt exist, it will create new ini file
                if (!File.Exists(FileName))
                {
                    FileStream sr = File.Create(FileName);
                    sr.Close();
                }
                //Read ini file
                System.IO.StreamReader stream = new System.IO.StreamReader(FileName, System.Text.Encoding.Default);
                strBuf = stream.ReadToEnd();
                stream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "INI File Read Fail.");
                return Default;
            }
            string[] rows = Split(strBuf, "\r\n");
            string oneRow;
            int i = 0;
            for (; i < rows.Length; i++)
            {
                oneRow = rows[i].Trim();                
                if (0 == oneRow.Length) { continue;}    //Blank row                
                if (';' == oneRow[0]) {  continue;}     //comment row                
                if (strSection != oneRow) { continue;}  //Not found                
                break; //found
            }

            //(2) not found associate section, will create new section 
            if (i >= rows.Length)
            {
                AppendToFile(FileName, "\r\n" + strSection + "\r\n" + KeyName + "=" + Default);
                return Default;
            }
            //found section
            i += 1; //Skip section  
            int bakIdxSection = i;//backup section next line
            string[] strLeft;
            
            //found attutiate
            for (; i < rows.Length; i++)
            {
                oneRow = rows[i].Trim();                
                
                if (0 == oneRow.Length)  { continue;} //Blank row                   
                if (';' == oneRow[0])    { continue;} //comment row                 
                if ('[' == oneRow[0]) {  break; }  //over range

                strLeft = Split(oneRow, "=");
                if (strLeft == null || strLeft.Length != 2) { continue;}
                
                
                if (strLeft[0].Trim() == KeyName) //found 
                {
                    //read
                    if (isRead)
                    {
                        //(3) found attutiate but not value
                        if (0 == strLeft[1].Trim().Length)
                        {
                            rows[i] = strLeft[0].Trim() + "=" + Default;
                            WriteArray(FileName, rows);
                            return Default;
                        }
                        else
                        {
                            //found it                        
                            return strLeft[1].Trim();
                        }
                    }
                    //Write
                    else
                    {
                        rows[i] = strLeft[0].Trim() + "=" + Default;
                        WriteArray(FileName, rows);
                        return Default;
                    }
                }
            }
            //(4)not found attutiate, it will create it and set it as default value
            rows[bakIdxSection] = rows[bakIdxSection] + "\r\n" + KeyName + "=" + Default;
            WriteArray(FileName, rows);
            return Default;
        }
    } 
}
