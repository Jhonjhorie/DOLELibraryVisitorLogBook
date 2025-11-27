using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace DoleVisitorLogbook.Views
{
    /// <summary>
    /// Interaction logic for QRCodeGeneratorControl.xaml
    /// </summary>
    public partial class QRCodeGeneratorControl : UserControl
    {
        private Bitmap? currentQRCodeBitmap;

        public QRCodeGeneratorControl()
        {
            InitializeComponent();
        }

        private bool ValidateForm()
        {
            bool isValid = true;

            // Reset all error messages
            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;

            // Validate Name
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                txtNameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Gender
            if (cbGender.SelectedItem == null)
            {
                txtGenderError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Client Type
            if (cbClientType.SelectedItem == null)
            {
                txtClientTypeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Office/Institution
            if (string.IsNullOrWhiteSpace(txtOffice.Text))
            {
                txtOfficeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Purpose
            if (string.IsNullOrWhiteSpace(txtPurpose.Text))
            {
                txtPurposeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Please fill in all required fields marked with *",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create QR code data string
                // Format: Name|Gender|ClientType|Office|Purpose
                string qrData = $"{txtName.Text.Trim()}|" +
                               $"{((ComboBoxItem)cbGender.SelectedItem).Content}|" +
                               $"{((ComboBoxItem)cbClientType.SelectedItem).Content}|" +
                               $"{txtOffice.Text.Trim()}|" +
                               $"{txtPurpose.Text.Trim()}";

                // Generate QR Code
                var qrWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = 400,
                        Width = 400,
                        Margin = 2,
                        ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H
                    }
                };

                var pixelData = qrWriter.Write(qrData);

                // Convert to Bitmap
                using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                {
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                        ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(
                            pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    // Store for saving later
                    currentQRCodeBitmap?.Dispose();
                    currentQRCodeBitmap = new Bitmap(bitmap);

                    // Display QR Code
                    imgQRCode.Source = BitmapToImageSource(bitmap);
                }

                // Show QR code and hide placeholder
                placeholderPanel.Visibility = Visibility.Collapsed;
                imgQRCode.Visibility = Visibility.Visible;

                // Show visitor info
                visitorInfoPanel.Visibility = Visibility.Visible;
                txtDisplayName.Text = txtName.Text.Trim();
                txtDisplayInfo.Text = $"{((ComboBoxItem)cbClientType.SelectedItem).Content} • " +
                                     $"{((ComboBoxItem)cbGender.SelectedItem).Content}\n" +
                                     $"{txtOffice.Text.Trim()}";

                // Enable action buttons
                btnSave.IsEnabled = true;
                btnPrint.IsEnabled = true;

                MessageBox.Show("QR Code generated successfully!\n\nYou can now save or print the QR code.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating QR code: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all fields and the QR code?",
                "Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            // Clear form fields
            txtName.Clear();
            txtOffice.Clear();
            txtPurpose.Clear();
            cbGender.SelectedIndex = -1;
            cbClientType.SelectedIndex = -1;

            // Hide error messages
            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;

            // Clear QR code display
            imgQRCode.Source = null;
            imgQRCode.Visibility = Visibility.Collapsed;
            placeholderPanel.Visibility = Visibility.Visible;
            visitorInfoPanel.Visibility = Visibility.Collapsed;

            // Disable action buttons
            btnSave.IsEnabled = false;
            btnPrint.IsEnabled = false;

            // Dispose current QR code bitmap
            currentQRCodeBitmap?.Dispose();
            currentQRCodeBitmap = null;

            txtName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentQRCodeBitmap == null)
            {
                MessageBox.Show("No QR code to save. Please generate a QR code first.",
                    "No QR Code",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                    Title = "Save QR Code",
                    FileName = $"QRCode_{txtName.Text.Trim().Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ImageFormat format = ImageFormat.Png;

                    switch (Path.GetExtension(saveDialog.FileName).ToLower())
                    {
                        case ".jpg":
                        case ".jpeg":
                            format = ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = ImageFormat.Bmp;
                            break;
                        default:
                            format = ImageFormat.Png;
                            break;
                    }

                    currentQRCodeBitmap.Save(saveDialog.FileName, format);

                    MessageBox.Show($"QR Code saved successfully to:\n{saveDialog.FileName}",
                        "Save Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving QR code: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (currentQRCodeBitmap == null)
            {
                MessageBox.Show("No QR code to print. Please generate a QR code first.",
                    "No QR Code",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Create a visual for printing
                    var printVisual = CreatePrintVisual();
                    printDialog.PrintVisual(printVisual, $"QR Code - {txtName.Text.Trim()}");

                    MessageBox.Show("QR Code sent to printer successfully!",
                        "Print Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing QR code: {ex.Message}",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private System.Windows.Controls.Border CreatePrintVisual()
        {
            // Create a visual element for printing with visitor info
            var printBorder = new System.Windows.Controls.Border
            {
                Background = System.Windows.Media.Brushes.White,
                Padding = new Thickness(40, 40, 40, 40),
                Width = 600,
                Height = 700
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Title
            var titleText = new TextBlock
            {
                Text = "DOLE Library Visitor QR Code",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(titleText);

            // QR Code Image
            if (imgQRCode.Source != null)
            {
                var qrImage = new System.Windows.Controls.Image
                {
                    Source = imgQRCode.Source,
                    Width = 400,
                    Height = 400,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                stackPanel.Children.Add(qrImage);
            }

            // Visitor Information
            var infoPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            AddInfoText(infoPanel, "Name:", txtName.Text);
            AddInfoText(infoPanel, "Gender:", ((ComboBoxItem)cbGender.SelectedItem).Content.ToString());
            AddInfoText(infoPanel, "Type:", ((ComboBoxItem)cbClientType.SelectedItem).Content.ToString());
            AddInfoText(infoPanel, "Office:", txtOffice.Text);
            AddInfoText(infoPanel, "Purpose:", txtPurpose.Text);

            stackPanel.Children.Add(infoPanel);

            // Generated date
            var dateText = new TextBlock
            {
                Text = $"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            stackPanel.Children.Add(dateText);

            printBorder.Child = stackPanel;
            printBorder.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            printBorder.Arrange(new Rect(0, 0, printBorder.DesiredSize.Width, printBorder.DesiredSize.Height));

            return printBorder;
        }

        private void AddInfoText(StackPanel panel, string label, string value)
        {
            var textBlock = new TextBlock
            {
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            textBlock.Inlines.Add(new System.Windows.Documents.Run(label)
            {
                FontWeight = FontWeights.Bold
            });
            textBlock.Inlines.Add(new System.Windows.Documents.Run($" {value}"));

            panel.Children.Add(textBlock);
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }
}