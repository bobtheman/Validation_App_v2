namespace AccreditValidation.AttributeHelper
{
    using AccreditValidation.Resources.Strings;
    using System.ComponentModel;

    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private static string Localize(string key)
        {
            return AppResources.ResourceManager.GetString(key);
        }

        public LocalizedDescriptionAttribute(string key)
            : base(Localize(key))
        {
        }
    }
}
