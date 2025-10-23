using AccreditValidation.Components.Services.Interface;

public class AlertService : IAlertService
{
    public event Action? OnAlertChanged;
    public event Func<string, string, string, string, Task<bool>>? OnConfirmRequested;
    public event Func<string, string, string, string, Task>? OnModalRequested;
    private string _alertMessage = string.Empty;
    private string _alertType = "alert-success";

    public string AlertMessage => _alertMessage;
    public string AlertType => _alertType;

    public void RegisterRefreshCallback(Action refreshCallback)
    {
        OnAlertChanged += refreshCallback;
    }

    public async Task ShowSuccessAlertAsync(string title, string message)
    {
        _alertMessage = $"{title}: {message}";
        _alertType = "alert-success";
        OnAlertChanged?.Invoke();

        await Task.Delay(3000);

        _alertMessage = string.Empty;
        OnAlertChanged?.Invoke();
    }

    public async Task ShowErrorAlertAsync(string title, string message)
    {
        _alertMessage = $"{title}: {message}";
        _alertType = "alert-danger";
        OnAlertChanged?.Invoke();

        await Task.Delay(3000);

        _alertMessage = string.Empty;
        OnAlertChanged?.Invoke();
    }

    public async Task<bool> ShowConfirmAlertAsync(string title, string message, string okText, string cancelText)
    {
        if (OnConfirmRequested != null)
        {
            return await OnConfirmRequested(title, message, okText, cancelText);
        }
        return false;
    }

    public async Task ShowModalAlertAsync(string title, string message, string okText, string cancelText)
    {
        if (OnModalRequested != null)
        {
            await OnModalRequested(title, message, okText, cancelText);
        }
    }
}
