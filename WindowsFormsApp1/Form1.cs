using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Newtonsoft.Json;
using Numpy;


namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        AutoSizeFormClass asc = new AutoSizeFormClass();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            asc.controlAutoSize(this);
            asc.resizeFont(this, chart1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            asc.controllInitializeSize(this);
        }

        public double[] calcGauss(int d, int n, double[] array)
        {
            double calcMean = 0.0;
            double calcVar = 0.0;
            double squareMean = 0.0;
            for (int i = 0; i <= d; i++)
            {
                calcMean += i * (array[i] / (double)n);
            }
            for (int i = 0; i <= d; i++)
            {
                squareMean += Math.Pow(i, 2) * (array[i] / (double)n);
            }
            calcVar = squareMean - Math.Pow(calcMean, 2);
            double[] result = new double[d + 1];
            for (int i = 0; i <= d; i++)
            {
                result[i] = (1 / (Math.Sqrt(2 * Math.PI) * Math.Sqrt(calcVar))) *
                    Math.Exp((Math.Pow(i - calcMean, 2) / (2 * calcVar)) * (-1));
            }

            return result;
        }

        private void Random_Parameter(object sender, EventArgs e)
        {
            var str_times = np.random.randint(1000).ToString();
            var str_rounds = np.random.randint(10000).ToString();
            var str_threshold1 = (np.random.randint(1000) / 1000).ToString();
            var str_threshold2 = (np.random.randint(1000) / 1000).ToString();

            textBox1.Text = str_times;
            textBox2.Text = str_rounds;
            textBox3.Text = str_threshold1;
            textBox4.Text = str_threshold2;
        }

        private void Run_and_Plot(object sender, EventArgs e)
        {
            int rounds = int.Parse(textBox1.Text);  //N
            int times = int.Parse(textBox2.Text);   //D
            double threshold1 = double.Parse(textBox3.Text);
            double threshold2 = double.Parse(textBox4.Text);

            var coin_toss1 = np.random.rand(rounds, times);
            var coin_toss2 = np.random.rand(rounds, times);

            coin_toss1[coin_toss1 >= threshold1] = (NDarray)1;
            coin_toss1[coin_toss1 < threshold1] = (NDarray)0;
            coin_toss2[coin_toss2 >= threshold2] = (NDarray)1;
            coin_toss2[coin_toss2 < threshold2] = (NDarray)0;

            coin_toss1 = (NDarray)1 - coin_toss1;
            coin_toss2 = (NDarray)1 - coin_toss2;

            var sum_coin_toss1 = np.sum(coin_toss1, new int[] { 1});  //sum_coin_toss1 = 每個round丟出幾次正面
            var sum_coin_toss2 = np.sum(coin_toss2, new int[] { 1});

            chart1.Series.Clear();

            var times_count1 = np.zeros(times + 1);
            var times_count2 = np.zeros(times + 1);

            for (int i = 0; i < rounds; i++) {  //times_count1 = 丟出n次正面的共有幾個rounds
                var a1 = sum_coin_toss1[i];
                var a2 = sum_coin_toss2[i];
                times_count1[(int)a1] = times_count1[(int)a1] + 1;
                times_count2[(int)a2] = times_count2[(int)a2] + 1;
            }

            Series h1 = new Series("Coin1_hist");
            Series h2 = new Series("Coin2_hist");

            chart1.Series.Add(h1);
            chart1.Series.Add(h2);

            var times_count1_c_shape = times_count1.GetData<double>();
            var times_count2_c_shape = times_count2.GetData<double>();
                        
            for (int i = 0; i < times + 1; i++)
            {
                chart1.Series[0].Points.AddXY(i, times_count1_c_shape[i]);  //畫直方圖
                chart1.Series[1].Points.AddXY(i, times_count2_c_shape[i]);
            }

            double[] y0 = new double[times + 1];

            double[] y1 = new double[times + 1];  
            double[] y2 = new double[times + 1];

            y1 = calcGauss(times, rounds, times_count1_c_shape);  //y1 = 高斯曲線的值
            y2 = calcGauss(times, rounds, times_count2_c_shape);

            double scale1 = times_count1_c_shape.Max() / y1.Max();  //scale1 = 比例
            double scale2 = times_count2_c_shape.Max() / y2.Max();
            
            Series g1 = new Series("Coin1_Gauss");
            Series g2 = new Series("Coin2_Gauss");

            g1.Color = Color.SeaGreen;
            g2.Color = Color.Red;
            g1.BorderWidth = 3;
            g2.BorderWidth = 3;

            chart1.Series.Add(g1);
            chart1.Series.Add(g2);

            g1.ChartType = SeriesChartType.Spline;
            g2.ChartType = SeriesChartType.Spline;

            for (int i = 0; i < times + 1; i++)
            {
                chart1.Series[2].Points.AddXY(i, y1[i]*scale1);
                chart1.Series[3].Points.AddXY(i, y2[i]*scale2);
            }
        }

        private void btn_Bayes_Click(object sender, EventArgs e)
        {
            if (!(chart1.Series.Count / 2 <= 1))
            {
                List<Annotation> line = chart1.Annotations.Where(a => a.Name.Contains("Bayes")).ToList();
                foreach (var item in line)
                {
                    chart1.Annotations.Remove(item);
                }
                findLine();
            }
        }

        public void findLine()
        {
            List<Series> s_list = new List<Series>();
            for (int i = 0; i < chart1.Series.Count/2; i ++)
            {
                s_list.Add(chart1.Series[i]);
            }

            int count = 0;
            string current = "None";
            for (int i = 0; i < s_list[0].Points.Count; i++)
            {                
                double max = s_list.Select(x => x.Points[i].YValues[0]).Max();
                //Console.WriteLine(max.ToString());
                if (max != 0)
                {
                    Series s = s_list[s_list.Select(x => x.Points[i].YValues[0]).ToList().IndexOf(max)];
                    Console.WriteLine(s.Name);
                    if (current != s.Name)
                    {
                        if (count != 0)
                        {
                            VerticalLineAnnotation VA = new VerticalLineAnnotation();
                            VA.AxisX = chart1.ChartAreas[0].AxisX;
                            VA.IsInfinitive = true;
                            VA.LineColor = Color.Black;
                            VA.LineWidth = 3;
                            VA.LineDashStyle = ChartDashStyle.Dash;
                            VA.X = i;
                            VA.Name = "VA Bayes : " + i;
                            chart1.Annotations.Add(VA);

                            TextAnnotation TA = new TextAnnotation();
                            TA.AxisX = chart1.ChartAreas[0].AxisX;
                            TA.X = i;
                            TA.Y = 0;
                            TA.Name = "TA Bayes : " + i;
                            TA.Text = i + "";
                            if (asc.currentSize != 0)
                            {
                                TA.Font = new Font(TA.Font.Name, asc.currentSize);
                            }
                            chart1.Annotations.Add(TA);
                        }
                        current = s.Name;
                        count++;
                    }
                }
            }
        }

        private void btn_kmeans_Click(object sender, EventArgs e)
        {
            MessageBox.Show("別按了啦我還沒做這個功能", "住手");
        }
    }
}
