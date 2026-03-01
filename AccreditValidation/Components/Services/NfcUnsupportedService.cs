namespace AccreditValidation.Components.Services;

using global::AccreditValidation.Components.Services.Interface;
using System;

public class NfcUnsupportedService : INfcService
{
    public bool IsAvailable => false;
    public bool IsEnabled => false;

    public event EventHandler<string>? TagRead;
    public event EventHandler<string>? TagError;

    public void StartListening() { }
    public void StopListening() { }
}
