namespace AccreditValidation.Components.Layout
{
    using AccreditValidation.Components.Services.Interface;
    using Microsoft.AspNetCore.Components;
    using System;
    using System.Threading.Tasks;

    public partial class NavMenu : IDisposable
    {
        [Inject] private ILocalizationService LocalizationService { get; set; }
        [Inject] private ILanguageStateService LanguageStateService { get; set; }

        protected override Task OnInitializedAsync()
        {
            LanguageStateService.OnLanguageChanged += Refresh;
            return Task.CompletedTask;
        }

        private void Refresh() => InvokeAsync(StateHasChanged);

        public void Dispose()
        {
            LanguageStateService.OnLanguageChanged -= Refresh;
        }
    }
}
