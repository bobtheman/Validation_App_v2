using AccreditValidation.Components.Services.Interface;
using Microsoft.AspNetCore.Components;

namespace AccreditValidation.Components.Shared
{
    public partial class ConfirmDialog
    {
        [Inject] private ILocalizationService LocalizationService { get; set; }

        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string Message { get; set; }
        [Parameter] public string OkText { get; set; }
        [Parameter] public string CancelText { get; set; }

        [Parameter] public EventCallback<bool> OnClose { get; set; }


        private async Task OnOkClicked()
        {
            await OnClose.InvokeAsync(true);
        }

        private async Task OnCancelClicked()
        {
            await OnClose.InvokeAsync(false);
        }
    }
}