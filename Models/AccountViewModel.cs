using MedServices.Extensions;
using MedServices.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace MedServices.Models
{
    public enum TwoFactorMethod : int
    {
        TwoFactorByEmail = 0,
        TwoFactorByPhoneNumber
    }
    
    public enum ContactType : int
    {
        [Description("E-Mail")]
        EMail = 0,
        [Description("Phone number")]
        PhoneNumber
    }

    public enum ContactConfirmationState : int
    {
        [Description("is not confirmed")]
        NotConfirmed = 0,
        [Description("awaiting confirmation")]
        AwaitingConfirmation,
        [Description("is confirmed")]
        Confirmed
    }

    public class TwoFactorAuthAttribute : ValidationAttribute
    {
        public static string DefaultErrorMessage { get; set; } = "User name must be specified";
        public new string? ErrorMessage { get => base.ErrorMessage ?? DefaultErrorMessage; set => base.ErrorMessage = value; }
        public TwoFactorAuthAttribute(string? errorMessage = null) : base(errorMessage ?? DefaultErrorMessage)
        {
        }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            ValidationResult result = null;
            AccountViewModel model = validationContext?.ObjectInstance as AccountViewModel;
            if(model != null)
            {
                result = model.TwoFactorEnabled && model.TwoFactorMethod == TwoFactorMethod.TwoFactorByEmail && string.IsNullOrWhiteSpace(model.Email)
                    || model.TwoFactorEnabled && model.TwoFactorMethod == TwoFactorMethod.TwoFactorByPhoneNumber && string.IsNullOrWhiteSpace(model.PhoneNumber) ?
                    new ValidationResult(ErrorMessage) : ValidationResult.Success;
            }
            return result;
        }
    }

    public abstract class ModelBase
    {
        [ValidateNever]
        public bool Submitted { get; set; }
        //public string ModelJson => JsonConvert.SerializeObject(this);
        public string DisplayNamesJson => JsonConvert.SerializeObject(this.GetDisplayNames());
        public string ErrorDescriptionsJson => JsonConvert.SerializeObject(this.GetErrorDescriptions());
    }
    
    public class AccountViewModel : ModelBase
    {
        public string? UserId { get; set; }
        [Required(ErrorMessage = "Login name must be specified")]
        [Display(Name = "User name, E-Mail or Phone number")]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "Password must be specified")]
        [DataType(DataType.Password)]
        [StringLength(20)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Password is not confirmed")]
        [Compare("Password", ErrorMessage = "Password confirmation don't match password")]
        [DataType(DataType.Password)]
        [StringLength(20)]
        [Display(Name = "Password confirmation")]
        public string PasswordConfirmation { get; set; }
        [Display(Name = "Enable two factor authentication")]
        public bool TwoFactorEnabled { get; set; }
        [Display(Name = "Two factor authentication method")]
        public TwoFactorMethod TwoFactorMethod { get; set; }
        [TwoFactorAuth(ErrorMessage = "E-Mail must be specified")]
        [EmailAddress]
        [Display(Name = "E-Mail")]
        public string? Email { get; set; }
        [TwoFactorAuth(ErrorMessage = "Phone number must be specified")]
        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
        public string? NormilizedPhoneNumber => GetNormilizedPhoneNumber(PhoneNumber);
        public ContactConfirmationState EmailConfirmationState { get; set; }
        public ContactConfirmationState PhoneConfirmationState { get; set; }
		[Display(Name = "Full name")]
		public string Name { get; set; }
        [Required(ErrorMessage = "ESIA login must be specified")]
        [Display(Name = "ESIA login")]
        public string? EsiaLogin { get; set; }
        [Required(ErrorMessage = "ESIA password must be specified")]
        [DataType(DataType.Password)]
        [StringLength(20)]
        [Display(Name = "ESIA password")]
        public string? EsiaPassword { get; set; }
        [Required(ErrorMessage = "Password is not confirmed")]
        [Compare("EsiaPassword", ErrorMessage = "ESIA password confirmation don't match ESIA password")]
        [DataType(DataType.Password)]
        [StringLength(20)]
        [Display(Name = "ESIA password confirmation")]
        public string EsiaPasswordConfirmation { get; set; }
		public static string? GetNormilizedPhoneNumber(string strPhoneNumber)
		{
			return !string.IsNullOrWhiteSpace(strPhoneNumber) ?
				string.Concat(Regex.Matches(strPhoneNumber, "[0-9]+")?.Select(curMatch => curMatch?.Value)) : null;
		}
        public string? GetContactConfirmationStateString(ContactType contactType)
        {
            ContactConfirmationState state = ContactConfirmationState.NotConfirmed;
            if(contactType == ContactType.PhoneNumber)
            {
                state = PhoneConfirmationState;
            }
            else
            {
                state = EmailConfirmationState;
            }
            return $"{contactType.GetValueDescription()} {state.GetValueDescription()}";
        }
        public void FillFromUser(MedServicesUser user)
        {
            if(user != null)
            {
                this.CopyPropertiesFrom(user);
                ServiceCollection services = new ServiceCollection();
                services.AddSingleton<IDataProtectionService, DataProtectionService>(sp => new DataProtectionService(sp));
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                IServiceScope scope = serviceProvider?.CreateScope();
                IDataProtectionService dataPropectionService = scope?.ServiceProvider?.GetRequiredService<IDataProtectionService>();
                EsiaPassword = dataPropectionService?.Unprotect(user.EsiaProtectedPassword);
            }
        }
        public void FillFromFormCollection(IFormCollection collection)
        {
            this.CopyPropertiesFromFormCollection(collection);
        }
        public AccountViewModel()
        {
        }
        public AccountViewModel(MedServicesUser? user = null)
        {
            FillFromUser(user);
        }
        public AccountViewModel(IFormCollection? collection = null)
        {
            FillFromFormCollection(collection);
        }
    }

}
