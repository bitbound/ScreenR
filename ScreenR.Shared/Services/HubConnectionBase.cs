using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ScreenR.Shared.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Services
{
    public class HubConnectionBase
    {
        private static readonly ConcurrentDictionary<Guid, byte[]> _dtoChunks = new();
        private readonly ILogger<HubConnectionBase> _logger;

        public HubConnectionBase(ILogger<HubConnectionBase> logger)
        {
            _logger = logger;
        }

        protected async Task<Result<T>> WaitForResponse<T>(
            HubConnection connection, 
            string methodName, 
            Guid requestId,
            Action sendAction, 
            int timeoutMs = 5_000)
        {
            try
            {
                T? returnValue = default;
                var signal = new SemaphoreSlim(0, 1);

                using var token = connection.On<DtoWrapper>(methodName, wrapper =>
                {
                    try
                    {
                        if (wrapper.RequestId == requestId)
                        {
                            _dtoChunks.AddOrUpdate(requestId, wrapper.DtoChunk, (k, v) =>
                            {
                                return v.Concat(wrapper.DtoChunk).ToArray();
                            });

                            if (wrapper.IsLastChunk && _dtoChunks.TryRemove(requestId, out var concatChunks))
                            {
                                returnValue = MessagePackSerializer.Deserialize<T>(concatChunks);
                                signal.Release();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while handling DTO wrapper.");
                    }
                 
                });

                sendAction.Invoke();

                var waitResult = await signal.WaitAsync(timeoutMs);

                if (!waitResult)
                {
                    return Result.Fail<T>("Timed out while waiting for response.");
                }

                if (returnValue is null)
                {
                    return Result.Fail<T>("Response was empty.");
                }

                return Result.Ok(returnValue);
            }
            catch (Exception ex)
            {
                return Result.Fail<T>(ex);
            }
        }
    }
}
