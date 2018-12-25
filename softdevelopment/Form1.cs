using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace softdevelopment
{

    public partial class Form1 : Form
    {
        private bool drawed;
        private double K;
        private double N0;
        private double A;
        private double B;
        private int days = 7;
        private List<double> deltaDzhMr = new List<double>();
        private List<double> deltaDzhMrSqr = new List<double>();
        private List<double> deltaMMC = new List<double>();
        private List<double> deltaMMCSqr = new List<double>();

        public Form1()
        {
            InitializeComponent();
            for (int i = 1; i < 8; i++)
                dataGrid.Rows.Add(i, 0);
        }


        private void fillDelta(List<double> deltas, List<double> sqrDeltas, int realValue, double funcValue)
        {
            double nevyazkaSqr = (double)realValue - funcValue;
            deltas.Add(nevyazkaSqr);
            nevyazkaSqr = Math.Pow(nevyazkaSqr, 2);
            sqrDeltas.Add(nevyazkaSqr);
        }


        private void button3_Click(object sender, EventArgs e)
        {
            //вывод неувязок и их квадратов модели Джелинского – Моранды
            listBox2.Items.Clear();
            listBox1.Items.Clear();
            deltaDzhMr.ForEach((x) => listBox1.Items.Add(x));
            deltaDzhMrSqr.ForEach((x) => listBox2.Items.Add(x));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //вывод неувязок и их квадратов интерполяции методом наименьших квадратов
            listBox2.Items.Clear();
            listBox1.Items.Clear();
            deltaMMC.ForEach((x) => listBox1.Items.Add(x));
            deltaMMCSqr.ForEach((x) => listBox2.Items.Add(x));
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            double leftPart = 1.0, rightPart = 0.0, eps, currentK;
            try
            {
                eps = double.Parse(textBoxEps.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            double delta, ti, first = 0, second = 0, third = 0, fourth = 0, plusK = double.Parse(textBoxPlusK.Text);
            days = 7;

            for (currentK = 0.0; Math.Abs(leftPart - rightPart) > eps && currentK < 1.0; currentK += plusK)
            {
                leftPart = rightPart = first = second = third = fourth = 0.0;
                for (int i = 0, j = days; i < j; i++)
                {
                    delta = int.Parse(dataGrid[1, i].Value.ToString());
                    ti = int.Parse(dataGrid[0, i].Value.ToString());
                    first += FirstEvaluate(delta, currentK, ti);
                    second += SecondEvaluate(ti, currentK);
                    third += ThirdEvaluate(currentK, ti);
                    fourth += FourthEvaluate(delta, currentK, ti);
                }
                leftPart = fourth;
                rightPart = first * second / third;
            }
            if (currentK >= 1.0)
            {
                MessageBox.Show("Не могу вычислить K");
                return;
            }
            //вывод коэффициента пропорциональности K
            textBoxK.Text = currentK.ToString("0.########");
            K = currentK;
            first = second = third = 0.0;
            for (int i = 0, j = days; i < j; i++)
            {
                delta = int.Parse(dataGrid[1, i].Value.ToString());
                ti = int.Parse(dataGrid[0, i].Value.ToString());
                first += FourthEvaluate(delta, currentK, ti);
                second += SecondEvaluate(ti, currentK);
            }
            second *= currentK;
            third = first / second;
            N0 = third;
            //вывод начального числа ошибок N0
            textBoxN0.Text = third.ToString("0.########");
            first = second = third = fourth = 0;
            for (int i = 0, j = days; i < j; i++)
            {
                delta = int.Parse(dataGrid[1, i].Value.ToString());
                ti = int.Parse(dataGrid[0, i].Value.ToString());
                first += delta * ti;
                second += ti;
                third += delta;
                fourth += ti * ti;
            }
            A = ((days * first) - (second * third)) /
                ((days * fourth) - (second * second));
            B = (third - (A * second)) / days;
            textBoxA.Text = A.ToString();
            textBoxB.Text = B.ToString();
            DrawGraph();
            drawed = true;           
        }

        private void DrawGraph()
        {
            double functionValueDzhMr, functionValueMMC;
            if (drawed)
            {
                foreach (Series s in chart1.Series)
                    s.Points.Clear();
                deltaDzhMr.Clear();
                deltaDzhMrSqr.Clear();
                deltaMMC.Clear();
                deltaMMCSqr.Clear();
                for (int i = 0, j = days; i < j; i++)
                {
                    chart1.Series[0].Points.AddXY(i + 1, int.Parse(dataGrid[1, i].Value.ToString()));
                    chart2.Series[0].Points.AddXY(i + 1, int.Parse(dataGrid[1, i].Value.ToString()));
                }
                for (int x = 0; x <= 10; x++)
                {
                    functionValueDzhMr = N0 * K * Math.Pow(Math.E, -K * x);
                    functionValueMMC = A * x + B;
                    chart1.Series[1].Points.AddXY(x, functionValueDzhMr);
                    if (x > 0)
                    {
                        if (x <= days)
                        {
                            fillDelta(deltaDzhMr, deltaDzhMrSqr, (int)(dataGrid.Rows.Count > x ? int.Parse(dataGrid[1, x].Value.ToString()) : 0), functionValueDzhMr);
                            chart1.Series[3].Points.AddXY(x, deltaDzhMrSqr.Last());
                            chart2.Series[1].Points.AddXY(x, functionValueMMC);
                            fillDelta(deltaMMC, deltaMMCSqr, (int)(dataGrid.Rows.Count > x ? int.Parse(dataGrid[1, x].Value.ToString()) : 0), functionValueMMC);
                            chart2.Series[2].Points.AddXY(x, deltaMMC.Last());
                            chart2.Series[3].Points.AddXY(x, deltaMMCSqr.Last());
                        }
                        chart1.Series[2].Points.AddXY(x, deltaDzhMr.Last());
                    }
                }
                textBox9.Text = deltaDzhMrSqr.Sum().ToString();
                textBoxSumMNK.Text = deltaMMCSqr.Sum().ToString();
            }
        }

        public double FirstEvaluate(double delta, double k, double ti)
        {
            return delta * Math.Exp(-k * ti);
        }

        public double SecondEvaluate(double ti, double k)
        {
            return ti * Math.Exp(2 * (-k) * ti);
        }

        public double ThirdEvaluate(double k, double ti)
        {
            return Math.Pow(Math.Exp(-k * ti), 2);
        }

        public double FourthEvaluate(double delta, double k, double ti)
        {
            return FirstEvaluate(delta, k, ti) * ti;
        }

    }
}
