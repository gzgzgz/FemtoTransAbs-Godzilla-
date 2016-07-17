using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using spectrometer_device;

namespace Godzilla
{
    public class ReadEEPROM
    {
        [DllImport("BWTEKUSB.DLL")]
        static extern int bwtekReadEEPROMUSB(StringBuilder OutFileName, int nChannel);

        // The following are all the supported bwtek spectrometer types
        public enum TypeID
        {
            UnKnown = -1,
            BTC110_MODE = 0,
            BRC100_MODE = 1,
            BTC120_MODE = 2,
            BTC121_MODE = 3,
            BTC200_MODE = 4,
            BTC300_MODE = 5,
            BTC320_MODE = 6,
            BTC400_MODE = 7,
            BTC500_MODE = 8,
            BRC110_MODE = 9,
            BTC111_MODE = 10,
            BRC111A_MODE = 11,
            BTC611_MODE = 12,
            BRC711_MODE = 13,
            BTC112_MODE = 14,
            BTC211_MODE = 15,
            BTC15x_MODE = 16,
            BRC112_MODE = 17,
            BRC311_MODE = 18,
            BTC311_MODE = 19,
            BTC610_MODE = 20,
            BTC321_MODE = 21,
            BTF111_MODE = 22,
            BTC811_MODE = 23,
            BTC221_MODE = 24,
            BTC251_MODE = 25,
            BTC711_MODE = 26,
            BTC611E_512_MODE = 27,
            BTC611E_1024_MODE = 28,
            BTC711E_512_MODE = 29,
            BTC711E_1024_MODE = 30,
            BRC711E_512_MODE = 31,
            BRC711E_1024_MODE = 32,
            BTF113_MODE = 33,
            BWS003_MODE = 34,
            BWS004_MODE = 35,
            BTC221E_G9208_256W_MODE = 36,
            BTU111_MODE = 37,
            BTC251E_512_RS232_MODE = 38,
            BTC251E_512_MODE = 39,
            BTC261E_512_RS232_MODE = 40,
            BTC261E_512_MODE = 41,
            BTC261E_256_MODE = 42,
            BTC261E_1024_MODE = 43,
            BRC131_MODE = 44,
            BTC262E_256_MODE = 45,
            BTC262E_512_MODE = 46,
            BTC262E_1024_MODE = 47,
            BRC100_OEM_MODE = 48,
            BRC641_MODE = 49,
            BTC613E_512_MODE = 50,
            BTC613E_1024_MODE = 51,
            BTC651E_512_MODE = 52,
            BTC651E_1024_MODE = 53,
            BWS225_256_MODE = 54,
            BWS225_512_MODE = 55,
            BTC641_MODE = 56,
            BWS102_MODE = 57,
            BWS003B_MODE = 58,
            BWS435_MODE = 59,
            BRC642E_2048_MODE = 60,
            BTC263E_256_MODE = 61,
            BRC113_MODE = 62,
            BRC112P_MODE = 63,
            BTC261P_512_MODE = 64,
            BRC115_MODE = 65,
            BTC665_MODE = 66,
            BTC675_MODE = 67,
            BTC655_MODE = 68,
            BTC264P_512_MODE = 69,
            BTC264P_1024_MODE = 70,
            BRC1K_MODE = 71,
            BTC665N_MODE = 72,
            BRC115P_MODE = 73,
            BTC655N_MODE = 74
        }
        public struct Spec_Para_Struct
        {
            public int usbtype;
            public int channel;

            public string cCode;
            public string model;
            public string spectrometer_name;
            public int spectrometer_type;
            public int pixel_number;
            public int timing_mode;
            public int input_mode;
            public int xaxis_data_reverse;


            public double inttime;
            public int inttime_int;
            public double inttime_min;
            public int inttime_base;
            public int inttime_unit;

            public int trigger_mode;  //0=internal, 1=external 

            public double starttime;
            public double endtime;
            public double deltatime;

            public double coefficient_a0;
            public double coefficient_a1;
            public double coefficient_a2;
            public double coefficient_a3;
            public double coefficient_b0;
            public double coefficient_b1;
            public double coefficient_b2;
            public double coefficient_b3;

            public double[] wavelength;

            public string Title;
            public string Model;
            public string Operator;

            public ushort[] DataArray;//= new ushort[MachineInfo.PixelNo];
            public int scan_flag;
            public int stop_flag;
            public int scan_mode;
            public int frame_num;
            public int active_frame;
            public int ave_num;
            public int darkcompensate_flag;
            public int smooth_flag;
            public int shutter_inverse;
        }

