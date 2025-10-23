using Microsoft.AspNetCore.Components;

namespace AccreditValidation.Components.Shared
{
    public partial class Alert
    {
        [Parameter] public bool Visible { get; set; }
        [Parameter] public string AlertMessage { get; set; } = "";
        [Parameter] public string AlertType { get; set; } = "alert-success"; // Bootstrap classes for example
    }
}