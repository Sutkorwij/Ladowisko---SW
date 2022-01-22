﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Threading;
using Emgu.CV.Util;

namespace Ladowisko
{
    public partial class Form1 : Form
    {
        Size desired_image_size;
        Image<Bgr, byte> image_PB1, image_temp2;
        Image<Gray, byte> image_temp1;
        VideoCapture camera;
        VectorOfVectorOfPoint rectContour = new VectorOfVectorOfPoint();
        VectorOfPoint rectContour_max = new VectorOfPoint();
        Mat rect_mat = new Mat();
        bool delay;
        int delay_counter;
        int prev_x, prev_y;
        int maxidx;


        public Form1()
        {
            InitializeComponent();
            desired_image_size = pictureBox1.Size;
            image_PB1 = new Image<Bgr, byte>(desired_image_size);
            image_temp1 = new Image<Gray, byte>(desired_image_size);
            image_temp2 = new Image<Bgr, byte>(desired_image_size);
            timer1.Enabled = false;
            delay = true;
            delay_counter = 0;
            prev_x = -1;
            prev_y = -1;
            maxidx = -1;

            try
            {
                camera = new VideoCapture();
                camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, desired_image_size.Width);
                camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, desired_image_size.Height);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region GUI
        private void button_Browse_Files_PB1_Click(object sender, EventArgs e)
        {
            textBox_Image_Path_PB1.Text = get_image_path();
        }

        private string get_image_path()
        {
            string ret = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Obrazy|*.jpg;*.jpeg;*.png;*.bmp";
            openFileDialog1.Title = "Wybierz obrazek.";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ret = openFileDialog1.FileName;
            }

            return ret;
        }

