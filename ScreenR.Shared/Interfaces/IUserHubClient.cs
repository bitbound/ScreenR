﻿using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Interfaces
{
    public interface IUserHubClient
    {
        Task ShowToast(string message, MessageLevel messageLevel);
        Task ReceiveDto(DtoWrapper dto);
    }
}
