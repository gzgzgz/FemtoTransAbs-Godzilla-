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
    public partial class adjust_whitelight : Form
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

        private double xmin;
        private double xmax;
        private double ymin;
        private double ymax;
        private volatile bool running_thread;

      

        private double[] wavelength;
        private double[] OD_values;
        System.Threading.Thread update_tran_spec;

        private Steema.TeeChart.Styles.Line tran_spec_line;


        public adjust_whitelight()
        {
            InitializeComponent();
            textBox5.Text = Convert.ToString(Form1.current_integration_time);
            xmin = 200.0;       // unit in nm
            xmax = 950.0;      // unit in nm
            ymin = -2000.0;   // unit in mOD
            ymax = 2000.0;      // unit in mOD

            wavelength = Form1.wavelength;

            checkBox1.Checked = true;
            checkBox2.Checked = true;

            textBox1.Text = Convert.ToString(xmin);
            textBox2.Text = Convert.ToString(xmax);
            textBox3.Text = Convert.ToString(ymin);
            textBox4.Text = Convert.ToString(ymax);

            textBox6.Text = Convert.ToString(Form1.current_delay_ps);

            tChart1.Legend.Visible = false;
            tChart1.Walls.Visible = false;
            tChart1.Header.Visible = false;
            tChart1.Series.Add(new Steema.TeeChart.Styles.Line());

            tran_spec_line = (tChart1.Series[0] as Steema.TeeChart.Styles.Line);

            running_thread = true;

            //Create a thread that continuously read spectral data from bwtek and update UI
            update_tran_spec = new System.Threading.Thread(obtain_ODs);
            update_tran_spec.Start();
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            running_thread = false;
         //   while (update_tran_spec.IsAlive);
            update_tran_spec.Abort();
            //System.Threading.Thread.Sleep(1000);
            this.Close();
        }

        public delegate void helpinvoke();

        public void Update_UI()
        {
            tChart1.Refresh();
        }

        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // check value, if valid, accept. Otherwise, go back to previous
                
                double changed_delay=Form1.current_delay_ps;
                try
                {
                    changed_delay = Convert.ToDouble(textBox6.Text);
                }
                catch 
                {
                    // Illegal input, revert back;
                    textBox6.Text = Convert.ToString(Form1.current_delay_ps);
                }

                if ( changed_delay<Form1.physical_delay_min || changed_delay>Form1.physical_delay_max)
                {
                    // Illegal input, revert back;
                    textBox6.Text = Convert.ToString(Form1.current_delay_ps);
                }
                else
                {
                    Form1.current_delay_ps = Convert.ToDouble(textBox6.Text);
                }
                Form1.Abs_Jog(Form1.mySMC, Form1.convert_ps_to_abs(Form1.current_delay_ps+Form1.current_time_zero));
            }
        }


        public static void calc_OD(UInt16[] previous_scan, UInt16[] current_scan, out double[] OD_values)
        {
             double[] myOD = new double[(previous_scan.Length)];
            for (int i = 0; i < previous_scan.Length; i++)
            {
               myOD[i] = Math.Log10(Convert.ToDouble(current_scan[i]) / Convert.ToDouble(previous_scan[i]));
              //  myOD[i] = Convert.ToDouble(previous_scan[i]);

            }
            OD_values = myOD;
        }



        
        private void obtain_ODs()
        {
            int retcode=0;
            int[] pin_status = new int[1];

            UInt16[] previous_spec = new UInt16[2048];
            double[] sum = new double[2048];
            UInt16[] spec_in_mem = new UInt16[2048];
            helpinvoke invoke_test = new helpinvoke(Update_UI);

            

            while (running_thread)
            {
                int previous_stat=1;
                pin_status[0]=0;
                while(true)
                {
                    retcode = bwtekGetTTLIn(0, pin_status, 0);
                //    System.Threading.Thread.Sleep(1);

                    if (retcode < 0)
                        MessageBox.Show("Query TTL pin 5 failed!");
                    if (previous_stat == 0 && pin_status[0] == 1)
                        break;

                    previous_stat = pin_status[0];
                }

          //      MessageBox.Show("Stop");

                int num_of_ave = Convert.ToInt32(Form1.current_integration_time * 400);  // default to 1 ms integration time
                UInt16[] spec_array_in_mem = new UInt16[num_of_ave * 2048];

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
                            calc_OD(previous_spec, spec_in_mem, out OD_values);
                        else
                            calc_OD(spec_in_mem, previous_spec, out OD_values);
                    }
                    else
                    {
                        if (i % 2 == 1)
                            calc_OD(previous_spec, spec_in_mem, out OD_values);
                        else
                            calc_OD(spec_in_mem, previous_spec, out OD_values);
                    }
                

                    for (int j = 0; j < 2048; j++)
                    {
                        sum[j] = sum[j] + OD_values[j];
                    }
                }
                tran_spec_line.Clear();
                for (int j = 0; j < 2048; j++)
                {
                    sum[j] = sum[j] / num_of_ave;
                }

                for (int j = 0; j < 2048-Form1.reduction_mode + 1; j++)
                {
                        double local_ave = 0.0;
                        double local_wave_ave = 0.0;
                        for (int s = j; s < j + Form1.reduction_mode; s++)
                        {
                            local_ave = local_ave + sum[s];
                            local_wave_ave = local_wave_ave + Form1.wavelength[s];
                        }

                        local_ave = local_ave / Form1.reduction_mode;
                        local_wave_ave = local_wave_ave / Form1.reduction_mode;

                        if(local_wave_ave>450.0)
                        tran_spec_line.Add(local_wave_ave, local_ave);
                }


                Invoke(invoke_test);
  

            }


        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double changed_integration_time = Form1.current_integration_time;
                try
                {
                    changed_integration_time = Convert.ToDouble(textBox5.Text);
                    
                }
                catch
                {
                    // Illegal input, revert back;
                    MessageBox.Show("Illegal integration time input!");
                }

                if (changed_integration_time>0.01)
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                tChart1.Axes.Bottom.Automatic = true;
            }
            else
            {
                tChart1.Axes.Bottom.Automatic = false;
                tChart1.Axes.Bottom.Minimum = xmin;
                tChart1.Axes.Bottom.Maximum = xmax;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                tChart1.Axes.Left.Automatic = true;
            }
            else
            {
                tChart1.Axes.Left.Automatic = false;
                tChart1.Axes.Left.Minimum = ymin;
                tChart1.Axes.Left.Maximum = ymax;
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double tmp_min_wavelength = xmin;
                try
                {
                    tmp_min_wavelength = Convert.ToDouble(textBox1.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    textBox1.Text = Convert.ToString(xmin);
                    MessageBox.Show("Illegal numerical value input!");
                }

                if (tmp_min_wavelength > 200.0 && tmp_min_wavelength<xmax)
                {
                   
                    xmin = tmp_min_wavelength;
                }
                else
                {
                    // Illegal input, revert back;
                    textBox1.Text = Convert.ToString(xmin);
                    MessageBox.Show("Minimum value of wavelength set too small (should be > 200 nm and < xmax)");
                }
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double tmp_max_wavelength = xmax;
                try
                {
                    tmp_max_wavelength = Convert.ToDouble(textBox2.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    textBox2.Text = Convert.ToString(xmax);
                    MessageBox.Show("Illegal numerical value input!");
                }

                if (tmp_max_wavelength < 950.0 && tmp_max_wavelength>xmin)
                {

                    xmax = tmp_max_wavelength;
                }
                else
                {
                    // Illegal input, revert back;
                    textBox2.Text = Convert.ToString(xmax);
                    MessageBox.Show("Maximum value of wavelength set too large (should be < 950 nm and > xmin)");
                }
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double tmp_min_OD = ymin;
                try
                {
                    tmp_min_OD = Convert.ToDouble(textBox3.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    textBox3.Text = Convert.ToString(ymin);
                    MessageBox.Show("Illegal numerical value input!");
                }

                if (tmp_min_OD<ymax)
                {
                    
                    ymin=tmp_min_OD;
                }
                else
                {
                    // Illegal input, revert back;
                    textBox3.Text = Convert.ToString(ymin);
                    MessageBox.Show("Minimum value of light intensity illegal (should be < ymax)");
                }

            }
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double tmp_max_OD = ymax;
                try
                {
                    tmp_max_OD = Convert.ToDouble(textBox3.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    textBox4.Text = Convert.ToString(ymax);
                    MessageBox.Show("Illegal numerical value input!");
                }

                if (tmp_max_OD > ymin)
                {

                    ymax = tmp_max_OD;
                }
                else
                {
                    // Illegal input, revert back;
                    textBox4.Text = Convert.ToString(ymax);
                    MessageBox.Show("Maximum value of light intenstiy set too large (should be < 100000 and >ymin)");
                }

            }
        }






    }


}
