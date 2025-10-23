namespace AccreditValidation
{
    public class Enums
    {
        public enum BadgeValidationResult : int
        {
            Error = 0x00,
            Success = 0x01,
            BadgeNotFound = 0x02,
            BadgeDeactivated = 0x04,
            RegistrationNotFoundAtEvent = 0x08,
            RegistrationNotApproved = 0x10,
            NoPrivileges = 0x20,
            RegistrationSubTypeNotAllowed = 0x21,
            AreaNotFound = 0x30,
            AreaNotActive = 0x31,
            AreaNoConfigurations = 0x32,
            SpaceFull = 0x33,
        }

        public enum ValidationMode : short
        {
            Online = 1,
            Offline = 2,
        }

        public enum ValidationDirection : short
        {
            In = 1,
            Out = 2,
        }

        public enum RegionalIndicator
        { 
            RegionalIndicatorSymbol = 127397
        }
    }
}