        private void button_From_File_PB1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = get_image_bitmap_from_file(textBox_Image_Path_PB1.Text, ref image_PB1);
        }



        private Bitmap get_image_bitmap_from_file(string path, ref Image<Bgr, byte> Data)
        {
            try
            {
                Mat temp = CvInvoke.Imread(path);
                CvInvoke.Resize(temp, temp, desired_image_size);
                Data = temp.ToImage<Bgr, byte>();
                return Data.Bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Podana ścieżka jest nieprawidłowa");
                return null;
            }
        }

        #endregion

        bool movie = false;

        private void Timer1_Tick_1(object sender, EventArgs e)
        {
            Mat temp = camera.QueryFrame();
            CvInvoke.Resize(temp, temp, pictureBox1.Size);
            image_PB1 = temp.ToImage<Bgr, byte>();
            pictureBox1.Image = image_PB1.Bitmap;
            button1.PerformClick();
            button5.PerformClick();
            button2.PerformClick();
            if (delay_counter < 3)
            {
                delay = true;
            }
            else
            {
                delay = false;
            }
        }

        #region program

        private void Kolorki()
        {
            // czyszczenie list
                ListView_Data.Clear();
                listView2.Clear();
            //zmienne pomocnicze
                int blockSize = (int)numericUpDown1.Value;
                int param1 = (int)numericUpDown2.Value;
            //kopiowanie image_PB1 do image_temp2
                image_PB1.CopyTo(image_temp2);
            //konwersja z BGR na Gray
                image_temp1 = image_PB1.Convert<Gray, byte>();
                pictureBox5.Image = image_temp1.Bitmap;
            //progowanie adaptacyjne
                CvInvoke.AdaptiveThreshold(image_temp1, image_temp1, 255, Emgu.CV.CvEnum.AdaptiveThresholdType.GaussianC, Emgu.CV.CvEnum.ThresholdType.Binary, blockSize, param1);
                pictureBox5.Image = image_temp1.Bitmap;
            //odwrocenie kolorow
                image_temp1._Not();
            pictureBox5.Image = image_temp1.Bitmap;
            pictureBox4.Image = image_temp2.Bitmap;
        }

        private void FindRect()
        {
            // zmienne pomocnicze
            int matches = 0;
            int x;
            int y;
            double area_max = 0;
            double area_temporary = 0;
            bool newmax = false;

            //odnajdywanie konturów przy użyciu RetrType.Tree - kontur wewnątrz konturu przypisywany jako kolejna liczba całkowita

            CvInvoke.FindContours(image_temp1, rectContour, rect_mat, Emgu.CV.CvEnum.RetrType.Tree, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            Image<Bgr, byte> image_temp1_bgr = image_temp1.Convert<Bgr, byte>();
            //dla każdego konturu:
            for (int i = 0; i < rectContour.Size; i++)
            {
                //obwód
                double perimeter = CvInvoke.ArcLength(rectContour[i], true);
                VectorOfPoint approx = new VectorOfPoint();
                //aproksymacja
                CvInvoke.ApproxPolyDP(rectContour[i], approx, 0.01 * perimeter, true);
                //pole
                double area = CvInvoke.ContourArea(rectContour[i]);
                //dla każdego konturu w zależności od obecnej ilości dopasowań do wzorca sprawdzamy inne warunki:
                switch (matches)
                {
                    //jeżeli nie ma dopasowań sprawdzamy, czy kontur jest czworobokiem, czy jego pole jest wieksze od 200 pixeli, oraz czy na na obrazie nie znalezliśmy już większego
                    //obiektu, który spełnił wszystkie warunki.
                    case 0:
                        if (approx.Size == 4 && area > 200 && area > area_max)
                        {
                            area_temporary = area;
                            matches++;
                            newmax = true;
                            listView1.Items.Add("One match");
                        }
                        else
                        {
                            matches = 0;
                            newmax = false;
                        }
                        break;
                    //dla jednego dopasowania sprawdzamy czy kontur jest czworobokiem.
                    case 1:
                        if (approx.Size == 4) matches++;
                        else
                        {
                            matches = 0;
                            newmax = false;
                        }
                        listView1.Items.Add("Two matches");
                        break;
                    //dla dwóch dopasowań sprawdzamy, czy kontur po aproksymacji ma więcej niż 4 punkty (jest na przykład 5kątem) - może to być przybliżony okrąg.
                    case 2:
                        if (approx.Size > 4) matches++;
                        else
                        {
                            matches = 0;
                            newmax = false;
                        }
                        listView1.Items.Add("Three matches");
                        break;
                    //dla trzech dopasowań jw.
                    case 3:
                        if (approx.Size > 4) matches++;
                        else
                        {
                            matches = 0;
                            newmax = false;
                        }
                        break;
                    //dla czterech dopasowań sprawdzamy, czy kontur ma więcej niż 4 puntky - może być to przybliżony X.
                    case 4:
                        if (approx.Size > 4 && newmax == true)
                        {
                            maxidx = i;
                            area_max = area_temporary;
                            newmax = false;
                            delay_counter = 0;
                            rectContour_max = rectContour[i - 4];
                            listView1.Items.Add("Match!");
                        }
                        else
                        {
                            matches = 0;
                            newmax = false;
                        }
                        break;
                }
                listView1.Clear();
                listView1.Items.Add("Area = " + area);
            }
            listView1.Items.Add("Area_max = " + area_max + "\n");

            //sprawdzamy czy możemy dla naszego obiektu wyliczyć momenty (jeżeli będzie exception - znaczy to, że nie znaleziono żadnego obiektu)
            try
            {
                var moments = CvInvoke.Moments(rectContour[maxidx - 2]);
                x = (int)(moments.M10 / moments.M00);
                y = (int)(moments.M01 / moments.M00);
                prev_x = (int)(moments.M10 / moments.M00);
                prev_y = (int)(moments.M01 / moments.M00);
                CvInvoke.DrawContours(image_temp1_bgr, rectContour_max, 0, new MCvScalar(0, 0, 255));
                CvInvoke.DrawContours(image_temp2, rectContour_max, 0, new MCvScalar(0, 0, 255));
                CvInvoke.Circle(image_temp1_bgr, new Point(x, y), 2, new MCvScalar(0, 255, 255), 2);
                CvInvoke.Circle(image_temp2, new Point(x, y), 2, new MCvScalar(0, 255, 255), 2);
                ListView_Data.Items.Add("Pozycja względem środka obrazu = " + ((int)desired_image_size.Width / 2 - x) + ", " + ((int)desired_image_size.Height / 2 - y) + "\n");
            }
            //w przypadku gdy nie znaleziono obiektu, sprawdzamy czy w ciągu ostatnich 3 klatek obrazu ten obiekt był wykryty - jeżeli tak to podajemy jego współrzędne.
            catch (Exception ex)
            {
                if (delay == true)
                {
                    if (prev_x != -1 && prev_y != -1)
                    {
                        try
                        {
                            CvInvoke.DrawContours(image_temp1_bgr, rectContour, maxidx - 4, new MCvScalar(0, 0, 255));
                            CvInvoke.DrawContours(image_temp2, rectContour, maxidx - 4, new MCvScalar(0, 0, 255));
                            CvInvoke.Circle(image_temp1_bgr, new Point(prev_x, prev_y), 2, new MCvScalar(0, 255, 255), 2);
                            CvInvoke.Circle(image_temp2, new Point(prev_x, prev_y), 2, new MCvScalar(0, 255, 255), 2);
                            ListView_Data.Items.Add("Pozycja względem środka obrazu = " + ((int)desired_image_size.Width / 2 - prev_x) + ", " + ((int)desired_image_size.Height / 2 - prev_y) + "\n");
                        }
                        catch (Exception ex2)
                        {
                            CvInvoke.PutText(image_temp1_bgr, "Err", new Point(10, 10), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                            CvInvoke.PutText(image_temp2, "Err", new Point(10, 10), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                        }
                    }
                    delay_counter++;
                }
                //jeżeli tego obiektu nie było w ciągu ostatnich 3 klatek - wyświetlamy informację o braku wzorca na obrazie.
                else
                {
                    CvInvoke.PutText(image_temp1_bgr, "No match", new Point(10, 10), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                    CvInvoke.PutText(image_temp2, "No match", new Point(10, 10), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                    ListView_Data.Items.Add("Brak wzorca");
                }
            }

            pictureBox4.Image = image_temp1_bgr.Bitmap;
            pictureBox2.Image = image_temp2.Bitmap;
        }
        #endregion

        private void highPassFilter()
        {
            double[] wsp = new double[] { -1, -1, -1,
                                          -1,  9, -1,
                                          -1, -1, -1 };
            double suma_wsp = 0;

            for (int i = 0; i < 9; i++)
            {
                suma_wsp += wsp[i];
            }

            byte[,,] temp1 = image_temp1.Data;
            byte[,,] temp2 = image_temp1.Data;

            for (int x = 1; x < desired_image_size.Width - 1; x++)
            {
                for (int y = 1; y < desired_image_size.Height - 1; y++)
                {
                    double B = 0;
                    B += wsp[0] * temp1[y - 1, x - 1, 0];
                    B += wsp[1] * temp1[y - 1, x, 0];
                    B += wsp[2] * temp1[y - 1, x + 1, 0];

                    B += wsp[3] * temp1[y, x - 1, 0];
                    B += wsp[4] * temp1[y, x, 0];
                    B += wsp[5] * temp1[y, x + 1, 0];

                    B += wsp[6] * temp1[y + 1, x - 1, 0];
                    B += wsp[7] * temp1[y + 1, x, 0];
                    B += wsp[8] * temp1[y + 1, x + 1, 0];

                    if ((int)suma_wsp != 0)
                    {
                        B /= suma_wsp;
                    }

                    if (B < 0) B = 0;
                    if (B > 255) B = 255;

                    temp2[y, x, 0] = (byte)B;
                }
            }
            image_temp1.Data = temp2;
            pictureBox5.Image = image_temp1.Bitmap;
        }

        private void lowPassFilter()
        {
            double[] wsp = new double[] {1, 8, 1,
                                         4, 6, 4,
                                         1, 8, 1};
            double suma_wsp = 0;

            for (int i = 0; i < 9; i++)
            {
                suma_wsp += wsp[i];
            }

            byte[,,] temp1 = image_temp1.Data;
            byte[,,] temp2 = image_temp1.Data;

            for (int x = 1; x < desired_image_size.Width - 1; x++)
            {
                for (int y = 1; y < desired_image_size.Height - 1; y++)
                {
                    double B = 0;
                    B += wsp[0] * temp1[y - 1, x - 1, 0];
                    B += wsp[1] * temp1[y - 1, x, 0];
                    B += wsp[2] * temp1[y - 1, x + 1, 0];

                    B += wsp[3] * temp1[y, x - 1, 0];
                    B += wsp[4] * temp1[y, x, 0];
                    B += wsp[5] * temp1[y, x + 1, 0];

                    B += wsp[6] * temp1[y + 1, x - 1, 0];
                    B += wsp[7] * temp1[y + 1, x, 0];
                    B += wsp[8] * temp1[y + 1, x + 1, 0];

                    if ((int)suma_wsp != 0)
                    {
                        B /= suma_wsp;
                    }

                    if (B < 0) B = 0;
                    if (B > 255) B = 255;

                    temp2[y, x, 0] = (byte)B;
                }
            }
            image_temp1.Data = temp2;
            pictureBox5.Image = image_temp1.Bitmap;
        }

        #region events

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            listView1.Clear();
            MouseEventArgs me = e as MouseEventArgs;
            byte[,,] temp = image_PB1.Data;
            byte R, G, B;
            B = temp[me.Y, me.X, 0];
            G = temp[me.Y, me.X, 1];
            R = temp[me.Y, me.X, 2];
            listView1.Items.Add("Kolor RGB = " + R.ToString() + ", " + G.ToString() + ", " + B.ToString() + "\n");
            listView1.Items.Add("Pozycja = " + me.X.ToString() + ", " + me.Y.ToString() + "\n");
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = e as MouseEventArgs;
            byte[,,] temp = image_PB1.Data;
            byte R, G, B;
            B = temp[me.Y, me.X, 0];
            G = temp[me.Y, me.X, 1];
            R = temp[me.Y, me.X, 2];
            listView1.Items.Add("Kolor RGB Przetworzony = " + R.ToString() + ", " + G.ToString() + ", " + B.ToString() + "\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Mat temp = new Mat();
            camera.Read(temp);
            CvInvoke.Resize(temp, temp, pictureBox1.Size);
            image_PB1 = temp.ToImage<Bgr, byte>();
            pictureBox1.Image = image_PB1.Bitmap;
        }


        
        private void button3_Click(object sender, EventArgs e)
        {
            movie = !movie;
            timer1.Enabled = movie;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Kolorki();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            lowPassFilter();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            highPassFilter();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            image_temp1.Erode(1);
            pictureBox5.Image = image_temp1.Bitmap;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            image_temp1.Dilate(1);
            pictureBox5.Image = image_temp1.Bitmap;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FindRect();
        }

        #endregion
    }
}