namespace AccreditValidation.Components.Layout
{
    using AccreditValidation.Components.Services.Interface;
    using Microsoft.AspNetCore.Components;
    using System.Globalization;

    public partial class LoginLayout : IDisposable
    {
        [Inject] IAppState AppState { get; set; }

        private Action? _appStateChangedHandler;

        protected override Task OnInitializedAsync()
        {
            AppState.ShowSpinner = true;
            _appStateChangedHandler = () =>
            {
                InvokeAsync(StateHasChanged);
            };
            AppState.OnChange += _appStateChangedHandler;
            AppState.ShowSpinner = false;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_appStateChangedHandler != null)
            {
                AppState.OnChange -= _appStateChangedHandler;
            }
        }
    }
}