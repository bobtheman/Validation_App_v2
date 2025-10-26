namespace AccreditValidation.Components.Services
{
    using global::AccreditValidation.Components.Services.Interface;
    using global::AccreditValidation.Models;
    using Microsoft.AspNetCore.Components;

    public class DirectionService : IDirectionService   
    {
        private readonly ILocalizationService _localizationService;

        public DirectionService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public Task<List<ValidationDirection>> GetValidationDirectionList(string SelectedDirectionCode)
        {
            var directionList = new List<ValidationDirection>
            {
                new ValidationDirection { Direction = _localizationService["In"], Identifier = Enums.ValidationDirection.In.ToString() },
                new ValidationDirection { Direction = _localizationService["Out"], Identifier = Enums.ValidationDirection.Out.ToString() }
            };

            foreach (var direction in directionList)
            {
                direction.IsSelected = direction.Identifier == SelectedDirectionCode;
            }

            return Task.FromResult(directionList);
        }
    }
}
