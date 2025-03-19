using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace animated_background_tool
{
    public partial class MainWindow : Window
    {
        private readonly List<Particle> particles = new List<Particle>();
        private readonly Random random = new Random();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private string? originalImagePath; // Caminho da imagem carregada
        private BitmapImage? processedImage; // Imagem processada

        public MainWindow()
        {
            InitializeComponent();
            InitializeParticles(); // Inicializa o sistema de partículas
            StartAnimation(); // Inicia a animação das partículas
            AllowDrop = true; // Habilita o drag-and-drop
        }

        // Inicializa as partículas animadas
        private void InitializeParticles()
        {
            for (int i = 0; i < 100; i++) // Ajuste o número de partículas, se necessário
            {
                var particle = new Particle
                {
                    X = random.NextDouble() * 800, // Largura inicial
                    Y = random.NextDouble() * 600, // Altura inicial
                    Radius = random.Next(2, 5), // Tamanho da partícula
                    Color = new SolidColorBrush(Color.FromRgb(
                        (byte)random.Next(100, 255),
                        (byte)random.Next(100, 255),
                        (byte)random.Next(100, 255))), // Cores aleatórias
                    VelocityX = random.NextDouble() * 2 - 1, // Velocidade X
                    VelocityY = random.NextDouble() * 2 - 1 // Velocidade Y
                };
                particles.Add(particle);
            }
        }

        // Inicia a animação
        private void StartAnimation()
        {
            timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            timer.Tick += (s, e) => UpdateParticles();
            timer.Start();
        }

        // Atualiza as partículas
        private void UpdateParticles()
        {
            ParticleCanvas.Children.Clear(); // Limpa o canvas

            foreach (var particle in particles)
            {
                particle.X += particle.VelocityX;
                particle.Y += particle.VelocityY;

                // Verifica colisão com bordas e inverte a direção
                if (particle.X < 0 || particle.X > ParticleCanvas.Width)
                    particle.VelocityX *= -1;
                if (particle.Y < 0 || particle.Y > ParticleCanvas.Height)
                    particle.VelocityY *= -1;

                // Desenha a partícula no canvas
                var ellipse = new Ellipse
                {
                    Width = particle.Radius * 2,
                    Height = particle.Radius * 2,
                    Fill = particle.Color
                };

                Canvas.SetLeft(ellipse, particle.X - particle.Radius);
                Canvas.SetTop(ellipse, particle.Y - particle.Radius);
                ParticleCanvas.Children.Add(ellipse);
            }
        }

        public class Particle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Radius { get; set; }
            public SolidColorBrush Color { get; set; } = new SolidColorBrush(Colors.White); // Valor padrão
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
        }

        // Evento de Drag-and-Drop para aceitar imagens
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy; // Permite o drop
            }
            else
            {
                e.Effects = DragDropEffects.None; // Proíbe o drop
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    originalImagePath = files[0]; // Caminho do arquivo carregado
                    MessageBox.Show($"Imagem carregada: {System.IO.Path.GetFileName(originalImagePath)}");

                    // Processar a imagem automaticamente
                    ProcessarImagem_Click(null, null);
                }
            }
            else
            {
                MessageBox.Show("Por favor, arraste um arquivo de imagem válido.");
            }
        }

        // Processa a imagem e aplica o efeito negativo
        private void ProcessarImagem_Click(object? sender, RoutedEventArgs? e)
        {
            if (originalImagePath == null)
            {
                MessageBox.Show("Nenhuma imagem carregada. Arraste uma imagem para continuar.");
                return;
            }

            try
            {
                var bitmap = new BitmapImage(new Uri(originalImagePath));
                var processedBitmap = ApplyNegativeEffect(bitmap);

                // Exibe a imagem processada
                ImagePreview.Source = processedBitmap;
                ImagePreview.Visibility = Visibility.Visible;

                // Habilita o botão de download
                processedImage = processedBitmap;
                BaixarImagemButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar a imagem: {ex.Message}");
            }
        }

        // Aplica o efeito negativo
        private BitmapImage ApplyNegativeEffect(BitmapImage originalBitmap)
        {
            var pixelFormat = PixelFormats.Bgr32;
            int stride = (originalBitmap.PixelWidth * pixelFormat.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * originalBitmap.PixelHeight];

            originalBitmap.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(255 - pixels[i]);     // Blue
                pixels[i + 1] = (byte)(255 - pixels[i + 1]); // Green
                pixels[i + 2] = (byte)(255 - pixels[i + 2]); // Red
            }

            var processedBitmap = BitmapSource.Create(
                originalBitmap.PixelWidth,
                originalBitmap.PixelHeight,
                originalBitmap.DpiX,
                originalBitmap.DpiY,
                pixelFormat,
                null,
                pixels,
                stride
            );

            var processedImage = new BitmapImage();
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(processedBitmap));
                encoder.Save(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);
                processedImage.BeginInit();
                processedImage.StreamSource = memoryStream;
                processedImage.CacheOption = BitmapCacheOption.OnLoad;
                processedImage.EndInit();
            }

            return processedImage;
        }

        // Salva a imagem processada
        private void BaixarImagem_Click(object sender, RoutedEventArgs e)
        {
            if (processedImage == null)
            {
                MessageBox.Show("Nenhuma imagem processada para baixar.");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Salvar Imagem Processada"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(processedImage));
                        encoder.Save(fileStream);
                    }
                    MessageBox.Show($"Imagem salva em: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar a imagem: {ex.Message}");
                }
            }
        }
    }
}
