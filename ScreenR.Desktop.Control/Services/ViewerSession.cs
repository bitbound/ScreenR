using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Control.Interfaces;
using ScreenR.Desktop.Control.Models;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Control.Services
{
    public interface IViewerSession
    {
        void FrameReceived();

        IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream();
        void Stop();
    }

    internal class ViewerSession : IViewerSession
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentQueue<DateTimeOffset> _fpsQueue = new();
        private readonly ILogger<ViewerSession> _logger;
        private readonly ConcurrentQueue<SentFrame> _sentFrames = new();
        private readonly IDesktopStreamer _streamer;
        private int _currentFps;
        private double _currentMbps;

        public ViewerSession(IDesktopStreamer streamer, ILogger<ViewerSession> logger)
        {
            _streamer = streamer;
            _logger = logger;
            _streamer.LoopIncremented += Streamer_LoopIncremented;
            _streamer.FrameSent += Streamer_FrameSent;
        }

        public void FrameReceived()
        {
            _streamer.FrameReceived();
        }

        public IAsyncEnumerable<DesktopFrameChunk> GetDesktopStream()
        {
            if (EnvironmentHelper.IsDebug)
            {
                _ = Task.Run(LogMetrics);
            }
            return _streamer.GetDesktopStream(_cts.Token);
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping streaming session.");
            _streamer.FrameSent -= Streamer_FrameSent;
            _streamer.LoopIncremented -= Streamer_LoopIncremented;
            _cts.Cancel();
        }

        private void Streamer_FrameSent(object? sender, Models.SentFrame e)
        {
            _sentFrames.Enqueue(e);
        }

        private void Streamer_LoopIncremented(object? sender, EventArgs e)
        {
            _fpsQueue.Enqueue(Time.Now);
        }

        private async Task LogMetrics()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(3_000);

                while (_fpsQueue.TryPeek(out var oldestTime) &&
                    Time.Now - oldestTime > TimeSpan.FromSeconds(1))
                {
                    _fpsQueue.TryDequeue(out _);
                }
                _currentFps = _fpsQueue.Count;

                Debug.WriteLine($"FPS: {_currentFps}");


                while (_sentFrames.TryPeek(out var oldestFrame) &&
                    Time.Now - oldestFrame.Timestamp > TimeSpan.FromSeconds(1))
                {
                    _sentFrames.TryDequeue(out _);
                }
                _currentMbps = (double)_sentFrames.Sum(x => x.FrameSize) / 1024 / 1024 * 8;

                Debug.WriteLine($"Current Mbps: {_currentMbps}");
            }
        }
    }
}