        private string get_section(int spectrometer_type)
        {
            string tmp_section = "";
            switch (spectrometer_type)
            {
                case 0: tmp_section = "BRC100"; break;
                case 1: tmp_section = "BRC100"; break;
                case 2: tmp_section = "BTC120"; break;
                case 3: tmp_section = "BTC121"; break;
                case 4: tmp_section = "BTC200"; break;
                case 5: tmp_section = "BTC300"; break;
                case 6: tmp_section = "BTC320"; break;
                case 7: tmp_section = "BTC400"; break;
                case 8: tmp_section = "BTC500"; break;
                case 9: tmp_section = "BRC110"; break;
                case 10: tmp_section = "BTC111"; break;
                case 11: tmp_section = "BRC111"; break;
                case 12: tmp_section = "BTC611"; break;
                case 13: tmp_section = "BRC711"; break;
                case 14: tmp_section = "BTC112"; break;
                case 15: tmp_section = "BTC211"; break;
                case 16: tmp_section = "BTC15x"; break;
                case 17: tmp_section = "BRC112"; break;
                case 18: tmp_section = "BRC311"; break;
                case 19: tmp_section = "BTC311"; break;
                case 20: tmp_section = "BTC610"; break;
                case 21: tmp_section = "BTC321"; break;
                case 22: tmp_section = "BTF111"; break;
                case 23: tmp_section = "BTC811"; break;
                case 24: tmp_section = "BTC221"; break;
                case 25: tmp_section = "BTC251"; break;
                case 26: tmp_section = "BTC711"; break;
                case 27: tmp_section = "BTC611E_512"; break;
                case 28: tmp_section = "BTC611E_1024"; break;
                case 29: tmp_section = "BTC711E_512"; break;
                case 30: tmp_section = "BTC711E_1024"; break;
                case 31: tmp_section = "BRC711E_512"; break;
                case 32: tmp_section = "BRC711E_1024"; break;
                case 33: tmp_section = "BTF113"; break;
                case 34: tmp_section = "BWS003"; break;
                case 35: tmp_section = "BWS004"; break;
                case 36: tmp_section = "BTC221E_G9208_256W"; break;
                case 37: tmp_section = "BTU111"; break;
                case 38: tmp_section = "BTC251E_512_RS232"; break;
                case 39: tmp_section = "BTC251E_512"; break;
                case 40: tmp_section = "BTC261E_512_RS232"; break;
                case 41: tmp_section = "BTC261E_512"; break;
                case 42: tmp_section = "BTC261E_256"; break;
                case 43: tmp_section = "BTC261E_1024"; break;
                case 44: tmp_section = "BRC131"; break;
                case 45: tmp_section = "BTC262E_256"; break;
                case 46: tmp_section = "BTC262E_512"; break;
                case 47: tmp_section = "BTC262E_1024"; break;
                case 48: tmp_section = "BRC100_OEM"; break;
                case 49: tmp_section = "BRC641"; break;
                case 50: tmp_section = "BTC613E_512"; break;
                case 51: tmp_section = "BTC613E_1024"; break;
                case 52: tmp_section = "BTC651E_512"; break;
                case 53: tmp_section = "BTC651E_1024"; break;
                case 54: tmp_section = "BWS225_256"; break;
                case 55: tmp_section = "BWS225_512"; break;
                case 56: tmp_section = "BTC641"; break;
                case 57: tmp_section = "BWS102"; break;
                case 58: tmp_section = "BWS003B"; break;
                case 59: tmp_section = "BWS435"; break;
                case 60: tmp_section = "BRC642E_2048"; break;
                case 61: tmp_section = "BTC263E_256"; break;
                case 62: tmp_section = "BRC113"; break;
                case 63: tmp_section = "BRC112P"; break;
                case 64: tmp_section = "BTC261P_512"; break;
                case 65: tmp_section = "BRC115"; break;
                case 66: tmp_section = "BTC665"; break;
                case 67: tmp_section = "BTC675"; break;
                case 68: tmp_section = "BTC655"; break;
                case 69: tmp_section = "BTC264P_512"; break;
                case 70: tmp_section = "BTC264P_1024"; break;
                case 71: tmp_section = "BRC1K"; break;
                case 72: tmp_section = "BTC665N"; break;
                case 73: tmp_section = "BRC115P"; break;
                case 74: tmp_section = "BTC655N"; break;
            }
            return tmp_section;
        }

        private Double StrToDouble(string tmp_str)
        {
            double tmp_v = double.Parse(tmp_str, new System.Globalization.CultureInfo("en-US"));
            return tmp_v;
        }

        public Spec_Para_Struct[] dev_paras;

