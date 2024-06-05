using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Color = System.Drawing.Color;
using Image = System.Windows.Controls.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
//using Size = System.Windows.Size;


//Маршаллинг функций из DLL, написанной на плюсах
internal static class myDll
{
    [DllImport("KG_Shaders.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void StartTimer(int i);

    [DllImport("KG_Shaders.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateWnd(IntPtr hInstance, IntPtr Parent);

    [DllImport("KG_Shaders.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr MessageBox(int hWnd, String text, String caption, uint type);

    [DllImport("KG_Shaders.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ex_loadModel(string filename);

    [DllImport("KG_Shaders.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern int ex_loadPixShader(IntPtr arr,  int size);
    public static extern int ex_loadPixShader([In, Out] string[] strings, int[] lengths, int size);

    [DllImport("KG_Shaders.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern int ex_loadVertShader(IntPtr arr, int size);
    public static extern int ex_loadVertShader([In, Out] string[] strings, int[] lengths, int size);

    [DllImport("KG_Shaders.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ex_Compile();

    [DllImport("KG_Shaders.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int loadTextute(int chanel, IntPtr texture, int w, int h);

    [DllImport("KG_Shaders.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int deleteTexture(int chanel);

    [DllImport("KG_Shaders.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string getErrStr();

    [DllImport("KG_Shaders.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
    public static extern int errLength();

}


namespace KG_SHADER_forms
{

    public class ControlHost : HwndHost
    {
        IntPtr hWnd;

        public delegate void redy_delegate();

        public redy_delegate ready;

        public ControlHost()
        {

        }

        protected unsafe override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            IntPtr hinstance = Marshal.GetHINSTANCE(typeof(App).Module);
            hWnd = myDll.CreateWnd((IntPtr)hinstance.ToPointer() + 6, (IntPtr)hwndParent.Handle);
            ready?.Invoke();
            return new HandleRef(this, hWnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            //DestroyWindow(hwnd.Handle);
        }


    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //[DllImport("Object.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern int loadModel([MarshalAs(UnmanagedType.LPWStr)] string filename, out ObjFile file);
        [DllImport("Object.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int loadModel(string path, out IntPtr objFile);

        [DllImport("Object.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr drawUVMap(IntPtr objFile);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjTexCord
        {
            public float u, v, w;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjVertex
        {
            public float x, y, z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjFace
        {
            public IntPtr vertices; // Pointer to array of ObjVertex
            public int vertexCount;
            public IntPtr texCoords; // Pointer to array of ObjTexCord
            public int texCoordCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjFile
        {
            public IntPtr faces; // Pointer to array of ObjFace
            public int faceCount;
        }


        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);

        enum TextureType
        {
            None,
            File,
            Resource
        };
        string loadedProject = string.Empty;
        Image[] imgArr = new Image[4];
        string[] textures = new string[4];

        private TextureType[] texTypes =
        {
            TextureType.None, TextureType.None, TextureType.None, TextureType.None
        };

        bool IsModelLoaded = true;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, a) =>
            {
                ControlHost host = new ControlHost();
                grid.Content = host;
                host.Visibility = Visibility.Visible;
                inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;


                host.ready += () =>
                {
                    LoadTextureFromResourse(0, "deftex.jpg");
                    compileShaders();
                };
                //AddCubeToViewport3D();
            };


            imgArr[0] = iTexture0;
            imgArr[1] = iTexture1;
            imgArr[2] = iTexture2;
            imgArr[3] = iTexture3;

            for (int i = 0; i < textures.Length; ++i)
            {
                textures[i] = string.Empty;
            }

        }

        void btnLoadDefTexture0(object sender, RoutedEventArgs e)
        {
            LoadTextureFromResourse(0, "deftex.jpg");
        }


        void loadProject(string s)
        {
            XDocument xdoc = XDocument.Load(s);
            XElement root = xdoc.Element("project");
            tbPix.Text = (string)root.Attribute("pix");
            tbVert.Text = (string)root.Attribute("vert");

            for (int i = 0; i < 4; ++i)
            {
                deleteTexture(i);

                string fname = (string)root.Attribute("t" + Convert.ToString(i));
                if (fname == string.Empty)
                    continue;

                textures[i] = fname;
                TextureType tt = TextureType.None;
                if (root.Attribute("tt" + Convert.ToString(i)) != null)
                {
                    int _tt = Convert.ToInt32(root.Attribute("tt" + Convert.ToString(i)).Value);
                    tt = (TextureType)_tt;
                }
                else
                {
                    tt = TextureType.File;
                }

                texTypes[i] = tt;


                if (tt == TextureType.File)
                {
                    FileInfo fi = new FileInfo(fname);
                    if (!fi.Exists)
                    {
                        string name = fi.Name;
                        string projdir = (new FileInfo(s)).Directory.FullName;
                        fname = projdir + "//" + name;
                    }
                    LoadTextureFromFile(i, fname);
                }

                if (tt == TextureType.Resource)
                    LoadTextureFromResourse(i, fname);
            }

            loadedProject = s;
            this.Title = (new FileInfo(loadedProject).Name) + " - KG Shaders";

        }

        void saveProject(string s)
        {
            XDocument xdoc = new XDocument();
            XElement root = new XElement("project");
            XAttribute vert = new XAttribute("vert", tbVert.Text);
            XAttribute pix = new XAttribute("pix", tbPix.Text);
            root.Add(vert);
            root.Add(pix);
            for (int i = 0; i <= 3; ++i)
            {
                XAttribute t = new XAttribute("t" + Convert.ToString(i), textures[i]);
                XAttribute ttype = new XAttribute("tt" + Convert.ToString(i), (int)texTypes[i]);
                root.Add(ttype);
                root.Add(t);
            }

            xdoc.Add(root);


            xdoc.Save(s);
            this.Title = (new FileInfo(loadedProject).Name) + " - KG Shaders";
        }

        private void compileShaders()
        {
            tbLog.Text = "";
            tbLog.Text += "Compiling... \n";
            {
                string a = tbVert.Text;
                string[] array = a.Split(new string[] { "\r" }, StringSplitOptions.None);

                List<int> strings_lengts = new List<int>();

                for (int i = 0; i < array.Length; ++i)
                {
                    array[i] += "\n";
                    strings_lengts.Add(array[i].Length);
                }

                myDll.ex_loadVertShader(array, strings_lengts.ToArray(), array.Length);
            }
            {
                string a = tbPix.Text;

                string[] array = a.Split(new string[] { "\r" }, StringSplitOptions.None);

                List<int> strings_lengts = new List<int>();

                for (int i = 0; i < array.Length; ++i)
                {
                    array[i] += "\n";
                    strings_lengts.Add(array[i].Length);
                }

                myDll.ex_loadPixShader(array, strings_lengts.ToArray(), array.Length);
            }

            myDll.ex_Compile();

            if (myDll.errLength() > 0)
            {
                string err = myDll.getErrStr();
                tbLog.Text += "ERRORS!!\r";
                tbLog.Text += err;
            }
            else
                tbLog.Text += "Compiled!";
        }
        private void bApplyShaders_Click(object sender, RoutedEventArgs e)
        {
            compileShaders();
        }

        private void bOpenModel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDlg = new Microsoft.Win32.OpenFileDialog();

                if (openDlg.ShowDialog() == true)
                {
                    string s = openDlg.FileName;
                    myDll.ex_loadModel(s);
                    IsModelLoaded = true;
                    LoadAndDrawModel(s);
                }
            }
            catch (Exception ex)
            { MessageBox.Show($"Ошибка: {ex.Message}\n{ex.StackTrace}"); }
        }


        private void LoadTextureFromFile(int chanel, string fileName)
        {
            System.Drawing.Bitmap b = null;
            System.Windows.Media.Imaging.BitmapSource bs = null;

            if (new FileInfo(fileName).Exists == false)
                return;

            b = new System.Drawing.Bitmap(fileName);


            using (MemoryStream memory = new MemoryStream())
            {
                b.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                imgArr[chanel].Source = bitmapImage;
            }

            textures[chanel] = fileName;
            texTypes[chanel] = TextureType.File;


            SentBitmapTo3d(b, chanel);



            // Old texture loading
            /*

            byte[] bb = new byte[b1.Height * b1.Width * 4];

            
            for (int i = 0; i < b.Height; ++i)
            {
                for (int j = 0; j < b.Width; ++j)
                {
                    var c = b1.GetPixel(j, i);
                    int offset = i * b.Width * 4 + j * 4;
                    bb[offset] = c.R;
                    bb[offset + 1] = c.G;
                    bb[offset + 2] = c.B;
                    bb[offset + 3] = c.A;

                }
            }
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(b1.Height * b1.Width * 4);
            Marshal.Copy(bb, 0, unmanagedPointer, b1.Height * b1.Width * 4);
            myDll.loadTextute(chanel, unmanagedPointer, b1.Width, b1.Height);
            Marshal.FreeHGlobal(unmanagedPointer);   
             
             */

        }

        private void LoadTextureFromResourse(int chanel, string resKey)
        {
            var bitmapImage = new BitmapImage(new Uri(@"pack://application:,,,/"
                                                + Assembly.GetExecutingAssembly().GetName().Name
                                                + ";component/"
                                                + resKey, UriKind.Absolute));

            var bitmap = BitmapImage2Bitmap(bitmapImage);
            var hbitmap = bitmap.GetHbitmap();
            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hbitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            imgArr[chanel].Source = bitmapImage;

            DeleteObject(hbitmap);

            textures[chanel] = resKey;
            texTypes[chanel] = TextureType.Resource;
            SentBitmapTo3d(bitmap, chanel);


        }
        //private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        //{
        //    // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

        //    System.Drawing.Bitmap bitmap;
        //    using (MemoryStream outStream = new MemoryStream())
        //    {
        //        BitmapEncoder enc = new BmpBitmapEncoder();
        //        enc.Frames.Add(BitmapFrame.Create(bitmapImage));
        //        enc.Save(outStream);
        //        bitmap = new System.Drawing.Bitmap(outStream);
        //    }
        //    return bitmap;
        //}

        private void SentBitmapTo3d(System.Drawing.Bitmap b, int chanel)
        {
            var b1 = new System.Drawing.Bitmap(b.Width, b.Height, PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(b1))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.CompositingQuality = CompositingQuality.AssumeLinear;
                g.DrawImage(b, 0, 0, b1.Width, b1.Height);
            }

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, b1.Width, b1.Height);
            var bitmapData = b1.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, b1.PixelFormat);
            myDll.loadTextute(chanel, bitmapData.Scan0, b1.Width, b1.Height);
            b1.UnlockBits(bitmapData);
        }

        private void btnTexLoad0(object sender, RoutedEventArgs e)
        {
            loadTexture(0);
        }

        private void btnTexLoad1(object sender, RoutedEventArgs e)
        {
            loadTexture(1);
        }

        private void btnTexLoad2(object sender, RoutedEventArgs e)
        {
            loadTexture(2);
        }

        private void btnTexLoad3(object sender, RoutedEventArgs e)
        {
            loadTexture(3);
        }

        private void btnTexDelete0(object sender, RoutedEventArgs e)
        {
            deleteTexture(0);
        }
        private void btnTexDelete1(object sender, RoutedEventArgs e)
        {
            deleteTexture(1);
        }
        private void btnTexDelete2(object sender, RoutedEventArgs e)
        {
            deleteTexture(2);
        }
        private void btnTexDelete3(object sender, RoutedEventArgs e)
        {
            deleteTexture(3);
        }

        private void deleteTexture(int number)
        {
            imgArr[number].Source = null;
            textures[number] = string.Empty;
            texTypes[number] = TextureType.None;
            myDll.deleteTexture(number);
        }

        private string lastDir = string.Empty;
        private void loadTexture(int number)
        {
            Microsoft.Win32.OpenFileDialog openDlg = new Microsoft.Win32.OpenFileDialog();

            if (lastDir == String.Empty)
            {
                if (loadedProject == string.Empty)
                {
                    openDlg.InitialDirectory =
                        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                else
                {
                    openDlg.InitialDirectory = System.IO.Path.GetDirectoryName(loadedProject);
                }

            }
            else
            {
                openDlg.InitialDirectory = lastDir;
            }
            if (openDlg.ShowDialog() == true)
            {
                string s = openDlg.FileName;
                LoadTextureFromFile(number, s);
                lastDir = System.IO.Path.GetDirectoryName(s);
            }

        }

        private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPalette.Visibility = ColorPalette.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button colorButton)
            {
                var color = ((SolidColorBrush)colorButton.Background).Color;
                inkCanvas.DefaultDrawingAttributes.Color = color;
                ColorPickerButton.Background = colorButton.Background;
                ColorPalette.Visibility = Visibility.Collapsed;
            }
        }

        private void BrushSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(BrushSizeTextBox.Text, out int newSize))
            {
                inkCanvas.DefaultDrawingAttributes.Width = newSize;
                inkCanvas.DefaultDrawingAttributes.Height = newSize;
            }
        }

        private void IncreaseBrushSize_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(BrushSizeTextBox.Text, out int currentSize))
            {
                currentSize++;
                BrushSizeTextBox.Text = currentSize.ToString();
            }
        }

        private void DecreaseBrushSize_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(BrushSizeTextBox.Text, out int currentSize))
            {
                if (currentSize > 1)
                {
                    currentSize--;
                    BrushSizeTextBox.Text = currentSize.ToString();
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveDlg = new Microsoft.Win32.SaveFileDialog();

            if (loadedProject == string.Empty)
            {
                saveDlg.InitialDirectory =
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                saveDlg.InitialDirectory = System.IO.Path.GetDirectoryName(loadedProject);
            }

            saveDlg.FileName = "Shader project";
            saveDlg.DefaultExt = "kgs";
            saveDlg.Filter = "Файлы KG-Shader (*.kgs)|*.kgs";
            if (saveDlg.ShowDialog() != true)
                return;

            loadedProject = saveDlg.FileName;
            saveProject(loadedProject);
        }

        private void bSave_Click(object sender, RoutedEventArgs e)
        {
            if (loadedProject == string.Empty)
                SaveAs_Click(sender, e);
            else
                saveProject(loadedProject);
        }

        private void bLoadProject_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDlg = new Microsoft.Win32.OpenFileDialog();
            openDlg.Filter = "Файлы KG-Shader (*.kgs)|*.kgs";
            if (loadedProject == string.Empty)
            {
                openDlg.InitialDirectory =
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                openDlg.InitialDirectory = System.IO.Path.GetDirectoryName(loadedProject);
            }
            if (openDlg.ShowDialog() == true)
            {
                string s = openDlg.FileName;
                loadProject(s);
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            helpWnd wnd = new helpWnd();
            wnd.ShowDialog();
        }

        //public static void DrawUVMap(string objFile)
        //{
        //    int width = 1024; // Задайте размеры вашей текстуры
        //    int height = 1024;

        //    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        //    using (Graphics g = Graphics.FromImage(bitmap))
        //    {
        //        g.Clear(Color.Black);
        //        g.SmoothingMode = SmoothingMode.AntiAlias;

        //        string[] lines = objFile.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        //        List<PointF> uvPoints = new List<PointF>();

        //        foreach (string line in lines)
        //        {
        //            if (line.StartsWith("vt "))
        //            {
        //                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //                if (parts.Length >= 3 &&
        //                    float.TryParse(parts[1], out float u) &&
        //                    float.TryParse(parts[2], out float v))
        //                {
        //                    uvPoints.Add(new PointF(u * width, (1 - v) * height));
        //                }
        //            }
        //            else if (line.StartsWith("f "))
        //            {
        //                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //                PointF[] facePoints = new PointF[parts.Length - 1];
        //                for (int i = 1; i < parts.Length; i++)
        //                {
        //                    string[] indices = parts[i].Split('/');
        //                    if (indices.Length >= 2 &&
        //                        int.TryParse(indices[1], out int uvIndex) &&
        //                        uvIndex - 1 < uvPoints.Count)
        //                    {
        //                        facePoints[i - 1] = uvPoints[uvIndex - 1];
        //                    }
        //                }
        //                g.DrawPolygon(Pens.White, facePoints);
        //            }
        //        }
        //    }

        //    bitmap.Save("uv_map.png", ImageFormat.Png); // Сохранение для отладки
        //    bitmap.Dispose();
        //}


        private void DrawUVMap(string objFilePath)
        {
            IntPtr objFile = IntPtr.Zero;
            int result = loadModel(objFilePath, out objFile);

            if (result != 0)
            {
                MessageBox.Show("Ошибка загрузки модели");
                return;
            }

            IntPtr bitmapPtr = drawUVMap(objFile);

            if (bitmapPtr == IntPtr.Zero)
            {
                MessageBox.Show("Ошибка создания UV-развертки");
                return;
            }

            // Create Bitmap from pointer
            Bitmap uvBitmap = Bitmap.FromHbitmap(bitmapPtr);

            // Save Bitmap as PNG
            // uvBitmap.Save("uvmap.png", System.Drawing.Imaging.ImageFormat.Png);

            // Dispose Bitmap and free HBITMAP handle

            //IntPtr objFile = IntPtr.Zero;
            //int result = loadModel(objFilePath, out objFile);

            //if (result != 0)
            //{
            //    MessageBox.Show("Ошибка загрузки модели");
            //    return;
            //}

            //IntPtr bitmapPtr = drawUVMap(objFile);

            //if (bitmapPtr == IntPtr.Zero)
            //{
            //    MessageBox.Show("Ошибка создания UV-развертки");
            //    return;
            //}

            //// Create Bitmap from pointer
            //Bitmap uvBitmap = Bitmap.FromHbitmap(bitmapPtr);

            //// Invert colors
            //uvBitmap = InvertColors(uvBitmap);

            //// Set white color as transparent
            uvBitmap.MakeTransparent(Color.White);

            //// Save inverted UV map
            uvBitmap.Save("uv_map.png", ImageFormat.Png);

            //// Display UV map on Image control
            DeleteObject(bitmapPtr);
            OpenImageForDrawing("D:\\3d graphic lab\\Новая Папка\\KG_Shaders-2019-4\\KG_SHADER_forms\\bin\\Debug\\uv_map.png");

        }

        private void OpenImageForDrawing(string imagePath)
        {
            // Load the image from file
            System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image();
            imageControl = drawingImage;
            // Загружаем изображение из файла
            Bitmap bitmap = new System.Drawing.Bitmap(imagePath);
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                imageControl.Source = bitmapImage;
            }

            //bitmap.BeginInit();
            //bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            //bitmap.CacheOption = BitmapCacheOption.OnLoad;
            //bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            //bitmap.EndInit();

            // Устанавливаем источник изображения


            // Настраиваем изображение (например, его размер и растяжение)
            imageControl.Width = inkCanvas.ActualWidth;
            imageControl.Height = inkCanvas.ActualHeight;
            imageControl.Stretch = Stretch.Fill; // Растягиваем изображение, чтобы оно заполнило весь холст

            // Устанавливаем Canvas.Top и Canvas.Left, если нужно (например, для центрирования изображения)
            Canvas.SetTop(imageControl, 0);
            Canvas.SetLeft(imageControl, 0);

            // Добавляем изображение в InkCanvas
            //inkCanvas.Children.Add(imageControl);
        }


        private ImageSource BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }


        private void LoadAndDrawModel(string modelPath)
        {
            //try
            //{
            //    //ObjFile objFile;
            //    //int result = loadModel(modelPath, out objFile);
            //    IntPtr objFilePtr;
            //    int result = loadModel(modelPath, out objFilePtr);
            //    if (result == 0)
            //    {
            //        string objFile = Marshal.PtrToStringAnsi(objFilePtr);
            //        //IntPtr bitmapPointer = IntPtr.Zero;
            //        // Успешно загружена модель
            //        try
            //        {
            //            DrawUVMap(objFile);
            //        }
            //        finally
            //        {
            //            // Освобождаем unmanaged память, если это необходимо
            //            //Marshal.FreeHGlobal(objFilePtr);
            //        }
            //    }
            //    else
            //    {
            //        MessageBox.Show("Ошибка при загрузке модели");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Ошибка в LoadAndDrawModel: {ex.Message}\n{ex.StackTrace}");
            //}
            DrawUVMap(modelPath);
        }


        //private void AddCubeToViewport3D()
        //{
        //    Создание модели куба
        //    if (!IsModelLoaded)
        //    {
        //        MeshGeometry3D mesh = new MeshGeometry3D();

        //        Point3DCollection corners = new Point3DCollection
        //    {
        //        new Point3D(0, 0, 0), // 0
        //        new Point3D(1, 0, 0), // 1
        //        new Point3D(1, 1, 0), // 2
        //        new Point3D(0, 1, 0), // 3
        //        new Point3D(0, 0, 1), // 4
        //        new Point3D(1, 0, 1), // 5
        //        new Point3D(1, 1, 1), // 6
        //        new Point3D(0, 1, 1)  // 7
        //    };
        //        mesh.Positions = corners;

        //        Int32Collection indices = new Int32Collection
        //    {
        //        0, 1, 2, 2, 3, 0, // Front
        //        1, 5, 6, 6, 2, 1, // Right
        //        5, 4, 7, 7, 6, 5, // Back
        //        4, 0, 3, 3, 7, 4, // Left
        //        3, 2, 6, 6, 7, 3, // Top
        //        4, 5, 1, 1, 0, 4  // Bottom
        //    };
        //        mesh.TriangleIndices = indices;

        //        GeometryModel3D cube = new GeometryModel3D
        //        {
        //            Geometry = mesh,
        //            Material = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Red))
        //        };

        //        ModelVisual3D model = new ModelVisual3D
        //        {
        //            Content = cube
        //        };

        //        MyViewport3D.Children.Add(model);
        //    }
        //}

        private string drawingFilePath = "NewCanvas1.jpg";

        private void InkCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            SaveInkCanvasAsImage((InkCanvas)sender);
            // После сохранения изображения обновляем текстурный ресурс 3D модели
            UpdateModelTextureWithDrawing();
            //MessageBox.Show("Ф");
        }

        private void SaveInkCanvasAsImage(InkCanvas inkCanvas)
        {
            int width = (int)inkCanvas.ActualWidth;
            int height = (int)inkCanvas.ActualHeight;
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);

            // Создаем визуализацию для рисования
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                // Заполняем фон белым цветом
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));

                // Рисуем текущее содержимое InkCanvas
                VisualBrush vb = new VisualBrush(inkCanvas);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(width, height)));
            }

            // Сохраняем изображение в файл
            rtb.Render(dv);

            JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
            jpegEncoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = new FileStream(drawingFilePath, FileMode.Create))
            {
                jpegEncoder.Save(fs);
            }

        }

        private void UpdateModelTextureWithDrawing()
        {
            // Загружаем сохраненное изображение из файла
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(drawingFilePath, UriKind.RelativeOrAbsolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            // Преобразуем BitmapImage в Bitmap
            Bitmap bmp = BitmapImage2Bitmap(bitmap);

            // Отправляем изображение в текстурный ресурс 3D модели
            SentBitmapTo3d(bmp, 0);
            //ForceUIUpdate();
        }

        private void ForceUIUpdate()
        {
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

        }

        // Преобразование BitmapImage в Bitmap
        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            Bitmap bitmap;
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(stream);
                bitmap = new Bitmap(stream);
            }
            return bitmap;
        }

        //private void SaveInkCanvasAsImage(InkCanvas inkCanvas)
        //{
        //    RenderTargetBitmap rtb = new RenderTargetBitmap((int)inkCanvas.ActualWidth,
        //                                                    (int)inkCanvas.ActualHeight,
        //                                                    96d, 96d, System.Windows.Media.PixelFormats.Default);
        //    rtb.Render(inkCanvas);

        //    JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
        //    jpegEncoder.Frames.Add(BitmapFrame.Create(rtb));

        //    using (var fs = new FileStream(drawingFilePath, FileMode.Create))
        //    {
        //        jpegEncoder.Save(fs);
        //    }
        //}

        private void bOpenDrawing_Click(object sender, RoutedEventArgs e)
        {
            var drawingTabItem = drawingTab;

            if (File.Exists(drawingFilePath))
            {
                var bitmap = new BitmapImage();
                using (var stream = new FileStream(drawingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                // Устанавливаем изображение как фон для InkCanvas
                ImageBrush brush = new ImageBrush(bitmap);
                inkCanvas.Background = brush;
            }

            drawingTabItem.Visibility = Visibility.Visible;
            drawingTabItem.IsSelected = true;
        }
    }
}
