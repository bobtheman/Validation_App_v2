namespace AccreditValidation.Components.Services.Interface
{
    public interface IAlertService
    {
        string AlertMessage { get; }
        string AlertType { get; }

        void RegisterRefreshCallback(Action refreshCallback);
        event Func<string, string, string, string, Task<bool>>? OnConfirmRequested;
        event Func<string, string, string, string, Task>? OnModalRequested;
        Task ShowSuccessAlertAsync(string title, string message);
        Task ShowErrorAlertAsync(string title, string message);
        Task<bool> ShowConfirmAlertAsync(string title, string message, string okText, string cancelText);
        Task ShowModalAlertAsync(string title, string message, string okText, string cancelText);
    }
}
