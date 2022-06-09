using ScreenR.Desktop.Core;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Desktop.Core.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace ScreenR.Desktop.Windows.Capture
{
    public interface IScreenRecorder
    {
        Task<Result> CaptureVideo(
            DisplayInfo display,
            int frameRate,
            Stream destinationStream,
            CancellationToken cancellationToken);
    }

    public class ScreenRecorder : IScreenRecorder
    {
        private readonly IScreenGrabber _grabber;

        public ScreenRecorder(IScreenGrabber screenGrabber)
        {
            _grabber = screenGrabber;
        }
        public async Task<Result> CaptureVideo(
            DisplayInfo display,
            int frameRate,
            Stream destinationStream,
            CancellationToken cancellationToken)
        {
            try
            {

                var captureArea = new Rectangle(Point.Empty, display.MonitorArea.Size);

                var evenWidth = captureArea.Width % 2 == 0 ? (uint)captureArea.Width : (uint)captureArea.Width + 1;
                var evenHeight = captureArea.Height % 2 == 0 ? (uint)captureArea.Height : (uint)captureArea.Height + 1;

                var size = captureArea.Width * 4 * captureArea.Height;

                var tempBuffer = new byte[size];

                var sourceVideoProperties = VideoEncodingProperties.CreateUncompressed(
                    MediaEncodingSubtypes.Argb32,
                    evenWidth,
                    evenHeight);

                var videoDescriptor = new VideoStreamDescriptor(sourceVideoProperties);

                var mediaStreamSource = new MediaStreamSource(videoDescriptor)
                {
                    BufferTime = TimeSpan.Zero
                };

                var stopwatch = Stopwatch.StartNew();

                mediaStreamSource.Starting += (sender, args) =>
                {
                    args.Request.SetActualStartPosition(stopwatch.Elapsed);
                };

                mediaStreamSource.SampleRequested += (sender, args) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        args.Request.Sample = null;
                        return;
                    }

                    var result = _grabber.GetScreenGrab(display.DeviceName);

                    while (!result.IsSuccess || result.Value is null)
                    {
                        result = _grabber.GetScreenGrab(display.DeviceName);
                    }

                    using var currentFrame = result.Value;

                    var pixels = currentFrame.GetPixels();
                    Marshal.Copy(pixels, tempBuffer, 0, size);
                    args.Request.Sample = MediaStreamSample.CreateFromBuffer(tempBuffer.AsBuffer(), stopwatch.Elapsed);
                };

                var encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
                encodingProfile.Video.Width = evenWidth;
                encodingProfile.Video.Height = evenHeight;

                var transcoder = new MediaTranscoder
                {
                    HardwareAccelerationEnabled = true,
                    AlwaysReencode = true
                };

                var prepareResult = await transcoder.PrepareMediaStreamSourceTranscodeAsync(
                    mediaStreamSource,
                    destinationStream.AsRandomAccessStream(),
                    encodingProfile);

                await prepareResult.TranscodeAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex);
            }
        }
    }
}
