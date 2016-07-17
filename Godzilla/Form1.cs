// Written by Dr. Zhi Guo
// version 1.00 alpha released in 2016.7
// Several advanced functions are still not implemented
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Steema.TeeChart;
using Newport;
using NationalInstruments.DAQmx;


using Diagnostics.Logging.Applet;
using Newport.Communication.VCP;
using System.Runtime.InteropServices;
using CyUSB;



namespace Godzilla
{
    public partial class Form1 : Form
    {
        [DllImport("BWTEKUSB.DLL")]
        static extern int bwtekTestUSB(int TimngMode, int PixelNumber, int InputMode, int chnnel, int pParam);
        [DllImport("BWTEKUSB.DLL")]
        static extern int bwtekSetTimeUSB(int lTime, int nChannel);
        [DllImport("BWTEKUSB.DLL")]
        static extern int bwtekDataReadUSB(int nTriggerMode, [In, Out] UInt16[] MemHandle, int nChannel);
        [DllImport("BWTEKUSB.DLL")]
        static extern Int32 bwtekFrameDataReadUSB(Int32 nFrameNum, Int32 nTriggerMode, UInt16[] MemHandle, Int32 nChannel);
        [DllImport("BWTEKUSB.DLL")]
        static extern int bwtekCloseUSB(int nChannel);
        [DllImport("BWTEKUSB.DLL")]
        static extern int bwtekGetTTLIn(int nNo, [In, Out] int[] pGetValue, int chnnel);

        public adjust_whitelight inst_whiteform;
        public Monitor_sample_abs inst_sample_abs;
        public splash splash_screen;
        public bool bwtek_OK;


        private Steema.TeeChart.Styles.ColorGrid mycolorgrid;
        private Steema.TeeChart.Styles.Line myline1;
        private Steema.TeeChart.Styles.Line myline2;
        private Steema.TeeChart.Styles.Line aveline;

        Steema.TeeChart.Styles.FastLine myfastline;


        public class kinetic_view
        {
            public double xmin;
            public double xmax;
            public double ymin;
            public double ymax;

            public kinetic_view()
            {
                xmin = 0.0;
                xmax = 1500.0;
                ymin = 0.0;
                ymax = 100.0;
            }
        }


        private int retcode = 0;

        public static double current_probe_wavelength;

        public static double current_delay_ps;

        public static double current_integration_time;

        public static double current_time_zero;

        public static int request_num_scans;

        public static double physical_delay_min;

        public static double physical_delay_max;

        public ReadEEPROM acquire_bwtek_EEPROM;

        public static double[] wavelength;

        public static UInt16[] spec_array_in_mem;

        public static int num_of_ave;

        public double[] current_OD_values;

        public double[] relative_delay_points;

        public static CommandInterfaceSMC100.SMC100 mySMC;

        public double[] concat_points;

        public static int reduction_mode;

        public double[, ,] history_storage;

        public double[,] average_scan;

        public System.Threading.Thread update_spec_kin;

        public volatile bool scan_suspended;

        public volatile bool terminate_scan;






