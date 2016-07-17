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
    public partial class Monitor_sample_abs : Form
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

        private double xmin;
        private double xmax;
        private double ymin;
        private double ymax;
        System.Threading.Thread update_abs_spec;
        private volatile bool running_thread;

        private Steema.TeeChart.Styles.FastLine spec_line;

        public Monitor_sample_abs(ref double[] wavelength)
        {
            InitializeComponent();
            xmin = 200.0;       // unit in nm
            xmax = 950.0;      // unit in nm
            ymin = 0.0;   // unit in mOD
            ymax = 60000.0;      // unit in mOD

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
            tChart1.Series.Add(new Steema.TeeChart.Styles.FastLine());



            spec_line = (tChart1.Series[0] as Steema.TeeChart.Styles.FastLine);

            running_thread = true;
            //Create a thread that continuously read spectral data from bwtek and update UI
            update_abs_spec = new System.Threading.Thread(cont_sample_bwtek);
            update_abs_spec.Start();



        }

        private void button1_Click(object sender, EventArgs e)
        {
            running_thread = false;
          //  while(update_abs_spec.IsAlive);
            // clean up
            update_abs_spec.Abort();
            this.Close();
        }

        
        public delegate void helpinvoke();

        public void Update_UI()
        {
            tChart1.Refresh();
        }
        

        private void cont_sample_bwtek()
        {
            int retcode;
            UInt16[] spec_in_mem=new UInt16[2048];

            helpinvoke invoke_test = new helpinvoke(Update_UI);

            while(running_thread)
              {


                  

                retcode = bwtekDataReadUSB(1, spec_in_mem, 0);

                spec_line.Clear();

                for (int i = 0; i < 2048-Form1.reduction_mode+1; i++)
                {
                    double local_ave=0.0;
                    double local_wave_ave = 0.0;
                    for (int j = i; j < i + Form1.reduction_mode; j++)
                    {
                        local_ave = local_ave + spec_in_mem[j];
                        local_wave_ave = local_wave_ave + Form1.wavelength[j];
                    }

                    local_ave = local_ave / Form1.reduction_mode;
                    local_wave_ave = local_wave_ave / Form1.reduction_mode;

                     //   spec_line.Add(Form1.wavelength[i], spec_in_mem[i]);
                    spec_line.Add(local_wave_ave, local_ave);
                }

                // Not sure if we need to add delegate method to refresh UI
                Invoke(invoke_test);
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

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double tmp_min_wavelength=xmin;
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

                if (tmp_min_wavelength >200.0 && tmp_min_wavelength<xmax)
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
                double tmp_min_lightIntensity = ymin;
                try
                {
                    tmp_min_lightIntensity = Convert.ToDouble(textBox3.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    textBox3.Text = Convert.ToString(ymin);
                    MessageBox.Show("Illegal numerical value input!");
                }

                if (tmp_min_lightIntensity > 0.0 && tmp_min_lightIntensity<ymax)
                {
                    
                    ymin = tmp_min_lightIntensity;
                }
                else
                {
                    // Illegal input, revert back;
                    textBox3.Text = Convert.ToString(ymin);
                    MessageBox.Show("Minimum value of light intensity illegal (should be > 0 and < ymax)");
                }
            }
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double tmp_max_lightIntensity = ymax;
                try
                {
                    tmp_max_lightIntensity = Convert.ToDouble(textBox4.Text);

                }
                catch
                {
                    // Illegal input, revert back;
                    textBox4.Text = Convert.ToString(ymax);
                    MessageBox.Show("Illegal numerical value input!");
                }

                if (tmp_max_lightIntensity < 100000 && tmp_max_lightIntensity>ymin)
                {

                    ymax = tmp_max_lightIntensity;
                }
                else
                {
                    // Illegal input, revert back;
                    textBox4.Text = Convert.ToString(ymax);
                    MessageBox.Show("Maximum value of light intenstiy set too large (should be < 100000 and >ymin)");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // scan the delay stage, stop by 1 s for observation
            Form1.Abs_Jog(Form1.mySMC, -500.0);
            Form1.Abs_Jog(Form1.mySMC, 500.0);
            Form1.Abs_Jog(Form1.mySMC, -500.0);
        }

    }
}
