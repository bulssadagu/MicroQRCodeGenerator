using System;
using System.Collections.Generic;
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
using System.IO;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace MicroQRCodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainControl mainControl;
        private byte[] currentQRCodeBytes;

        public MainWindow()
        {
            InitializeComponent();
            mainControl = new MainControl();
            QRCodeInput.Focus();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string qrCodeData = QRCodeInput.Text;
                
                if (string.IsNullOrWhiteSpace(qrCodeData))
                {
                    MessageBox.Show("QR 코드 번호를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QRCodeInput.Focus();
                    return;
                }

                byte[] qrCodeBytes = mainControl.GenerateQRCode(qrCodeData);
                currentQRCodeBytes = qrCodeBytes;
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(qrCodeBytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                QRCodeImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"QR 코드 생성 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentQRCodeBytes == null || currentQRCodeBytes.Length == 0)
                {
                    MessageBox.Show("먼저 QR 코드를 생성해주세요.", "저장 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG 이미지|*.png",
                    DefaultExt = ".png",
                    FileName = $"QR_{QRCodeInput.Text}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    mainControl.SaveQRCode(currentQRCodeBytes, saveDialog.FileName);
                    MessageBox.Show($"QR 코드가 저장되었습니다.\n{saveDialog.FileName}", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCsvButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "CSV 파일|*.csv|텍스트 파일|*.txt|모든 파일|*.*",
                    DefaultExt = ".csv"
                };

                if (openDialog.ShowDialog() == true)
                {
                    string csvContent = File.ReadAllText(openDialog.FileName, Encoding.UTF8);
                    CsvInput.Text = csvContent;
                    StatusText.Text = "CSV 파일이 로드되었습니다.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 로드 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GenerateBatchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CsvInput.Text))
                {
                    MessageBox.Show("CSV 데이터를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var folderDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "저장 폴더 선택",
                    Filter = "폴더|.",
                    DefaultExt = "."
                };

                if (folderDialog.ShowDialog() != true)
                    return;

                string outputDirectory = System.IO.Path.GetDirectoryName(folderDialog.FileName);
                
                GenerateBatchButton.IsEnabled = false;
                ProgressBar.Value = 0;
                StatusText.Text = "QR 코드를 생성 중입니다...";

                List<(string Code, byte[] QRCode)> qrCodes = await mainControl.GenerateQRCodesFromCsv(CsvInput.Text);
                
                if (qrCodes.Count == 0)
                {
                    MessageBox.Show("생성할 데이터가 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    GenerateBatchButton.IsEnabled = true;
                    return;
                }

                StatusText.Text = $"{qrCodes.Count}개의 QR 코드를 저장 중입니다...";
                await mainControl.SaveMultipleQRCodes(qrCodes, outputDirectory);

                ProgressBar.Value = 100;
                StatusText.Text = $"완료! {qrCodes.Count}개의 QR 코드가 '{outputDirectory}'에 저장되었습니다.";
                MessageBox.Show($"일괄 생성 및 저장 완료!\n저장된 QR 코드: {qrCodes.Count}개\n저장 위치: {outputDirectory}", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                
                GenerateBatchButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"일괄 생성 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                GenerateBatchButton.IsEnabled = true;
            }
        }
    }
}