        public Form1()
        {
            InitializeComponent();
            splash_screen = new splash();
            splash_screen.Show();
            if (DO_initialization_on_bwtek())
            {
                bwtek_OK = true;
                toolStripStatusLabel2.Text = "Spectrometer communication OK";
                toolStripStatusLabel2.ForeColor = System.Drawing.Color.ForestGreen;
                toolStripStatusLabel2.BackColor = System.Drawing.Color.White;
            }
            else
            {
                bwtek_OK = false;
                string mytext = "Spectrometer communication Error";
                toolStripStatusLabel1.Text = mytext;
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;

                while (!bwtek_OK)
                {
                    MessageBox.Show("Failed to find spectrometer, try to unplug and restart the spectromemter!");
                    System.Threading.Thread.Sleep(3000);
                    int flag;
                    flag = bwtekTestUSB(1, 2048, 1, 0, 0);
                    if (retcode < 0)
                        bwtek_OK = false;
                }

            }

            splash_screen.Close();

            //initialize all the components, their default values

            radioButton1.Checked = true;
            radioButton2.Checked = false;
            radioButton3.Checked = false;
            radioButton4.Checked = true;
            radioButton5.Checked = true;
            radioButton6.Checked = false;
            radioButton7.Checked = false;
            radioButton8.Checked = false;

            reduction_mode = 1;
            current_integration_time = 1.0;
            textBox13.Text = Convert.ToString(current_integration_time);
            current_probe_wavelength = 590.0;
            textBox2.Text = Convert.ToString(current_probe_wavelength);
            current_delay_ps = 0.0;
            textBox1.Text = Convert.ToString(current_delay_ps);
            current_time_zero = 0.0;
            textBox3.Text = Convert.ToString(current_time_zero);
            request_num_scans = 8;
            textBox15.Text = Convert.ToString(request_num_scans);
            physical_delay_min = -500.0;
            physical_delay_max = 500.0;



            acquire_bwtek_EEPROM = new ReadEEPROM();

            wavelength = acquire_bwtek_EEPROM.dev_paras[0].wavelength;


            while (!acquire_bwtek_EEPROM.status)
            {
                MessageBox.Show("Failed to access EEPROM, try to unplug and restart the spectromemter!");
                acquire_bwtek_EEPROM = new ReadEEPROM();
            }


            // Setup CCD exposure time
            retcode = bwtekSetTimeUSB(1000, 0);
            if (retcode < 0)
            {
                MessageBox.Show("Set spectrometer integration time failed!");
            }


            // test stage and reset it to home position
            test_motion_stage();



            // Next attach TChart objects to the controls
            // 3 TeeChart controls in this window
            tChart1.Series.Add(new Steema.TeeChart.Styles.ColorGrid());
            mycolorgrid = (tChart1.Series[0] as Steema.TeeChart.Styles.ColorGrid);
            mycolorgrid.Pen.Visible = false;
            tChart1.Legend.Visible = false;
            tChart1.Header.Visible = false;
            tChart1.Axes.Visible = false;
            tChart1.Series.Add(new Steema.TeeChart.Styles.ColorGrid());
            mycolorgrid.EndColor = Color.Red;
            mycolorgrid.StartColor = Color.Blue;
            mycolorgrid.MidColor = Color.ForestGreen;



            tChart2.Legend.Visible = false;
            tChart2.Walls.Visible = false;
            tChart2.Header.Visible = false;
            tChart2.Series.Add(new Steema.TeeChart.Styles.Line());
            myline1 = new Steema.TeeChart.Styles.Line();

            tChart3.Legend.Visible = false;
            tChart3.Walls.Visible = false;
            tChart3.Header.Visible = false;
            tChart3.Series.Add(new Steema.TeeChart.Styles.Line());
            myline2 = new Steema.TeeChart.Styles.Line();

            tChart3.Series.Add(new Steema.TeeChart.Styles.Line());
            aveline = new Steema.TeeChart.Styles.Line();


            myline1 = (tChart2.Series[0] as Steema.TeeChart.Styles.Line);
            myline1.Pointer.Visible = true;
            myline1.Pointer.Style = Steema.TeeChart.Styles.PointerStyles.Diamond;

            myline2 = (tChart3.Series[0] as Steema.TeeChart.Styles.Line);
            myline2.Pointer.Visible = true;
            myline2.Pointer.Style = Steema.TeeChart.Styles.PointerStyles.Circle;

            aveline = (tChart3.Series[1] as Steema.TeeChart.Styles.Line);
            aveline.Color = Color.ForestGreen;
            aveline.Pointer.Visible = false;
            // aveline.Pointer.Style = Steema.TeeChart.Styles.PointerStyles.Nothing;


            //kinetic_view current_kin_view = new kinetic_view();

            num_of_ave = Convert.ToInt32(current_integration_time * 400);
            spec_array_in_mem = new UInt16[num_of_ave * 2048];

            scan_suspended = false;
            terminate_scan = false;


        }




        public bool test_motion_stage()
        {
            int retcode;
            mySMC = new CommandInterfaceSMC100.SMC100();
            retcode = mySMC.OpenInstrument("COM1");


            if (retcode == -1)
            {
                MessageBox.Show("Open SMC controler failed!");
                return false;
            }
            else
            {
                // string[] device_string=mySMC.GetDevices();

                // Enter Configuration mode
                if (!config_stage(mySMC))
                    MessageBox.Show("Failed to configure the stage parameter!");

                // Homing
                Go_home(mySMC);
                return true;
            }
        }

        private bool config_stage(CommandInterfaceSMC100.SMC100 mySMC)
        {
            string myerr_info;
            // Let's do SmartStage reset.
            int ret_code;
            ret_code = mySMC.ZX_Set(1, 3, out myerr_info);
            if (ret_code == -1)
                return false;
            else
                return true;

        }

        private void Go_home(CommandInterfaceSMC100.SMC100 mySMC)
        {
            string myerr_info;
            string myerr_code;
            string current_state;
            string code = "";
            string match_code = "1E";
            int ret_code;
            ret_code = mySMC.OR(1, out myerr_info);
            if (ret_code == -1)
                MessageBox.Show("Failed to execute OR command (Homing command)!");
            do
            {
                ret_code = mySMC.TS(1, out myerr_code, out current_state, out myerr_info);
                if (ret_code == -1)
                    MessageBox.Show("Failed to execute TS command (mov query command)!");

            } while (string.Compare(code, match_code) == 0);
        }

        public delegate void helpinvoke();

