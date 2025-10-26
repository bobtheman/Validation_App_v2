namespace AccreditValidation.Components.Layout
{
    using AccreditValidation.Components.Services.Interface;
    using AccreditValidation.Shared.Services.AlertService;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Routing;
    using System.Globalization;
    using System;

    public partial class MainLayout : IDisposable
    {
        [Inject] IAppState AppState { get; set; }
        [Inject] IAlertService AlertService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        private Action? _appStateChangedHandler;
        private TaskCompletionSource<bool>? _confirmTcs;

        private bool IsConfirmVisible { get; set; }
        private bool IsShowModalVisible { get; set; }
        private string ConfirmTitle { get; set; } = string.Empty;
        private string ConfirmMessage { get; set; } = string.Empty;
        private string ModalTitle { get; set; } = string.Empty;
        private string ModalMessage { get; set; } = string.Empty;
        private string OkText { get; set; } = string.Empty;
        private string CancelText { get; set; } = string.Empty;

        protected override Task OnInitializedAsync()
        {
            AppState.ShowSpinner = true;
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");

            _appStateChangedHandler = () =>
            {
                InvokeAsync(StateHasChanged);
            };
            AppState.OnChange += _appStateChangedHandler;
            AppState.ShowSpinner = false;
            AlertService.RegisterRefreshCallback(StateHasChanged);
            AlertService.OnConfirmRequested += ShowConfirmDialog;
            AlertService.OnModalRequested += ShowModalDialog;
            return Task.CompletedTask;
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            //SpinnerService.Show();
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;

            if (_appStateChangedHandler != null)
            {
                AppState.OnChange -= _appStateChangedHandler;
            }
        }

        private Task<bool> ShowConfirmDialog(string title, string message, string okText, string cancelText)
        {
            ConfirmTitle = title;
            ConfirmMessage = message;
            OkText = okText;
            CancelText = cancelText;

            _confirmTcs = new TaskCompletionSource<bool>();
            IsConfirmVisible = true;

            StateHasChanged();

            return _confirmTcs.Task;
        }

        private void ConfirmOkClicked()
        {
            IsConfirmVisible = false;
            _confirmTcs?.SetResult(true);
            StateHasChanged();
        }

        private void ConfirmCancelClicked()
        {
            IsConfirmVisible = false;
            _confirmTcs?.SetResult(false);
            StateHasChanged();
        }

        private Task<bool> ShowModalDialog(string title, string message, string okText, string cancelText)
        {
            ModalTitle = title;
            ModalMessage = message;
            OkText = okText;
            CancelText = cancelText;

            IsShowModalVisible = true;
            StateHasChanged();

            return Task.FromResult(true);
        }

        private void ModalOkClicked()
        {
            IsShowModalVisible = false;
            StateHasChanged();
        }

        private void ModalCancelClicked()
        {
            IsShowModalVisible = false;
            StateHasChanged();
        }
    }
}
