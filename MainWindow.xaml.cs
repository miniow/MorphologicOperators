using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MorphologicOperators
{
    public class MorphologicalProcessor
    {
        public byte[,] Dilation(byte[,] image, bool[,] structuringElement)
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

        public byte[,] Erosion(byte[,] image, bool[,] structuringElement)
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

        public byte[,] Opening(byte[,] image, bool[,] structuringElement)
        {
            byte[,] eroded = Erosion(image, structuringElement);
            return Dilation(eroded, structuringElement);
        }

        public byte[,] Closing(byte[,] image, bool[,] structuringElement)
        {
            byte[,] dilated = Dilation(image, structuringElement);
            return Erosion(dilated, structuringElement);
        }

        public byte[,] HitOrMiss(byte[,] image, bool[,] seObject, bool[,] seBackground)
        {
            byte[,] erodedObject = Erosion(image, seObject);
            byte[,] complementImage = Complement(image);
            byte[,] erodedBackground = Erosion(complementImage, seBackground);

            int width = image.GetLength(0);
            int height = image.GetLength(1);
            byte[,] output = new byte[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[x, y] = (byte)((erodedObject[x, y] > 0 && erodedBackground[x, y] > 0) ? 255 : 0);
                }
            }

            return output;
        }

        private byte[,] Complement(byte[,] image)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            byte[,] complement = new byte[width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    complement[x, y] = (byte)(255 - image[x, y]);

            return complement;
        }
    }

    public partial class MainWindow : Window
    {
        private byte[,] originalImage;
        private byte[,] currentImage;
        private bool[,] structuringElement;
        private bool[,] backgroundStructuringElement;
        private readonly MorphologicalProcessor processor;

        private Stack<byte[,]> undoStack = new Stack<byte[,]>();
        private Stack<byte[,]> redoStack = new Stack<byte[,]>();

        public MainWindow()
        {
            InitializeComponent();
            processor = new MorphologicalProcessor();
            UpdateUndoRedoButtons();
        }

        private void LoadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Pliki obrazów (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    ImageControl.Source = bitmap;
                    originalImage = ConvertToGrayscale(bitmap);
                    currentImage = (byte[,])originalImage.Clone();
                    undoStack.Clear();
                    redoStack.Clear();
                    UpdateUndoRedoButtons();
                    ProcessedImageControl.Source = null;
                    StatusTextBlock.Text = "Obraz wczytany pomyślnie.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas ładowania obrazu: {ex.Message}");
                }
            }
        }

        private async void ApplyDilation(object sender, RoutedEventArgs e)
        {
            await ApplyMorphologicalOperationAsync(processor.Dilation, "Dylatacja");
        }

        private async void ApplyErosion(object sender, RoutedEventArgs e)
        {
            await ApplyMorphologicalOperationAsync(processor.Erosion, "Erozja");
        }

        private async void ApplyOpening(object sender, RoutedEventArgs e)
        {
            await ApplyMorphologicalOperationAsync(processor.Opening, "Otwarcie");
        }

        private async void ApplyClosing(object sender, RoutedEventArgs e)
        {
            await ApplyMorphologicalOperationAsync(processor.Closing, "Zamknięcie");
        }


        private void CreateStructuringElement(object sender, RoutedEventArgs e)
        {
            structuringElement = CreateStructuringElement(StructElementWidth, StructElementHeight, StructuringElementGrid, "obiektu");
        }

        private void CreateBackgroundStructuringElement(object sender, RoutedEventArgs e)
        {
            backgroundStructuringElement = CreateStructuringElement(BackgroundStructElementWidth, BackgroundStructElementHeight, BackgroundStructuringElementGrid, "tła");
        }


        private bool[,] CreateStructuringElement(TextBox widthTextBox, TextBox heightTextBox, UniformGrid grid, string elementName)
        {
            const int MaxSize = 20;

            if (!int.TryParse(widthTextBox.Text, out int width) ||
                !int.TryParse(heightTextBox.Text, out int height))
            {
                MessageBox.Show($"Podaj poprawne wymiary elementu strukturyzującego {elementName}.");
                return null;
            }

            if (width <= 0 || height <= 0 || width > MaxSize || height > MaxSize)
            {
                MessageBox.Show($"Wymiary elementu strukturyzującego {elementName} muszą być większe niż 0 i nie przekraczać {MaxSize}.");
                return null;
            }

            grid.Rows = height;
            grid.Columns = width;
            grid.Children.Clear();

            bool[,] elementArray = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentX = x;
                    int currentY = y;

                    CheckBox cb = new CheckBox
                    {
                        Margin = new Thickness(2),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    cb.Checked += (s, ev) => { elementArray[currentX, currentY] = true; };
                    cb.Unchecked += (s, ev) => { elementArray[currentX, currentY] = false; };

                    grid.Children.Add(cb);
                }
            }

            StatusTextBlock.Text = $"Element strukturyzujący {elementName} utworzony.";

            return elementArray;
        }


        private async void ApplyHitOrMiss(object sender, RoutedEventArgs e)
        {
            if (currentImage == null)
            {
                MessageBox.Show("Wczytaj obraz przed zastosowaniem operacji.");
                return;
            }

            if (structuringElement == null)
            {
                MessageBox.Show("Zdefiniuj element strukturyzujący obiektu przed zastosowaniem operacji.");
                return;
            }

            if (backgroundStructuringElement == null)
            {
                MessageBox.Show("Zdefiniuj element strukturyzujący tła przed zastosowaniem operacji.");
                return;
            }

            try
            {
                undoStack.Push((byte[,])currentImage.Clone());
                redoStack.Clear();
                UpdateUndoRedoButtons();

                Cursor = Cursors.Wait;
                StatusTextBlock.Text = $"Wykonywanie operacji: Hit-or-Miss...";

                byte[,] processed = await Task.Run(() => processor.HitOrMiss(currentImage, structuringElement, backgroundStructuringElement));

                currentImage = processed;

                DisplayImage(processed);

                StatusTextBlock.Text = $"Operacja Hit-or-Miss zakończona.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wykonywania operacji Hit-or-Miss: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async Task ApplyMorphologicalOperationAsync(Func<byte[,], bool[,], byte[,]> operation, string operationName)
        {
            if (currentImage == null)
            {
                MessageBox.Show("Wczytaj obraz przed zastosowaniem operacji.");
                return;
            }

            if (structuringElement == null)
            {
                MessageBox.Show("Zdefiniuj element strukturyzujący przed zastosowaniem operacji.");
                return;
            }

            try
            {
                undoStack.Push((byte[,])currentImage.Clone());
                redoStack.Clear();
                UpdateUndoRedoButtons();

                Cursor = Cursors.Wait;
                StatusTextBlock.Text = $"Wykonywanie operacji: {operationName}...";

                byte[,] processed = await Task.Run(() => operation(currentImage, structuringElement));

                currentImage = processed;

                DisplayImage(processed);

                StatusTextBlock.Text = $"Operacja {operationName} zakończona.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wykonywania operacji {operationName}: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void Undo(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push((byte[,])currentImage.Clone());
                currentImage = undoStack.Pop();
                DisplayImage(currentImage);
                UpdateUndoRedoButtons();
                StatusTextBlock.Text = "Cofnięto ostatnią operację.";
            }
        }

        private void Redo(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push((byte[,])currentImage.Clone());
                currentImage = redoStack.Pop();
                DisplayImage(currentImage);
                UpdateUndoRedoButtons();
                StatusTextBlock.Text = "Ponowiono operację.";
            }
        }

        private void UpdateUndoRedoButtons()
        {
            UndoButton.IsEnabled = undoStack.Count > 0;
            RedoButton.IsEnabled = redoStack.Count > 0;
        }

        private void ResetStructuringElements(object sender, RoutedEventArgs e)
        {
            StructuringElementGrid.Children.Clear();
            structuringElement = null;
            StructElementWidth.Clear();
            StructElementHeight.Clear();

            BackgroundStructuringElementGrid.Children.Clear();
            backgroundStructuringElement = null;
            BackgroundStructElementWidth.Clear();
            BackgroundStructElementHeight.Clear();

            MessageBox.Show("Elementy strukturyzujące zostały zresetowane.");
            StatusTextBlock.Text = "Elementy strukturyzujące zresetowane.";
        }

        private byte[,] ConvertToGrayscale(BitmapImage bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            byte[,] gray = new byte[width, height];

            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            byte[] pixelData = new byte[height * stride];
            bitmap.CopyPixels(pixelData, stride, 0);

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int index = rowOffset + x * bytesPerPixel;

                    if (index + 2 >= pixelData.Length)
                    {
                        gray[x, y] = 0;
                        continue;
                    }

                    byte b = pixelData[index];
                    byte g = pixelData[index + 1];
                    byte r = pixelData[index + 2];
                    gray[x, y] = (byte)((r + g + b) / 3);
                }
            }

            return gray;
        }

        private BitmapSource ConvertToBitmapSource(byte[,] image)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            int stride = width;
            byte[] pixelData = new byte[width * height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixelData[y * width + x] = image[x, y];

            return BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null, pixelData, stride);
        }

        private void DisplayImage(byte[,] image)
        {
            BitmapSource bitmap = ConvertToBitmapSource(image);
            ProcessedImageControl.Source = bitmap;
        }

        private void SaveProcessedImage(object sender, RoutedEventArgs e)
        {
            if (ProcessedImageControl.Source == null)
            {
                MessageBox.Show("Nie ma przetworzonego obrazu do zapisania.");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Obraz PNG|*.png|Obraz JPEG|*.jpg|Obraz BMP|*.bmp",
                Title = "Zapisz przetworzony obraz"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                BitmapEncoder encoder;

                switch (System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    default:
                        encoder = new PngBitmapEncoder();
                        break;
                }

                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)ProcessedImageControl.Source));

                try
                {
                    using (var stream = System.IO.File.Create(saveFileDialog.FileName))
                    {
                        encoder.Save(stream);
                    }
                    MessageBox.Show("Obraz został zapisany pomyślnie.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisywania obrazu: {ex.Message}");
                }
            }
        }
    }
}