        public void Update_UI()
        {
            tChart1.Refresh();
            tChart2.Refresh();
            tChart3.Refresh();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Read checkbox status to determine how many delay time point schemes present
            concat_points = new double[0];
            bool radio1_checked = checkBox1.Checked;
            bool radio2_checked = checkBox2.Checked;
            bool radio3_checked = checkBox3.Checked;

            double R1_from_value = 0.0;
            double R1_to_value = 0.0;
            double R1_step_size = 0.0;

            double R2_from_value = 0.0;
            double R2_to_value = 0.0;
            double R2_step_size = 0.0;


            double R3_from_value = 0.0;
            double R3_to_value = 0.0;
            double R3_step_size = 0.0;


            double[] R1_points = new double[0];
            double[] R2_points = new double[0];
            double[] R3_points = new double[0];


            int R1_point_num = 0;
            int R2_point_num = 0;
            int R3_point_num = 0;

            bool at_least_one_checked = radio1_checked | radio2_checked | radio3_checked;

            if (!at_least_one_checked)
            {
                MessageBox.Show("Need to check at least one scheme!");
                return;
            }

            if (radio1_checked)
            {
                // check from value, to value and step size
                try
                {
                    R1_from_value = Convert.ToDouble(textBox4.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid from value in Range 1!");
                    return;
                }

                try
                {
                    R1_to_value = Convert.ToDouble(textBox5.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid to value in Range 1!");
                    return;
                }

                try
                {
                    R1_step_size = Convert.ToDouble(textBox6.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid step size in Range 1!");
                    return;
                }

                if (R1_from_value < (physical_delay_min + current_time_zero))
                {
                    R1_from_value = physical_delay_min + current_time_zero;
                    textBox4.Text = Convert.ToString(R1_from_value);
                }

                if (R1_to_value > (physical_delay_max + current_time_zero))
                {
                    R1_to_value = physical_delay_max + current_time_zero;
                    textBox4.Text = Convert.ToString(R1_to_value);
                }

                if (R1_from_value >= R1_to_value)
                {
                    MessageBox.Show("Please make sure start point is smaller than end point in Range 1");
                    return;
                }

                if (R1_step_size <= 0.0)
                {
                    MessageBox.Show("Range 1 step size need to be positive");
                    return;
                }
                else
                {
                    if (R1_step_size > (R1_to_value - R1_from_value))
                    {
                        R1_step_size = R1_to_value - R1_from_value;
                        R1_point_num = 1;
                    }
                    else
                    {
                        R1_point_num = Convert.ToInt32(((R1_to_value - R1_from_value) / R1_step_size)) + 1;
                    }


                    // generate linear scheme
                    R1_points = new double[R1_point_num];
                    R1_points[0] = R1_from_value;
                    for (int i = 1; i < R1_point_num; i++)
                    {
                        R1_points[i] = R1_points[i - 1] + R1_step_size;
                    }


                }

            }

            if (radio2_checked)
            {
                // check from value, to value and step size
                try
                {
                    R2_from_value = Convert.ToDouble(textBox7.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid from value in Range 2!");
                    return;
                }

                try
                {
                    R2_to_value = Convert.ToDouble(textBox8.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid to value in Range 2!");
                    return;
                }

                try
                {
                    if (radioButton1.Checked)
                        R2_step_size = Convert.ToDouble(textBox9.Text);
                    else
                    {
                        if (radioButton2.Checked)
                            R2_point_num = Convert.ToInt32(textBox9.Text);
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid step size in Range 2!");
                    return;
                }


                if (R2_from_value < (physical_delay_min + current_time_zero))
                {
                    R2_from_value = physical_delay_min + current_time_zero;
                    textBox7.Text = Convert.ToString(R2_from_value);
                }

                // Add one line to check if Range 1 exist, validity check for from value

                if (radio1_checked)
                {
                    if (R2_from_value < R1_to_value)
                    {
                        MessageBox.Show("Invalid from value in Range 2, it needs to be larger than to value in Range 1");
                        return;
                    }
                }


                if (R2_to_value > (physical_delay_max + current_time_zero))
                {
                    R2_to_value = physical_delay_max + current_time_zero;
                    textBox8.Text = Convert.ToString(R2_to_value);
                }

                if (R2_from_value >= R2_to_value)
                {
                    MessageBox.Show("Please make sure start point is smaller than end point in Range 2");
                    return;
                }

                if (radioButton1.Checked)
                {
                    if (R2_step_size <= 0.0)
                    {
                        MessageBox.Show("Range 2 step size need to be positive");
                        return;
                    }
                    else
                    {
                        if (R2_step_size > (R2_to_value - R2_from_value))
                        {
                            R2_step_size = R2_to_value - R2_from_value;
                            R2_point_num = 1;
                        }
                        else
                        {
                            R2_point_num = Convert.ToInt32(((R2_to_value - R2_from_value) / R2_step_size)) + 1;
                        }


                        // generate linear scheme
                        R2_points = new double[R2_point_num];
                        R2_points[0] = R2_from_value;
                        for (int i = 1; i < R2_point_num; i++)
                        {
                            R2_points[i] = R2_points[i - 1] + R2_step_size;
                        }


                    }
                }
                else
                {
                    if (R2_point_num <= 0)
                    {
                        MessageBox.Show("Range 2 Point number need to be positive");
                        return;
                    }
                    else
                    {
                        // generate log scheme
                        R2_points = new double[R2_point_num];
                        R2_points[0] = R2_from_value;
                        double shift_value = 0.0;
                        if (R2_from_value < 0)
                        {
                            shift_value = (1.0 - R2_from_value);
                        }

                        double log_length = Math.Log10((R2_to_value + shift_value) / (R2_from_value + shift_value));
                        for (int i = 1; i < R2_point_num; i++)
                        {

                            R2_points[i] = (R2_from_value + shift_value) * Math.Pow(10.0, log_length / (R2_point_num - 1) * i) - shift_value;

                        }


                    }
                }


            }

            if (radio3_checked)
            {
                // check from value, to value and step size
                try
                {
                    R3_from_value = Convert.ToDouble(textBox10.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid from value in Range 3!");
                    return;
                }

                try
                {
                    R3_to_value = Convert.ToDouble(textBox11.Text);
                }
                catch
                {
                    MessageBox.Show("Invalid to value in Range 3!");
                    return;
                }

                try
                {
                    if (radioButton3.Checked)
                        R3_step_size = Convert.ToDouble(textBox12.Text);
                    else
                    {
                        if (radioButton4.Checked)
                            R2_point_num = Convert.ToInt32(textBox12.Text);
                    }
                }
                catch
                {
                    MessageBox.Show("Invalid step size in Range 3!");
                    return;
                }


                if (R3_from_value < (physical_delay_min + current_time_zero))
                {
                    R3_from_value = physical_delay_min + current_time_zero;
                    textBox10.Text = Convert.ToString(R3_from_value);
                }

                // Add one line to check if Range 1 exist, validity check for from value

                if (radio1_checked)
                {
                    if (R3_from_value < R1_to_value)
                    {
                        MessageBox.Show("Invalid from value in Range 3, it needs to be larger than to value in Range 1");
                        return;
                    }
                }

                if (radio2_checked)
                {
                    if (R3_from_value < R2_to_value)
                    {
                        MessageBox.Show("Invalid from value in Range 3, it needs to be larger than to value in Range 2");
                        return;
                    }
                }


                if (R3_to_value > (physical_delay_max + current_time_zero))
                {
                    R3_to_value = physical_delay_max + current_time_zero;
                    textBox11.Text = Convert.ToString(R3_to_value);
                }

                if (R3_from_value >= R3_to_value)
                {
                    MessageBox.Show("Please make sure start point is smaller than end point in Range 3");
                    return;
                }

                if (radioButton3.Checked)
                {
                    if (R3_step_size <= 0.0)
                    {
                        MessageBox.Show("Range 3 step size need to be positive");
                        return;
                    }
                    else
                    {
                        if (R3_step_size > (R3_to_value - R3_from_value))
                        {
                            R3_step_size = R3_to_value - R3_from_value;
                            R3_point_num = 1;
                        }
                        else
                        {
                            R3_point_num = Convert.ToInt32(((R3_to_value - R3_from_value) / R3_step_size)) + 1;
                        }


                        // generate linear scheme
                        R3_points = new double[R3_point_num];
                        R3_points[0] = R3_from_value;
                        for (int i = 1; i < R3_point_num; i++)
                        {
                            R3_points[i] = R3_points[i - 1] + R3_step_size;
                        }


                    }
                }
                else
                {
                    if (R3_point_num <= 0)
                    {
                        MessageBox.Show("Range 3 Point number need to be positive");
                        return;
                    }
                    else
                    {
                        // generate log scheme
                        R3_points = new double[R3_point_num];
                        R3_points[0] = R3_from_value;
                        double shift_value = 0.0;
                        if (R3_from_value < 0)
                        {
                            shift_value = (1.0 - R3_from_value);
                        }

                        double log_length = Math.Log10((R3_to_value + shift_value) / (R3_from_value + shift_value));
                        for (int i = 1; i < R3_point_num; i++)
                        {

                            R3_points[i] = (R3_from_value + shift_value) * Math.Pow(10.0, log_length / (R3_point_num - 1) * i) - shift_value;

                        }


                    }
                }

            }

            // Start data acquision
            //first concatenate data points sequentially


            if (radio1_checked)
            {
                int old_pos = concat_points.Length;
                Array.Resize(ref concat_points, concat_points.Length + R1_points.Length);
                R1_points.CopyTo(concat_points, old_pos);
            }
            if (radio2_checked)
            {
                int old_pos = concat_points.Length;
                Array.Resize(ref concat_points, concat_points.Length + R2_points.Length);
                R2_points.CopyTo(concat_points, old_pos);

            }
            if (radio3_checked)
            {
                int old_pos = concat_points.Length;
                Array.Resize(ref concat_points, concat_points.Length + R3_points.Length);
                R3_points.CopyTo(concat_points, old_pos);
            }


            //generate relative delay gap

            relative_delay_points = new double[concat_points.Length - 1];


            generate_delay_points(ref concat_points);

            // move stage along designated delay points
            button5.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            //     data_scan(ref concat_points);
            aveline.Clear();
            update_spec_kin = new System.Threading.Thread(data_scan);
            update_spec_kin.Start();


            button1.Enabled = true;
            button2.Enabled = true;

                            

            // implement code for averaging



        }

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox1.Text = text;
            }
        }

        delegate void forceUpdateCallback();
        private void forceUpdate()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox1.InvokeRequired)
            {
                forceUpdateCallback d = new forceUpdateCallback(forceUpdate);
                this.Invoke(d);
            }
            else
            {
                this.textBox1.Update();
            }
        }

        delegate void forceUpdateCallback2();
        private void forceUpdate2()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.tChart2.InvokeRequired)
            {
                forceUpdateCallback2 d = new forceUpdateCallback2(forceUpdate2);
                this.Invoke(d);
            }
            else
            {
                this.tChart2.Refresh();
            }
        }

        delegate void forceUpdateCallback3();
        private void forceUpdate3()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.tChart3.InvokeRequired)
            {
                forceUpdateCallback3 d = new forceUpdateCallback3(forceUpdate3);
                this.Invoke(d);
            }
            else
            {
                this.tChart3.Refresh();
            }
        }

