using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MorphologicOperators
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    

    public partial class MainWindow : Window
    {
        private byte[,] originalImage;
        private bool[,] structuringElement;


        public MainWindow()
        {
            InitializeComponent();
        }
        private void LoadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                ImageControl.Source = bitmap; // ImageControl to kontrolka Image w XAML
            }
        }
        private void LoadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                ImageControl.Source = bitmap;
                originalImage = ConvertToGrayscale(bitmap);
            }
        }

        private void CreateStructuringElement(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(StructElementWidth.Text, out int width) ||
                !int.TryParse(StructElementHeight.Text, out int height))
            {
                MessageBox.Show("Podaj poprawne wymiary elementu strukturyzującego.");
                return;
            }

            // Upewnij się, że szerokość i wysokość są większe niż 0
            if (width <= 0 || height <= 0)
            {
                MessageBox.Show("Wymiary elementu strukturyzującego muszą być większe niż 0.");
                return;
            }

            StructuringElementGrid.Rows = height;
            StructuringElementGrid.Columns = width;
            StructuringElementGrid.Children.Clear();

            structuringElement = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Tworzenie lokalnych kopii x i y
                    int currentX = x;
                    int currentY = y;

                    CheckBox cb = new CheckBox();
                    cb.Margin = new Thickness(2);

                    cb.Checked += (s, ev) => { structuringElement[currentX, currentY] = true; };
                    cb.Unchecked += (s, ev) => { structuringElement[currentX, currentY] = false; };

                    StructuringElementGrid.Children.Add(cb);
                }
            }
        }


        private void ApplyDilation(object sender, RoutedEventArgs e)
        {
            if (originalImage == null || structuringElement == null)
            {
                MessageBox.Show("Wczytaj obraz i zdefiniuj element strukturyzujący.");
                return;
            }

            byte[,] dilated = Dilation(originalImage, structuringElement);
            DisplayImage(dilated);
        }

        private void ApplyErosion(object sender, RoutedEventArgs e)
        {
            if (originalImage == null || structuringElement == null)
            {
                MessageBox.Show("Wczytaj obraz i zdefiniuj element strukturyzujący.");
                return;
            }

            byte[,] eroded = Erosion(originalImage, structuringElement);
            DisplayImage(eroded);
        }

        private void ApplyOpening(object sender, RoutedEventArgs e)
        {
            if (originalImage == null || structuringElement == null)
            {
                MessageBox.Show("Wczytaj obraz i zdefiniuj element strukturyzujący.");
                return;
            }

            byte[,] opened = Opening(originalImage, structuringElement);
            DisplayImage(opened);
        }

        private void ApplyClosing(object sender, RoutedEventArgs e)
        {
            if (originalImage == null || structuringElement == null)
            {
                MessageBox.Show("Wczytaj obraz i zdefiniuj element strukturyzujący.");
                return;
            }

            byte[,] closed = Closing(originalImage, structuringElement);
            DisplayImage(closed);
        }

        private void ApplyHitOrMiss(object sender, RoutedEventArgs e)
        {
            if (originalImage == null || structuringElement == null)
            {
                MessageBox.Show("Wczytaj obraz i zdefiniuj element strukturyzujący.");
                return;
            }

            byte[,] hitOrMiss = HitOrMiss(originalImage, structuringElement);
            DisplayImage(hitOrMiss);
        }
        private byte[,] ConvertToGrayscale(BitmapImage bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            byte[,] gray = new byte[width, height];

            // Inicjalizacja pikseli
            int stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);
            byte[] pixelData = new byte[height * stride];
            bitmap.CopyPixels(pixelData, stride, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4; // Zakładamy format BGRA
                    byte b = pixelData[index];
                    byte g = pixelData[index + 1];
                    byte r = pixelData[index + 2];
                    gray[x, y] = (byte)((r + g + b) / 3);
                }
            }

            return gray;
        }

        private byte[,] Dilation(byte[,] image, bool[,] structuringElement)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            int seWidth = structuringElement.GetLength(0);
            int seHeight = structuringElement.GetLength(1);
            int seCenterX = seWidth / 2;
            int seCenterY = seHeight / 2;

            byte[,] output = new byte[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool match = false;
                    for (int j = 0; j < seHeight; j++)
                    {
                        for (int i = 0; i < seWidth; i++)
                        {
                            int xi = x + i - seCenterX;
                            int yj = y + j - seCenterY;

                            if (xi >= 0 && xi < width && yj >= 0 && yj < height)
                            {
                                if (structuringElement[i, j] && image[xi, yj] > 128)
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                        if (match) break;
                    }
                    output[x, y] = match ? (byte)255 : (byte)0;
                }
            }

            return output;
        }

        private byte[,] Erosion(byte[,] image, bool[,] structuringElement)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            int seWidth = structuringElement.GetLength(0);
            int seHeight = structuringElement.GetLength(1);
            int seCenterX = seWidth / 2;
            int seCenterY = seHeight / 2;

            byte[,] output = new byte[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool match = true;
                    for (int j = 0; j < seHeight; j++)
                    {
                        for (int i = 0; i < seWidth; i++)
                        {
                            int xi = x + i - seCenterX;
                            int yj = y + j - seCenterY;

                            if (structuringElement[i, j])
                            {
                                if (xi < 0 || xi >= width || yj < 0 || yj >= height || image[xi, yj] < 128)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (!match) break;
                    }
                    output[x, y] = match ? (byte)255 : (byte)0;
                }
            }

            return output;
        }
        private byte[,] Opening(byte[,] image, bool[,] structuringElement)
        {
            byte[,] eroded = Erosion(image, structuringElement);
            byte[,] opened = Dilation(eroded, structuringElement);
            return opened;
        }
        private byte[,] Closing(byte[,] image, bool[,] structuringElement)
        {
            byte[,] dilated = Dilation(image, structuringElement);
            byte[,] closed = Erosion(dilated, structuringElement);
            return closed;
        }
        private byte[,] HitOrMiss(byte[,] image, bool[,] structuringElement)
        {
            // Implementacja hit-or-miss może być bardziej złożona.
            // Poniżej prosty przykład detekcji miejsca, gdzie element strukturyzujący pasuje dokładnie.

            int width = image.GetLength(0);
            int height = image.GetLength(1);
            int seWidth = structuringElement.GetLength(0);
            int seHeight = structuringElement.GetLength(1);
            int seCenterX = seWidth / 2;
            int seCenterY = seHeight / 2;

            byte[,] output = new byte[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool match = true;
                    for (int j = 0; j < seHeight; j++)
                    {
                        for (int i = 0; i < seWidth; i++)
                        {
                            int xi = x + i - seCenterX;
                            int yj = y + j - seCenterY;

                            if (structuringElement[i, j])
                            {
                                if (xi < 0 || xi >= width || yj < 0 || yj >= height || image[xi, yj] < 128)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (!match) break;
                    }
                    output[x, y] = match ? (byte)255 : (byte)0;
                }
            }

            return output;
        }
        private BitmapSource ConvertToBitmapSource(byte[,] image)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            int stride = width;
            byte[] pixelData = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixelData[y * width + x] = image[x, y];
                }
            }

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelData, stride);
        }

        private void DisplayImage(byte[,] processedImage)
        {
            BitmapSource bitmap = ConvertToBitmapSource(processedImage);
            ProcessedImageControl.Source = bitmap; // ProcessedImageControl to inna kontrolka Image w XAML
        }

    }


}