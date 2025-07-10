using MedServices.Data;
using MedServices.Extensions;
using MedServices.Models;
using MedServices.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NETCore.MailKit.Extensions;
using NETCore.MailKit.Infrastructure;
using NETCore.MailKit.Infrastructure.Internal;

namespace MedServices.Startup
{
    public class Startup
    {
        public static string MedServicesTokenProviderName = "MedServicesTokenProvider";
        public static IConfiguration AppConfiguration { get; set; }
        public static IWebHostEnvironment WebHostEnvironment { get; set; }
		public static MedServicesUserOptions UserOptions { get; set; }
        public static MailKitServiceOptions MailKitOptions { get; set; }
        public static SmsServiceOptions SmsOptions { get; set; }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            AppConfiguration = configuration;
            WebHostEnvironment = env;
        }
        public void ConfigureServices(IServiceCollection services)
        {
			//services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(AppConfiguration["ConnectionStrings:DefaultConnection"]));
			services.AddDbContext<ApplicationDbContext>();
			services.AddIdentity<MedServicesUser, IdentityRole>(options =>
			{
				options.SignIn.RequireConfirmedAccount = true;
                //options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.EmailConfirmationTokenProvider = MedServicesTokenProviderName;
                options.Tokens.PasswordResetTokenProvider = MedServicesTokenProviderName;
                options.Tokens.ProviderMap[MedServicesTokenProviderName] = new TokenProviderDescriptor(typeof(MedServicesTokenProvider));
			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders()
				.AddTokenProvider<MedServicesTokenProvider>(MedServicesTokenProviderName);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
            services.AddControllersWithViews();
			services.AddRazorPages();
			services.AddDistributedMemoryCache();
			services.AddSession(options =>
			{
				options.Cookie.Name = ".MedServices.Session";
				options.Cookie.HttpOnly = false;
				options.Cookie.IsEssential = true;
				options.IdleTimeout = TimeSpan.FromMinutes(10);
			});
            services.AddDataProtection()
                .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                });
            services.Configure<IdentityOptions>(options =>
            {
                // sign-in
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = true; //false;
                options.Password.RequireUppercase = true; //false;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 7;
                //options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.LoginPath = "/UserAccount/Login";
                options.AccessDeniedPath = "/UserAccount/AccessDenied";
                options.SlidingExpiration = true;
            });
			services.Configure<CookiePolicyOptions>(options =>
			{
				options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
			});
			services.AddOptions();
			services.Configure<MedServicesUserOptions>(AppConfiguration?.GetSection(MedServicesUserOptions.SectionName));
            services.Configure<MailKitServiceOptions>(options => AppConfiguration?.GetSection(MailKitServiceOptions.SectionName)?.Bind(options));
            services.Configure<SmsServiceOptions>(options => AppConfiguration?.GetSection(SmsServiceOptions.SectionName)?.Bind(options));
            services.AddMailKit(optionBuilder => optionBuilder.UseMailKit(new MailKitOptions().FillFromConfiguration()));
            services.AddSingleton<IDataProtectionService, DataProtectionService>(sp => new DataProtectionService(sp));
            services.AddSingleton<IEmailConfirmationService, EmailConfirmationService>();
            services.AddSingleton<IPhoneConfirmationService, PhoneConfirmationService>();
        }
		public void Configure(IApplicationBuilder app, IServiceProvider sp, IOptions<MedServicesUserOptions> userOptions,
			IOptions<MailKitServiceOptions> mailKitOptions, IOptions<SmsServiceOptions> smsOptions)
        {
			UserOptions = userOptions?.Value;
            MailKitOptions = mailKitOptions?.Value;
            SmsOptions = smsOptions?.Value;
			app.UseStaticFiles();
            app.UseDefaultFiles();
			app.UseRouting();
			app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseEndpoints(endpoints =>
            {
				endpoints.MapDefaultControllerRoute();
				endpoints.MapRazorPages();
			});
		}
    }
}
