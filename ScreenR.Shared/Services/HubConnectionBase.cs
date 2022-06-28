using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using ScreenR.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Services
{
    public class HubConnectionBase
    {
        protected async Task<Result<T>> WaitForResponse<T>(
            HubConnection connection, 
            string methodName, 
            Guid requestId,
            Action sendAction, 
            int timeoutMs = 5_000)

            where T : BaseDto
        {
            try
            {
                T? returnValue = default;
                var signal = new SemaphoreSlim(0, 1);

                using var token = connection.On<byte[]>(methodName, dtoBytes =>
                {
                    var baseDto = MessagePackSerializer.Deserialize<BaseDto>(dtoBytes);

                    if (baseDto.RequestId == requestId)
                    {
                        returnValue = MessagePackSerializer.Deserialize<T>(dtoBytes);
                        signal.Release();
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
