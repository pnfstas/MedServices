using MedServices.Extensions;
using MedServices.Services;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedServices.Models
{
    public class MedServicesUser : IdentityUser
    {
        [ProtectedPersonalData]
        public override string? UserName
        { 
            get => base.UserName;
            set
            {
                base.UserName = value;
                NormalizedUserName = value?.ToUpperInvariant();
            }
        }
        [ProtectedPersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public override string? Email
        { 
            get => base.Email;
            set
            {
                base.Email = value;
                NormalizedEmail = value?.ToUpperInvariant();
            }
        }
        [ProtectedPersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public override string? PhoneNumber
        {
            get => base.PhoneNumber;
            set
            {
                base.PhoneNumber = value;
            }
        }
		[ProtectedPersonalData]
		[Column(TypeName = "nvarchar(256)")]
		public string? NormilizedPhoneNumber { get; set; }
		[PersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? Name { get; set; }
        [ProtectedPersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? EsiaLogin { get; set; }
        [ProtectedPersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? EsiaProtectedPassword { get; set; }
        public MedServicesUser() : base()
        {
        }
        public MedServicesUser(string userName) : base(userName)
        {
        }
        public MedServicesUser(AccountViewModel model)
        {
            FillFromModel(model);
        }
        public void FillFromModel(AccountViewModel model)
        {
            if(model != null)
            {
                this.CopyPropertiesFrom(model);
                ServiceCollection services = new ServiceCollection();
                services.AddSingleton<IDataProtectionService, DataProtectionService>(sp => new DataProtectionService(sp));
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                IServiceScope scope = serviceProvider?.CreateScope();
                IDataProtectionService dataPropectionService = scope?.ServiceProvider?.GetRequiredService<IDataProtectionService>();
                EsiaProtectedPassword = dataPropectionService?.Protect(model.EsiaPassword);
            }
        }
    }
}
