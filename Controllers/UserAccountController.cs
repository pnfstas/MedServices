using MedServices.Data;
using MedServices.Models;
using MedServices.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MedServices.Controllers
{
    /*
    public class UserAccountController(
        ApplicationDbContext context,
        UserManager<MedServicesUser> userManager,
        SignInManager<MedServicesUser> signInManager,
        IEmailConfirmationService emailConfirmationService) : Controller
    {
        public ApplicationDbContext DbContext { get; } = context;
        public UserManager<MedServicesUser> UserManager { get; } = userManager;
        public SignInManager<MedServicesUser> SignInManager { get; } = signInManager;
        public IEmailConfirmationService EmailConfirmationService { get; } = emailConfirmationService;
    */
    public class UserAccountController : Controller
    {
        public ApplicationDbContext DbContext { get; }
        public UserManager<MedServicesUser> UserManager { get; }
        public SignInManager<MedServicesUser> SignInManager { get; }
        public IEmailConfirmationService EmailConfirmationService { get; }
        public IPhoneConfirmationService PhoneConfirmationService { get; }
        public UserAccountController(
            ApplicationDbContext context,
            UserManager<MedServicesUser> userManager,
            SignInManager<MedServicesUser> signInManager,
            IEmailConfirmationService emailConfirmationService,
            IPhoneConfirmationService phoneConfirmationService)
        {
            DbContext = context;
            UserManager = userManager;
            SignInManager = signInManager;
            EmailConfirmationService = emailConfirmationService;
            PhoneConfirmationService = phoneConfirmationService;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View(new AccountViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> ProcessRegister(AccountViewModel model)
        {
            IActionResult actionResult = View("Register", new AccountViewModel());
            if(ModelState.IsValid)
            {
                MedServicesUser user = null;
                if(!string.IsNullOrWhiteSpace(model.UserId))
                {
                    user = await UserManager.FindByIdAsync(model.UserId);
                }
                else
                {
                    user = new MedServicesUser(model);
                    IdentityResult identityResult = await UserManager.CreateAsync(user);
                    if(identityResult != IdentityResult.Success)
                    {
                        user = null;
                    }
                }
                if(user != null)
                {
                     actionResult = RedirectToAction("Login", new AccountViewModel(user));
                }
            }
            return actionResult;
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmContact(IFormCollection collection)
        {
            AccountViewModel model = new AccountViewModel(collection);
            if(!string.IsNullOrWhiteSpace(model?.UserName) && model.TwoFactorEnabled)
            {
                if(model.TwoFactorMethod == TwoFactorMethod.TwoFactorByPhoneNumber)
                {
                    if(await PhoneConfirmationService.SendVerificationCode(model))
                    {
                        model.PhoneConfirmationState = ContactConfirmationState.AwaitingConfirmation;
                    }
                }
                else
                {
                    if(await EmailConfirmationService.SendVerificationCode(HttpContext, model))
                    {
                        model.EmailConfirmationState = ContactConfirmationState.AwaitingConfirmation;
                    }
                }
            }
            return View("Register", model ?? new AccountViewModel());
        }
        [HttpGet]
        public async Task<IActionResult> CompleteContactConfirmation(AccountViewModel model, ContactType contactType, string code)
        {
            MedServicesUser user = await UserManager.FindByIdAsync(model.UserId);
            if(user != null)
            {
                if(await UserManager.VerifyTwoFactorTokenAsync(user, Startup.Startup.MedServicesTokenProviderName, code))
                {
                    if(contactType == ContactType.PhoneNumber)
                    {
                        model.PhoneConfirmationState = ContactConfirmationState.Confirmed;
                    }
                    else
                    {
                        model.EmailConfirmationState = ContactConfirmationState.Confirmed;
                    }
                }
            }
            return View("Register", model ?? new AccountViewModel());
        }
        [HttpGet]
        public IActionResult Login(AccountViewModel? model = null)
        {
            return View(model ?? new AccountViewModel());
        }
    }
}
