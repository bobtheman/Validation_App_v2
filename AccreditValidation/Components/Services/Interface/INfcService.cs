namespace AccreditValidation.Components.Services.Interface;

public interface INfcService
{
    bool IsAvailable { get; }

    bool IsEnabled { get; }

    event EventHandler<string> TagRead;

    event EventHandler<string> TagError;

    void StartListening();

    void StopListening();
}