using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QRCoder;
using System.IO;

namespace MicroQRCodeGenerator
{
    public class MainControl
    {
        #region Private Properties

        #endregion

        #region Initialize/Dispose
        public MainControl() 
        {
            
        }

        #endregion

        #region Public Properties


        #endregion

        #region Public Methods

        public byte[] GenerateQRCode(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("QR 코드 데이터가 비어있습니다.", nameof(data));
            }

            // M2 = requestedVersion: -2
            // ECC M = 현장 라벨용 최소 안정권
            using var qrCodeData = QRCodeGenerator.GenerateMicroQrCode(
                data,
                QRCodeGenerator.ECCLevel.M,
                requestedVersion: -2
            );

            using var qrCode = new PngByteQRCode(qrCodeData);

            // pixelsPerModule은 이미지 해상도용.
            // 실제 인쇄 크기는 라벨 프로그램/프린터 DPI에서 module size로 맞춰야 함.
            byte[] pngBytes = qrCode.GetGraphic(pixelsPerModule: 20);

            return pngBytes;
        }

        public void SaveQRCode(byte[] qrCodeBytes, string filePath)
        {
            if (qrCodeBytes == null || qrCodeBytes.Length == 0)
            {
                throw new ArgumentException("QR 코드 데이터가 비어있습니다.", nameof(qrCodeBytes));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("파일 경로가 비어있습니다.", nameof(filePath));
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, qrCodeBytes);
        }

        public async Task<List<(string Code, byte[] QRCode)>> GenerateQRCodesFromCsv(string csvContent)
        {
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                throw new ArgumentException("CSV 내용이 비어있습니다.", nameof(csvContent));
            }

            var results = new List<(string, byte[])>();
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            await Task.Run(() =>
            {
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    try
                    {
                        var code = trimmedLine.Split(',')[0].Trim();
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            byte[] qrCode = GenerateQRCode(code);
                            results.Add((code, qrCode));
                        }
                    }
                    catch
                    {
                        // Skip invalid lines
                    }
                }
            });

            return results;
        }

        public async Task SaveMultipleQRCodes(List<(string Code, byte[] QRCode)> qrCodes, string outputDirectory)
        {
            if (qrCodes == null || qrCodes.Count == 0)
            {
                throw new ArgumentException("저장할 QR 코드가 없습니다.", nameof(qrCodes));
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("출력 디렉토리가 지정되지 않았습니다.", nameof(outputDirectory));
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await Task.Run(() =>
            {
                foreach (var (code, qrCodeBytes) in qrCodes)
                {
                    string fileName = $"QR_{code}.png";
                    string filePath = Path.Combine(outputDirectory, fileName);
                    SaveQRCode(qrCodeBytes, filePath);
                }
            });
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
