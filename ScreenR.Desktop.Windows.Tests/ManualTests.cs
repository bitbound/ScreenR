#nullable disable
using Microsoft.Extensions.Logging;
using ScreenR.Shared.Models;
using ScreenR.Desktop.Core.Services;
using ScreenR.Desktop.Windows.Capture;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;

namespace ScreenR.Desktop.Windows.Tests
{
    [TestClass]
    //[Ignore("Manual")]
    public class ManualTests
    {
        private IEnumerable<DisplayInfo> _displays;
        private BitmapUtility _imageHelper;
        private ScreenRecorder _recorder;
        private LoggerFactory _factory;
        private ScreenGrabber _grabber;
        private ILogger<ScreenGrabber> _logger;

        [TestMethod]
        public async Task ScreenRecorderTest()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var display = _displays.First();
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "test.mp4");
            using var fs = new FileStream(savePath, FileMode.Create);

            var recordTask = Task.Run(async () => await _recorder.CaptureVideo(display, 15, fs, token));

            await Task.Delay(3_000);

            cts.Cancel();

            await recordTask;
        }

        [TestMethod]
        public void CaptureAndEncodeSpeedTest()
        {
            var display = _displays.First();
            var iterations = 30;
            var quality = 80;
            var sw = Stopwatch.StartNew();

            SKBitmap currentFrame = new();
            SKBitmap previousFrame = new();

            for (var i = 0; i < iterations; i++)
            {
                previousFrame?.Dispose();
                previousFrame = currentFrame.Copy();
                currentFrame.Dispose();

                currentFrame = _grabber.GetScreenGrab(display.DeviceName).Value;
                var diffArea = _imageHelper.GetDiffArea(currentFrame, previousFrame);
                using var cropped = _imageHelper.CropBitmap(currentFrame, diffArea);
                using var skData = cropped.Encode(SKEncodedImageFormat.Webp, quality);
            }
            sw.Stop();
            Console.WriteLine($"ScreenGrab & WEBP: {GetAverage(sw, iterations)}ms per iteration");

            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                previousFrame.Dispose();
                previousFrame = currentFrame.Copy();
                currentFrame.Dispose();

                currentFrame = _grabber.GetScreenGrab(display.DeviceName).Value;
                var diffArea = _imageHelper.GetDiffArea(currentFrame, previousFrame);
                using var cropped = _imageHelper.CropBitmap(currentFrame, diffArea);
                using var skData = cropped.Encode(SKEncodedImageFormat.Jpeg, quality);
            }
            sw.Stop();
            Console.WriteLine($"ScreenGrab & JPEG: {GetAverage(sw, iterations)}ms per iteration");
        }

        [TestMethod]
        public void CaptureSpeedTest()
        {
            var display = _displays.First();
            var iterations = 30;
            var sw = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                using var bitmap = _grabber.GetScreenGrab(display.DeviceName).Value;
            }

            sw.Stop();

            Console.WriteLine($"ScreenGrab: {GetAverage(sw, iterations)}ms per capture");



            sw.Restart();

            for (var i = 0; i < iterations; i++)
            {
                using var bitmap = _grabber.GetDirectXGrab(display).Value;
            }

            sw.Stop();

            Console.WriteLine($"DirectX: {GetAverage(sw, iterations)}ms per capture");



            sw.Restart();

            for (var i = 0; i < iterations; i++)
            {
                using var bitmap = _grabber.GetBitBltGrab(display).Value;
            }

            sw.Stop();

            Console.WriteLine($"BitBlt: {GetAverage(sw, iterations)}ms per capture");



            sw.Restart();

            for (var i = 0; i < iterations; i++)
            {
                using var bitmap = _grabber.GetWinFormsGrab(display).Value;
            }

            sw.Stop();

            Console.WriteLine($"WinForms: {GetAverage(sw, iterations)}ms per capture");
        }

        [TestMethod]
        public void DiffSpeedTests()
        {
            using var bitmap1 = GetImage("Image1.png");
            using var bitmap2 = GetImage("Image2.png");
            var iterations = 60;


            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                _ = _imageHelper.GetDiffArea(bitmap1, bitmap2);
            }
            sw.Stop();
            Console.WriteLine($"Diff Area: {GetAverage(sw, iterations)}ms per call");


            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                using var imageDiff = _imageHelper.GetImageDiff(bitmap1, bitmap2).Value;
            }
            sw.Stop();
            Console.WriteLine($"Image Diff: {GetAverage(sw, iterations)}ms per call");
        }

        [TestMethod]
        public void EncodeSpeedTest()
        {
            using var skBitmap = GetImage("Image1.png");
            var quality = 75;
            var iterations = 30;

            {
                using var skData = skBitmap.Encode(SKEncodedImageFormat.Jpeg, quality);
                Console.WriteLine($"JPEG size: {skData.Size:N0}");
            }

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                using var skData = skBitmap.Encode(SKEncodedImageFormat.Jpeg, quality);
            }
            sw.Stop();
            Console.WriteLine($"JPEG: {GetAverage(sw, iterations)}ms per encode");


            {
                using var skData = skBitmap.Encode(SKEncodedImageFormat.Png, quality);
                Console.WriteLine($"PNG size: {skData.Size:N0}");
            }
            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                using var skData = skBitmap.Encode(SKEncodedImageFormat.Png, quality);
            }
            sw.Stop();
            Console.WriteLine($"PNG: {GetAverage(sw, iterations)}ms per encode");


            {
                using var skData = skBitmap.Encode(SKEncodedImageFormat.Webp, quality);
                Console.WriteLine($"WEBP size: {skData.Size:N0}");
            }
            sw.Restart();
            for (var i = 0; i < iterations; i++)
            {
                using var skData = skBitmap.Encode(SKEncodedImageFormat.Webp, quality);
            }
            sw.Stop();
            Console.WriteLine($"WEBP: {GetAverage(sw, iterations)}ms per encode");
        }

        [TestMethod]
        public void GetDiffAreaTest()
        {
            using var bitmap1 = GetImage("Image1.png");
            using var bitmap2 = GetImage("Image2.png");

            var diffArea = _imageHelper.GetDiffArea(bitmap1, bitmap2);
            using var cropped = _imageHelper.CropBitmap(bitmap2, diffArea);

            SaveFile(cropped, "Test.webp");
        }

        [TestMethod]
        public void GetImageDiffTest()
        {
            using var bitmap1 = GetImage("Image1.png");
            using var bitmap2 = GetImage("Image2.png");

            var diff = _imageHelper.GetImageDiff(bitmap1, bitmap2);

            SaveFile(diff.Value, "Test.webp");
        }

        [TestInitialize]
        public void Init()
        {
            _factory = new LoggerFactory();
            _logger = _factory.CreateLogger<ScreenGrabber>();
            _grabber = new ScreenGrabber(_logger);
            _displays = _grabber.GetDisplays();
            _imageHelper = new BitmapUtility(_factory.CreateLogger<BitmapUtility>());
            _recorder = new ScreenRecorder(_grabber);
        }

        [TestMethod]
        public void SaveFileTest()
        {
            var result = _grabber.GetScreenGrab(_displays.First().DeviceName);

            if (result.Value is null)
            {
                throw new ArgumentNullException(nameof(result.Value));
            }

            SaveFile(result.Value, "Test.jpg", SKEncodedImageFormat.Jpeg);
        }

        private static SKBitmap GetImage(string frameFileName)
        {
            using var mrs = Assembly.GetExecutingAssembly().GetManifestResourceStream($"ScreenR.Desktop.Windows.Tests.Resources.{frameFileName}");
            using var resourceImage = (Bitmap)Image.FromStream(mrs);
            return resourceImage.ToSKBitmap();
        }

        private static void SaveFile(
            SKBitmap bitmap,
            string fileName,
            SKEncodedImageFormat format = SKEncodedImageFormat.Webp,
            int quality = 80)
        {
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            using var fs = new FileStream(savePath, FileMode.Create);
            bitmap.Encode(fs, format, quality);
        }

        private static double GetAverage(Stopwatch sw, int iterations)
        {
            return Math.Round(sw.Elapsed.TotalMilliseconds / iterations, 2);
        }
    }
}