        delegate void enableButtonCallback(bool status);
        private void enableButton(bool status)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.button5.InvokeRequired)
            {
                enableButtonCallback d = new enableButtonCallback(enableButton);
                this.Invoke(d, new object[] { status });
            }
            else
            {
                this.button5.Enabled = status;
            }
        }


        delegate void updateScanNumCallback(int current_num);
        private void updateScanNum(int current_num)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox15.InvokeRequired)
            {
                updateScanNumCallback d = new updateScanNumCallback(updateScanNum);
                this.Invoke(d, new object[] { current_num });
            }
            else
            {
                this.textBox15.Text = Convert.ToString(request_num_scans) + "(" + Convert.ToString(current_num + 1) + ")";
            }
        }

        delegate void updateRemainTimeCallback(int current_num);
        private void updateRemainTime(int current_num)
        {
            if (this.textBox16.InvokeRequired)
            {
                updateRemainTimeCallback d = new updateRemainTimeCallback(updateRemainTime);
                this.Invoke(d, new object[] { current_num });
            }
            else
            {
                int rest = request_num_scans - current_num;
                double remain_time = rest * (relative_delay_points.Length + 1) * current_integration_time / 60.0;
                this.textBox16.Text = Convert.ToString(remain_time);
            }
        }

        delegate void forceUpdateColorGridCallback();
        private void forceUpdateColorGrid()
        {
            if (this.tChart1.InvokeRequired)
            {
                forceUpdateColorGridCallback d = new forceUpdateColorGridCallback(forceUpdateColorGrid);
                this.Invoke(d);
            }
            else
            {
                this.tChart1.Refresh();
            }
        }

        private void wrap_writeline(System.IO.StreamWriter indiv_file, ref double[] write_array)
        {
            indiv_file.Write(Convert.ToString(current_delay_ps) + " ");
            double local_ave = 0.0;
            for (int i = 0; i < 2048 - reduction_mode + 1; i++)
            {
                for (int j = i; j < i + reduction_mode; j++)
                {
                    local_ave += write_array[j];
                }
                local_ave /= reduction_mode;

                indiv_file.Write(Convert.ToString(local_ave) + " ");

            }
            indiv_file.WriteLine();
        }

        private void acquire_ave_from_history(int past_scans)
        {
            for (int i = 0; i < relative_delay_points.Length + 1; i++)
                for (int j = 0; j < 2048; j++)
                    average_scan[i, j] = 0.0;

            for (int k = 0; k < past_scans; k++)
                for (int i = 0; i < relative_delay_points.Length + 1; i++)
                    for (int j = 0; j < 2048; j++)
                        average_scan[i, j] += history_storage[k, i, j];

            for (int i = 0; i < relative_delay_points.Length + 1; i++)
                for (int j = 0; j < 2048; j++)
                    average_scan[i, j] /= past_scans;


        }

        private void last_write_average(ref double[,] average_scan)
        {
            string avepath = "test_ave.csv";
            System.IO.StreamWriter ave_file = new System.IO.StreamWriter(avepath, true);

            ave_file.Write("0.00000");

            for (int i = 0; i < concat_points.Length; i++)
                ave_file.Write(","+Convert.ToString(concat_points[i]));

            ave_file.WriteLine();



            for (int j = 0; j < 2048 - reduction_mode + 1; j++)
            {
                double local_wave_ave = 0.0;
                for (int s = j; s < j + reduction_mode; s++)
                    local_wave_ave = local_wave_ave + wavelength[s];
                local_wave_ave = local_wave_ave / Form1.reduction_mode;

                ave_file.Write(Convert.ToString(local_wave_ave));

                for (int i = 0; i < concat_points.Length; i++)
                    ave_file.Write("," + Convert.ToString(average_scan[i, j]));

                ave_file.WriteLine();
            }


            ave_file.Close();

        }

        private void data_scan()
        {
            mycolorgrid.Clear();
            myline1.Clear();
            myline2.Clear();
            aveline.Clear();

            history_storage = new double[request_num_scans, relative_delay_points.Length + 1, 2048];
            average_scan = new double[relative_delay_points.Length + 1, 2048];
            for (int s = 0; s < request_num_scans; s++)
            {
                mycolorgrid.Clear();
                updateScanNum(s);
                updateRemainTime(s);
                // Try to change the line style, or only show the average curve
                string filepath = "test_" + Convert.ToString(s) + ".txt";
                System.IO.StreamWriter indiv_file = new System.IO.StreamWriter(filepath, true);

                wrap_writeline(indiv_file, ref wavelength);

                Abs_Jog(mySMC, convert_ps_to_abs(concat_points[0] - current_time_zero));
                current_delay_ps = concat_points[0];
                SetText(Convert.ToString(current_delay_ps));
                forceUpdate();
                spectra_acquire_once(s, 0, out current_OD_values);
                forceUpdate2();
                //  indiv_file.WriteLine(current_OD_values);



                wrap_writeline(indiv_file, ref current_OD_values);

                update_kin_plot();
                while (scan_suspended) ;
                // pause thread

                if (terminate_scan)
                {
                    indiv_file.Close();
                    break;
                }

                for (int i = 1; i < relative_delay_points.Length; i++)
                {
                    Relative_Jog(mySMC, convert_ps_to_abs(relative_delay_points[i]));
                    current_delay_ps = concat_points[i];
                    SetText(Convert.ToString(current_delay_ps));
                    forceUpdate();
                    while (!query_move_status(mySMC)) ;
                    spectra_acquire_once(s, i + 1, out current_OD_values);
                    forceUpdate2();
                    update_kin_plot();
                    forceUpdate3();
                    // Need to save file
                    //    indiv_file.WriteLine(current_OD_values);
                    wrap_writeline(indiv_file, ref current_OD_values);
                    while (scan_suspended) ;

                    if (terminate_scan)
                    {
                        indiv_file.Close();
                        break;
                    }

                }

                // Finished one scan, Let us erase last old scan and replace it with a yellow average curve;
                myline2.Clear();
                acquire_ave_from_history(s + 1);
                update_ave_plot();
                forceUpdate3();

                // update colorgrids
                forceUpdateColorGrid();


                indiv_file.Close();
            }

            helpinvoke invoke_test = new helpinvoke(Update_UI);

            Invoke(invoke_test);

            last_write_average(ref average_scan);
            //button5.Enabled = true;
            enableButton(true);
            terminate_scan = false;
        }

        private void update_ave_plot()
        {
            int border_index = 0; ;

            //   border_index = Array.FindLastIndex(wavelength, search_for_idx);

            for (int i = 0; i < 2048; i++)
            {
                if (wavelength[i] > current_probe_wavelength)
                {
                    border_index = i;
                    break;
                }
            }


            double left_bound = Math.Abs(wavelength[border_index - 1] - current_probe_wavelength);
            double right_bound = Math.Abs(wavelength[border_index] - current_probe_wavelength);
            if (left_bound > right_bound)
                border_index--;

            //retrieve data based on index
            // preset myline2 style to yellow solid lines
            aveline.Clear();
            for (int i = 0; i < relative_delay_points.Length + 1; i++)
            {
                aveline.Add(concat_points[i], average_scan[i, border_index]);
            }
        }




        private void update_kin_plot()
        {
            // first convert probe wavelength to index;

            int border_index = 0; ;

            //   border_index = Array.FindLastIndex(wavelength, search_for_idx);

            for (int i = 0; i < 2048; i++)
            {
                if (wavelength[i] > current_probe_wavelength)
                {
                    border_index = i;
                    break;
                }
            }


            double left_bound = Math.Abs(wavelength[border_index - 1] - current_probe_wavelength);
            double right_bound = Math.Abs(wavelength[border_index] - current_probe_wavelength);
            if (left_bound > right_bound)
                border_index--;

            //retrieve data based on index

            //MessageBox.Show(Convert.ToString(border_index));

            double local_ave = 0.0;
            for (int i = 0; i < reduction_mode; i++)
            {
                int half = (int)(reduction_mode / 2);
                local_ave += current_OD_values[border_index - half + i];
            }
            local_ave /= reduction_mode;

            //      myline2.Add(current_delay_ps, current_OD_values[border_index]);
            myline2.Add(current_delay_ps, local_ave);



        }

        private bool search_for_idx(double test_wavelength)
        {

            if (test_wavelength > current_probe_wavelength)
                return false;
            else
                return true;
        }

        private void delay(long t)
        {
            long b = DateTime.Now.Ticks / 10;
            long e = 0; long c = 0; ;
            do
            {
                e = DateTime.Now.Ticks / 10;
                c = e - b;
                Application.DoEvents();
            }
            while (c < t);
        }

        private void spectra_acquire_once(int current_scan, int current_delay, out double[] current_OD_ave)
        {

            double[] OD_values;
            int[] pin_status = new int[1];
            int retcode;


            UInt16[] previous_spec = new UInt16[2048];
            double[] sum = new double[2048];
            UInt16[] spec_in_mem = new UInt16[2048];


            int previous_stat = 1;
           int previous_stat_next = 1;
            pin_status[0] = 1;
            while (true)
            {
                retcode = bwtekGetTTLIn(0, pin_status, 0);
                //    System.Threading.Thread.Sleep(1);

                if (retcode < 0)
                    MessageBox.Show("Query TTL pin 5 failed!");
                if (previous_stat == 0 && previous_stat_next==0 && pin_status[0] == 1)
                    break;

                previous_stat = pin_status[0];
                previous_stat_next = pin_status[0];
            }
            num_of_ave = Convert.ToInt32(Form1.current_integration_time * 400);  // default to 1 ms integration time
            spec_array_in_mem = new UInt16[num_of_ave * 2048];

         //   delay(700);

            for (int i = 0; i < num_of_ave; i++)
            {

                retcode = bwtekDataReadUSB(1, spec_in_mem, 0);
                //  retcode=bwtekFrameDataReadUSB(num_of_ave, 1, spec_in_mem, 0);
                if (retcode == -1)
                {
                    MessageBox.Show("Read data failed");
                }
                for (int j = 0; j < 2048; j++)
                    spec_array_in_mem[i * 2048 + j] = spec_in_mem[j];
            }


            //   retcode=bwtekFrameDataReadUSB(num_of_ave, 1, spec_array_in_mem, 0);

            for (int j = 0; j < 2048; j++)
                sum[j] = 0.0;

            for (int i = 0; i < num_of_ave - 1; i++)
            {

                Array.Copy(spec_array_in_mem, i * 2048, previous_spec, 0, 2048);
                Array.Copy(spec_array_in_mem, (i + 1) * 2048, spec_in_mem, 0, 2048);
                // To OD_values;

                if (pin_status[0] == 1)
                {
                    if (i % 2 == 0)
                        adjust_whitelight.calc_OD(previous_spec, spec_in_mem, out OD_values);
                    else
                        adjust_whitelight.calc_OD(spec_in_mem, previous_spec, out OD_values);
                }
                else
                {
                    if (i % 2 == 1)
                        adjust_whitelight.calc_OD(previous_spec, spec_in_mem, out OD_values);
                    else
                        adjust_whitelight.calc_OD(spec_in_mem, previous_spec, out OD_values);
                }


                for (int j = 0; j < 2048; j++)
                {
                    sum[j] = sum[j] + OD_values[j];
                }
            }


            for (int j = 0; j < 2048; j++)
            {
                sum[j] = sum[j] / num_of_ave;

            }

            myline1.Clear();

            for (int j = 0; j < 2048 - reduction_mode + 1; j++)
            {
                double local_ave = 0.0;
                double local_wave_ave = 0.0;
                for (int s = j; s < j + reduction_mode; s++)
                {
                    local_ave = local_ave + sum[s];
                    local_wave_ave = local_wave_ave + wavelength[s];
                }

                local_ave = local_ave / Form1.reduction_mode;
                local_wave_ave = local_wave_ave / Form1.reduction_mode;

                if (local_wave_ave > 450.0)
                    myline1.Add(local_wave_ave, local_ave);
                history_storage[current_scan, current_delay, j] = local_ave;

                // I also would like to update the colorgrid
                if (local_wave_ave > 500.0)
                    mycolorgrid.Add(current_delay, local_ave, local_wave_ave);
            }


            current_OD_ave = sum;


        }

        public static void Abs_Jog(CommandInterfaceSMC100.SMC100 mySMC, double abs_pos)
        {
            string myerr_info;
            int ret_code;
            ret_code = mySMC.PA_Set(1, abs_pos, out myerr_info);
            if (ret_code == -1)
                MessageBox.Show("Failed to execute PA command (absolute move)!");
            while (!query_move_status(mySMC)) ;
        }

        private void Relative_Jog(CommandInterfaceSMC100.SMC100 mySMC, double relative_pos)
        {
            string myerr_info;
            int ret_code;
            ret_code = mySMC.PR_Set(1, relative_pos, out myerr_info);
            if (ret_code == -1)
            {
                MessageBox.Show("Failed to execute PR command (relative move)!");
            }
            while (!query_move_status(mySMC)) ;
        }

        public static bool query_move_status(CommandInterfaceSMC100.SMC100 mySMC)
        {
            string current_state;
            string myerr_info;
            string myerr_code;
            int ret_code;
            ret_code = mySMC.TS(1, out myerr_code, out current_state, out myerr_info);
            if (ret_code == -1)
            {
                MessageBox.Show("Failed to do move status query");
                return false;
            }
            int code = Convert.ToInt32(current_state);
            if (code == 33)
            {
                // This means it converts from MOVE to READY
                return true;
            }
            else
                return false;
        }

        public static double convert_ps_to_abs(double ps_value)
        {
            const double c_speed = 2.99792458e8;
            return ps_value * (1.0e-12) * c_speed * (1.0e3) / 4.0; // The return value is in mm unit
        }

        private void generate_delay_points(ref double[] myconcat)
        {

            int num_points = myconcat.Length - 1;
            for (int i = 0; i < num_points; i++)
            {
                relative_delay_points[i] = myconcat[i + 1] - myconcat[i];
            }
        }


        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                label15.Text = "point number";
            }
            else
            {
                label15.Text = "step";
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                label15.Text = "step";
            }
            else
            {
                label15.Text = "point number";
            }
        }


        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                label16.Text = "step";
            }
            else
            {
                label16.Text = "point number";
            }
        }


        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                label16.Text = "point number";
            }
            else
            {
                label16.Text = "step";
            }
        }




        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (inst_whiteform == null || inst_whiteform.IsDisposed)
            {
                inst_whiteform = new adjust_whitelight();
                inst_whiteform.Show();
            }
        }

        private bool DO_initialization_on_bwtek()
        {
            splash_screen.Refresh();

            int retcode;

            retcode = bwtekTestUSB(1, 2048, 1, 0, 0);
            if (retcode < 0)
                return false;

            System.Threading.Thread.Sleep(200);
            return true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (inst_sample_abs == null || inst_sample_abs.IsDisposed)
            {
                inst_sample_abs = new Monitor_sample_abs(ref wavelength);
                inst_sample_abs.Show();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // implement code for pausing data acquisition
            if (!scan_suspended)
            {
                scan_suspended = true;
                button3.Text = "Resume scan";
            }
            else
            {
                scan_suspended = false;
                button3.Text = "Pause, and Adjust";
            }

        }

        private void textBox13_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double changed_integration_time = Form1.current_integration_time;
                try
                {
                    changed_integration_time = Convert.ToDouble(textBox13.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    MessageBox.Show("Illegal integration time input!");
                }

                if (changed_integration_time > 0.01)
                {
                    // Illegal input, revert back;
                    Form1.current_integration_time = changed_integration_time;
                }
                else
                {
                    MessageBox.Show("Illegal integration time or integration time too short (should be > 10 ms)");
                }
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            reduction_mode = 2;
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            reduction_mode = 4;
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            reduction_mode = 8;
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void tChart1_ClickSeries(object sender, Steema.TeeChart.Styles.Series s, int valueIndex, MouseEventArgs e)
        {

            current_probe_wavelength = Convert.ToInt32(s.GetVertAxis.CalcPosValue(e.Y));
            MessageBox.Show(Convert.ToString(current_probe_wavelength)+"nm");
            myline2.Clear();
        //    update_ave_plot();
        //    update_kin_plot();

        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double changed_wavelength = Form1.current_probe_wavelength;
                try
                {
                    changed_wavelength = Convert.ToDouble(textBox2.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    
                    MessageBox.Show("Illegal wavelength input!");
                }

                if (changed_wavelength > wavelength[0] && changed_wavelength< wavelength[wavelength.Length-1])
                {
                    // Illegal input, revert back;
                    current_probe_wavelength = changed_wavelength;
                    // update myline2 and aveline
                    myline2.Clear();
                    
                }
                else
                {
                    MessageBox.Show("Illegal wavelength input (should be > 200.0 nm and <950.0 nm)");
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!terminate_scan)
            {
                terminate_scan = true;
            }

        }

        private void textBox15_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int request_num_scans = Form1.request_num_scans;
                try
                {
                    request_num_scans = Convert.ToInt32(textBox15.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    MessageBox.Show("Illegal scan No. input!");
                }

                if (request_num_scans >= 1)
                {
                    // Illegal input, revert back;
                    Form1.request_num_scans = request_num_scans;
                }
                else
                {
                    MessageBox.Show("Illegal scan No. (should be >= 1)");
                }
            }
        }


    }

}
