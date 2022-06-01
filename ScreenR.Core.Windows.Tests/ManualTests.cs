using Microsoft.Extensions.Logging;
using ScreenR.Core.Models;
using SkiaSharp;
using System.Diagnostics;

namespace ScreenR.Core.Windows.Tests
{
    [TestClass]
    //[Ignore("Manual")]
    public class ManualTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private LoggerFactory _factory;
        private ILogger<ScreenGrabberWindows> _logger;
        private ScreenGrabberWindows _grabber;
        private IEnumerable<DisplayInfo> _displays;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Init()
        {
            _factory = new LoggerFactory();
            _logger = _factory.CreateLogger<ScreenGrabberWindows>();
            _grabber = new ScreenGrabberWindows(_logger);
            _displays = _grabber.GetDisplays();
        }

        [TestMethod]
        public void SaveFileTest()
        {
            var bitmap = _grabber.GetScreenGrab(_displays.First().Name);

            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Test.jpg");
            using var fs = new FileStream(savePath, FileMode.Create);
            bitmap.Value.Encode(fs, SKEncodedImageFormat.Jpeg, 80);
        }

        [TestMethod]
        public void CaptureSpeedTest()
        {
            var display = _displays.First();

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < 60; i++)
            {
                using var bitmap = _grabber.GetScreenGrab(display.Name).Value;
            }

            sw.Stop();

            Console.WriteLine($"ScreenGrab Time: {sw.Elapsed.TotalMilliseconds}ms");



            sw.Restart();

            for (var i = 0; i < 60; i++)
            {
                using var bitmap = _grabber.GetDirectXGrab(display).Value;
            }

            sw.Stop();

            Console.WriteLine($"DirectX Time: {sw.Elapsed.TotalMilliseconds}ms");



            sw.Restart();

            for (var i = 0; i < 60; i++)
            {
                using var bitmap = _grabber.GetBitBltGrab(display).Value;
            }

            sw.Stop();

            Console.WriteLine($"BitBlt Time: {sw.Elapsed.TotalMilliseconds}ms");



            sw.Restart();

            for (var i = 0; i < 60; i++)
            {
                using var bitmap = _grabber.GetWinFormsGrab(display).Value;
            }

            sw.Stop();

            Console.WriteLine($"WinForms Time: {sw.Elapsed.TotalMilliseconds}ms");
        }
    }
}