        private void Load_para(string filename, int Index)
        {
            Spec_Para_Struct[] spec_para = new Spec_Para_Struct[33];
            IniFile dev_ini_file = new IniFile(filename);
            string section_str = "SPECTROMETER";
            int spectrometer_type = Convert.ToInt32(dev_ini_file.ReadString(section_str, "spectrometer_type", "14"));
            section_str = get_section(spectrometer_type);
            int pixelnumber = Convert.ToInt32(dev_ini_file.ReadString(section_str, "pixelnumber", "2048"));
            int timing_mode = Convert.ToInt32(dev_ini_file.ReadString(section_str, "timing_mode", "1"));
            int input_mode = Convert.ToInt32(dev_ini_file.ReadString(section_str, "input_mode", "1"));
            int inttime_unit = Convert.ToInt32(dev_ini_file.ReadString(section_str, "inttime_unit", "1"));//0=us, 1=ms, 2=us
            int inttime_base = Convert.ToInt32(dev_ini_file.ReadString(section_str, "intTimeBase", "0"));
            int inttime_min = Convert.ToInt32(dev_ini_file.ReadString(section_str, "inttime_min", "10"));


            section_str = "COMMON";
            string model = dev_ini_file.ReadString(section_str, "model", "");
            string ccode = dev_ini_file.ReadString(section_str, "c_code", "");
            double inttime = StrToDouble(dev_ini_file.ReadString(section_str, "inttime", "10"));
            double[] coefficeint_a = new double[4];
            coefficeint_a[0] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_a0", "0"));
            coefficeint_a[1] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_a1", "0"));
            coefficeint_a[2] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_a2", "0"));
            coefficeint_a[3] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_a3", "0"));
            double[] coefficeint_b = new double[4];
            coefficeint_b[0] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_b0", "0"));
            coefficeint_b[1] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_b1", "0"));
            coefficeint_b[2] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_b2", "0"));
            coefficeint_b[3] = StrToDouble(dev_ini_file.ReadString(section_str, "coefs_b3", "0"));
            int xaxis_data_reverse = Convert.ToInt32(dev_ini_file.ReadString(section_str, "xaxis_data_reverse", "0"));
            int shutter_inverse = Convert.ToInt32(dev_ini_file.ReadString("EXTERNAL_IO", "TTL4_Inverse", "0")); //shutter is TTL_OUT4            

            spec_para[Index].cCode = ccode;
            spec_para[Index].channel = Index;
            spec_para[Index].spectrometer_type = spectrometer_type;
            spec_para[Index].model = model;
            spec_para[Index].pixel_number = pixelnumber;
            spec_para[Index].timing_mode = timing_mode;
            spec_para[Index].input_mode = input_mode;
            spec_para[Index].inttime_base = inttime_base;
            spec_para[Index].inttime_unit = inttime_unit;
            spec_para[Index].inttime_min = inttime_min;

            spec_para[Index].inttime = inttime_min;
            if (spec_para[Index].spectrometer_type == (int)TypeID.BRC115_MODE) { spec_para[Index].inttime = 1050; }
            if (spec_para[Index].spectrometer_type == (int)TypeID.BRC115P_MODE) { spec_para[Index].inttime = 1050; }
            spec_para[Index].inttime_int = (int)inttime;
            spec_para[Index].trigger_mode = 0;
            spec_para[Index].xaxis_data_reverse = xaxis_data_reverse;
            spec_para[Index].coefficient_a0 = coefficeint_a[0];
            spec_para[Index].coefficient_a1 = coefficeint_a[1];
            spec_para[Index].coefficient_a2 = coefficeint_a[2];
            spec_para[Index].coefficient_a3 = coefficeint_a[3];
            spec_para[Index].coefficient_b0 = coefficeint_b[0];
            spec_para[Index].coefficient_b1 = coefficeint_b[1];
            spec_para[Index].coefficient_b2 = coefficeint_b[2];
            spec_para[Index].coefficient_b3 = coefficeint_b[3];
            spec_para[Index].xaxis_data_reverse = xaxis_data_reverse;
            spec_para[Index].shutter_inverse = shutter_inverse;

            spec_para[Index].wavelength = new double[pixelnumber + 1];
            for (int i = 0; i < pixelnumber; i++)
            {
                spec_para[Index].wavelength[i] = coefficeint_a[0] + coefficeint_a[1] * Math.Pow(i, 1) + coefficeint_a[2] * Math.Pow(i, 2) + coefficeint_a[3] * Math.Pow(i, 3);
            }
            dev_paras = spec_para;

        }

        public bool status;

             // Read out CCD device EEPROM information
        public ReadEEPROM()
        {
            int retcode;
            StringBuilder fileloc = new StringBuilder("c:\\temp\\para.ini");
            retcode = bwtekReadEEPROMUSB(fileloc, 0);

            if (retcode < 0)
                status = false;
            else
            {
                Load_para(fileloc.ToString(), 0);
                status=true;
            }

        }



    }
}
