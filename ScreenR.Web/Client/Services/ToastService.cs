using ScreenR.Shared.Enums;
using ScreenR.Web.Client.Models;
using System;
using System.Collections.Concurrent;
using System.Timers;


namespace ScreenR.Web.Client.Services
{
    public interface IToastService
    {
        ConcurrentList<Toast> Toasts { get; }

        event EventHandler OnToastsChanged;

        void ShowToast(string message, MessageLevel messageLevel = MessageLevel.Information, int expirationMillisecond = 3000, string? styleOverrides = null);
    }

    public class ToastService : IToastService
    {
        public event EventHandler? OnToastsChanged;
        public ConcurrentList<Toast> Toasts { get; } = new();

        public void ShowToast(string message,
            MessageLevel messageLevel = MessageLevel.Information,
            int expirationMillisecond = 3000,
            string? styleOverrides = null)
        {
            var classString = messageLevel switch
            {
                MessageLevel.Information => "bg-info",
                MessageLevel.Warning => "bg-warning",
                MessageLevel.Error => "bg-error",
                MessageLevel.Success => "bg-success",
                _ => "bg-info"
            };

            var toastModel = new Toast(Guid.NewGuid().ToString(),
                message,
                classString,
                TimeSpan.FromMilliseconds(expirationMillisecond),
                styleOverrides);

            Toasts.Add(toastModel);

            OnToastsChanged?.Invoke(this, EventArgs.Empty);

            var removeToastTimer = new System.Timers.Timer(toastModel.Expiration.TotalMilliseconds + 1000)
            {
                AutoReset = false
            };
            removeToastTimer.Elapsed += (s, e) =>
            {
                Toasts.Remove(toastModel);
                OnToastsChanged?.Invoke(this, EventArgs.Empty);
                removeToastTimer.Dispose();
            };
            removeToastTimer.Start();
        }
    }
}
