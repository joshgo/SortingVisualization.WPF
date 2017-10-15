using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BitmapTest
{
    public enum OrdinalValue { Normal, Spiral }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        protected delegate void SetImageDelegate(System.Windows.Controls.Image img, BitmapImage bi);

        private Bitmap _bmp = null;
        private readonly Random _rand = new Random();
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private List<int> _positions = null;
        private readonly Dictionary<int, Color> _colorMap = new Dictionary<int, Color>();
        private readonly Stopwatch _drawTime = new Stopwatch();
        private int _swap = 0;
        private volatile int _swapRateRefresh = 5000;
        private Dictionary<string, string> _resources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<int, int> _sliderToRates = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _ordinalMap = new Dictionary<int, int>();
        public int SwapRefreshRate { get; set; }

        public MainWindow()
        {
            _resources.Add("Starry Night", "pack://application:,,,/Images/starry-night.jpg");
            _resources.Add("Cafe Terrace", "pack://application:,,,/Images/van-gogh-cafe.jpg");
            _resources.Add("Self Portrait", "pack://application:,,,/Images/van-gogh-portrait.jpg");
            _sliderToRates.Add(0, 5000);
            _sliderToRates.Add(1, 20000);
            _sliderToRates.Add(2, 50000);
            _sliderToRates.Add(3, 100000);
            _sliderToRates.Add(4, 250000);
            _sliderToRates.Add(5, 500000);
            _sliderToRates.Add(6, 750000);
            _sliderToRates.Add(7, 1000000);
            _sliderToRates.Add(8, 2500000);
            _sliderToRates.Add(9, 4000000);
            _sliderToRates.Add(10, 10000000);


            InitializeComponent();
            AllocConsole();

            LoadImage("Starry Night");
        }

        public OrdinalValue OrdinalValue { get; set; }

        private void Run(Action vf, string name = "")
        {
            var sw = new Stopwatch();
            sw.Start();
            vf();
            sw.Stop();
            Console.WriteLine("{0} {1}ms", name, sw.ElapsedMilliseconds);
        }

        private void LoadImage(string name)
        {
            _bmp = new Bitmap(Application.GetResourceStream(new Uri(_resources[name])).Stream);
            Run(InitPositions, "InitPositions");
            Run(DrawImage, "DrawImage");
        }
        enum Direction : int { R = 0, D, L, U };

        private void InitPositions()
        {
            var W = _bmp.Width;
            var H = _bmp.Height;
            var SIZE = W * H;
            _positions = new List<int>(SIZE);

            if (_normalMenuItem.IsChecked)
            {
                InitNormalPositions();
            }
            else
            {
                InitSpiralPositions();
            }
            
            for (int i = 0; i < SIZE; i++)
            {
                _positions.Add(i);
                var m = _ordinalMap[i];
                var x = m % W;
                var y = m / W;
                _colorMap[m] = _bmp.GetPixel(x, y);
            }
        }

        private void InitNormalPositions()
        {
            var W = _bmp.Width;
            var H = _bmp.Height;
            var SIZE = W * H;
            _ordinalMap.Clear();
            for(int i = 0; i < SIZE; i++)
            {
                _ordinalMap.Add(i, i);
            }
        }

        private void InitSpiralPositions()
        {
            var W = _bmp.Width;
            var H = _bmp.Height;
            var SIZE = W * H;

            Direction dir = Direction.R;
            var x = 0;
            var y = 0;

            Func<int, int, int> pointFn = new Func<int, int, int>((x1, y1) => y1 * W + x1);
            int TOPSIDE = 0;
            int RIGHTSIDE = W;
            int BOTTOMSIDE = H;
            int LEFTSIDE = -1;
            _ordinalMap.Clear();
            for (int i = 0; i < SIZE; i++)
            {
                var p = pointFn(x, y);
                _ordinalMap.Add(i, p);

                while (true)
                {
                    if (dir == Direction.R && x + 1 < RIGHTSIDE)
                        x++;
                    else if (dir == Direction.D && y + 1 < BOTTOMSIDE)
                        y++;
                    else if (dir == Direction.L && x - 1 > TOPSIDE)
                        x--;
                    else if (dir == Direction.U && y - 1 > LEFTSIDE)
                        y--;
                    else
                    {
                        if (dir == Direction.R) RIGHTSIDE--;
                        else if (dir == Direction.D) BOTTOMSIDE--;
                        else if (dir == Direction.L) LEFTSIDE++;
                        else if (dir == Direction.U) TOPSIDE++;

                        int d = (int)dir;
                        d = ++d % 4;
                        dir = (Direction)d;
                        continue;
                    }

                    break;
                }
            }
        }

        private void RandomizePositions()
        {
            for (int i = 0; i < 1000000; i++)
            {
                var x = _rand.Next(_positions.Count);
                var z = _rand.Next(_positions.Count);

                if (x == z) continue;

                var t = _positions[x];
                _positions[x] = _positions[z];
                _positions[z] = t;
            }
        }

        private void DrawImage()
        {
            var W = _bmp.Width;
            var H = _bmp.Height;

            for (int i = 0; i < _positions.Count; i++)
            {
                var loc = _ordinalMap[i];
                var x = loc % W;
                var y = loc / W;

                var v = _ordinalMap[_positions[i]];
                _bmp.SetPixel(x, y, _colorMap[v]);
            }

            var bmpImage = new BitmapImage();
            var mem = new MemoryStream();
            _bmp.Save(mem, ImageFormat.Bmp);
            mem.Position = 0;

            bmpImage.BeginInit();
            bmpImage.StreamSource = mem;
            bmpImage.EndInit();
            bmpImage.Freeze();

            TheImage.Dispatcher.Invoke(new SetImageDelegate(SetImage), TheImage, bmpImage);
        }

        protected void SetImage(System.Windows.Controls.Image img, BitmapImage bi)
        {
            img.Source = bi;
        }

        private void DisableMenuItems()
        {
            TheImage.Dispatcher.Invoke(new Action(() => {
                try
                {
                    foreach (var i in TheImage.ContextMenu.Items)
                        (i as MenuItem).IsEnabled = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));
        }

        private void EnableMenuItems()
        {
            TheImage.Dispatcher.Invoke(new Action(() =>
            {
                foreach (var i in TheImage.ContextMenu.Items)
                    (i as MenuItem).IsEnabled = true;
            }));
        }

        private void OnSetOrdinalValue(object sender, RoutedEventArgs e)
        {
            var value = (e.Source as MenuItem).CommandParameter.ToString();
            if (value.ToString().ToUpper() == "NORMAL")
            {
                _normalMenuItem.IsChecked = true;
                _spiralMenuItem.IsChecked = false;
            }
            else if (value.ToString().ToUpper() == "SPIRAL")
            {
                _normalMenuItem.IsChecked = false;
                _spiralMenuItem.IsChecked = true;
            }
        }

        private void OnLoadImage(object sender, RoutedEventArgs e)
        {
            var mi = e.Source as MenuItem;
            if (mi == null)
                return;
            LoadImage(mi.CommandParameter.ToString());
        }

        private void OnResetClick(object sendor, RoutedEventArgs e)
        {
            for (int i = 0; i < _positions.Count; i++)
                _positions[i] = i;

            DrawImage();
        }

        private void OnRandomizeClick(object sender, RoutedEventArgs e)
        {
            RandomizePositions();
            DrawImage();
        }

        #region BUBBLESORT
        private void OnRunBubbleSort(object sender, RoutedEventArgs e)
        {
            var action = new Action(() =>
            {
                for (int i = 0; i < _positions.Count; i++)
                {
                    for (int j = 0; j < _positions.Count - 1; j++)
                    {
                        if (_positions[j] > _positions[j + 1])
                        {
                            int tmp = _positions[j + 1];
                            _positions[j + 1] = _positions[j];
                            _positions[j] = tmp;
                            _swap++;
                        }

                        if (_swap >= _swapRateRefresh)
                        {
                            DrawImage();
                            _swap = 0;
                        }
                    }
                }
            });

            var sortWorker = new BackgroundWorker();
            sortWorker.DoWork += delegate (object s1, DoWorkEventArgs e1)
            {
                DisableMenuItems();
                action();
                DrawImage();
                EnableMenuItems();
            };

            sortWorker.RunWorkerAsync();
        }
        #endregion

        #region QUICKSORT
        private void OnRunQuickSort(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Running quicksort w/ rate: " + _swapRateRefresh);

            var sortWorker = new BackgroundWorker();
            sortWorker.DoWork += delegate (object s1, DoWorkEventArgs e1)
            {
                DisableMenuItems();
                Quick_Sort(_positions, 0, _positions.Count - 1);
                DrawImage();
                EnableMenuItems();
            };

            sortWorker.RunWorkerAsync();
        }

        private void Quick_Sort(List<int> arr, int left, int right)
        {
            if (left < right)
            {
                int pivot = Partition(arr, left, right);

                if (pivot > 1)
                {
                    Quick_Sort(arr, left, pivot - 1);
                }
                if (pivot + 1 < right)
                {
                    Quick_Sort(arr, pivot + 1, right);
                }
            }
        }

        private int Partition(List<int> arr, int left, int right)
        {
            int pivot = arr[left];
            while (true)
            {
                while (arr[left] < pivot)
                {
                    left++;
                }

                while (arr[right] > pivot)
                {
                    right--;
                }

                if (left < right)
                {
                    if (arr[left] == arr[right]) return right;

                    int temp = arr[left];
                    arr[left] = arr[right];
                    arr[right] = temp;
                    _swap++;
                }
                else
                {
                    return right;
                }

                if (_swap >= _swapRateRefresh)
                {
                    DrawImage();
                    _swap = 0;
                }
            }

        }
        #endregion

        #region HEAPSORT
        private void OnHeapSort(object sender, RoutedEventArgs e)
        {
            var sortWorker = new BackgroundWorker();
            sortWorker.DoWork += delegate (object s1, DoWorkEventArgs e1)
            {
                DisableMenuItems();
                HeapSort(_positions);
                DrawImage();
                EnableMenuItems();
            };

            sortWorker.RunWorkerAsync();
        }

        public void HeapSort(List<int> input)
        {
            //Build-Max-Heap
            int heapSize = input.Count;
            for (int p = (heapSize - 1) / 2; p >= 0; p--)
                MaxHeapify(input, heapSize, p);

            for (int i = input.Count - 1; i > 0; i--)
            {
                //Swap
                int temp = input[i];
                input[i] = input[0];
                input[0] = temp;
                _swap++;

                if(_swap >= _swapRateRefresh)
                {
                    DrawImage();
                    _swap = 0;
                }

                heapSize--;
                MaxHeapify(input, heapSize, 0);
            }
        }

        private void MaxHeapify(List<int> input, int heapSize, int index)
        {
            int left = (index + 1) * 2 - 1;
            int right = (index + 1) * 2;
            int largest = 0;

            if (left < heapSize && input[left] > input[index])
                largest = left;
            else
                largest = index;

            if (right < heapSize && input[right] > input[largest])
                largest = right;

            if (largest != index)
            {
                int temp = input[index];
                input[index] = input[largest];
                input[largest] = temp;
                _swap++;

                if (_swap >= _swapRateRefresh)
                {
                    DrawImage();
                    _swap = 0;
                }

                MaxHeapify(input, heapSize, largest);
            }
        }

        #endregion
        
        #region INSERTION SORT
        private void OnInsertionSort(object sender, RoutedEventArgs e)
        {
            var sortWorker = new BackgroundWorker();
            sortWorker.DoWork += delegate (object s1, DoWorkEventArgs e1)
            {
                DisableMenuItems();
                InsertionSort(_positions);
                DrawImage();
                EnableMenuItems();
            };
            sortWorker.RunWorkerAsync();
        }

        public void InsertionSort(List<int> inputArray)
        {
            for (int i = 0; i < inputArray.Count - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    if (inputArray[j - 1] > inputArray[j])
                    {
                        int temp = inputArray[j - 1];
                        inputArray[j - 1] = inputArray[j];
                        inputArray[j] = temp;
                        _swap++;

                        if (_swap >= _swapRateRefresh)
                        {
                            _swap = 0;
                            DrawImage();
                        }
                    }
                }
            }
        }
        #endregion

        private void RefreshRate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void RefreshRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // "5000,20000,50000,100000,250000,500000,750000,1000000,2500000,4000000"
            this.Title = "Slider value : " + _sliderToRates[(int)RefreshRate.Value];

            try
            {
                Console.WriteLine("RefreshRate: " + RefreshRate.Value);
                _swapRateRefresh = _sliderToRates[(int)RefreshRate.Value];
            }catch(Exception e1)
            {
                Console.WriteLine(e1);
            }
        }
    }
}
