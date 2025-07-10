using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MedServices.Models
{
	public class MedServicesUserOptions
    {
        public static string SectionName { get; } = "ForumEngineUserOptions";
        public int DefaultUserNameLength { get; set; }
        public int DefaultPasswordLength { get; set; }
        public int VerificationCodeLength { get; set; }
        public int PasswordResetCodeLength { get; set; }
        public TimeSpan TokenLifespan { get; set; }
        public int MaxRegistrationFailedCount { get; set; }
        public int MaxAccessFailedCount { get; set; }
        public int RegistrationLockoutInterval { get; set; }
        public int LoginLockoutInterval { get; set; }
        public void Fill(IConfiguration configuration)
        {
            configuration?.GetSection(SectionName)?.Bind(this);
            JObject jobjOptions = JObject.Parse(configuration?.GetSection(SectionName)?.Value);
            if(jobjOptions?.ContainsKey(nameof(TokenLifespan)) == true)
            {
                dynamic dynTokenLifespan = jobjOptions[nameof(TokenLifespan)].ToObject<dynamic>();
                TokenLifespan = new TimeSpan(dynTokenLifespan.Days, dynTokenLifespan.Hours, dynTokenLifespan.Minutes, dynTokenLifespan.Seconds);
                /*
                JToken jtokTokenLifespan = jobjOptions[nameof(TokenLifespan)];
                TokenLifespan = new TimeSpan(
                    (int)jtokTokenLifespan["Days"],
                    (int)jtokTokenLifespan["Hours"],
                    (int)jtokTokenLifespan["Minutes"],
                    (int)jtokTokenLifespan["Seconds"]);
                */
            }
        }
    }

    public class MailKitServiceOptions
    {
        public static string SectionName { get; } = "MailKit-Hotmail";
        public string Server { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public bool Security { get; set; }
    }

    public class SmsServiceOptions
    {
        public static string SectionName { get; } = "Sms";
        public string Url { get; set; }
        public string Authorization { get; set; }
    }
}
