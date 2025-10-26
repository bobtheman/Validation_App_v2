namespace AccreditValidation.Helper
{
    using AccreditValidation.Helper.Interface;
    using AccreditValidation.Shared.Constants;

    public class DevicePlaformHelper : IDevicePlaformHelper
    {
        public bool HoneywellDevice()
        {
            if (DeviceInfo.Current.Manufacturer == ConstantsName.Honeywell)
            {
                return true;
            }

            return false;
        }
    }
}
