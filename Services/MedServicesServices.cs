//using HtmlAgilityPack;
using MimeKit;
using MailKit;
using MailKit.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MedServices.Helpers;
using MedServices.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MedServices.Services
{
    public enum VerificationPurpose : int
    {
        None = 0,
        EmailConfirmation,
        PasswordReset
    }
    public class MedServicesTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public MedServicesTokenProviderOptions()
        {
            MedServicesUserOptions userOptions = Startup.Startup.UserOptions;
            TokenLifespan = userOptions?.TokenLifespan ?? TokenLifespan;
            Name = "MedServicesTokenProvider";
        }
    }
    public class MedServicesTokenProvider : DataProtectorTokenProvider<MedServicesUser>//IUserTwoFactorTokenProvider<MedServicesUser>
    {
        internal class TwoFactorTokenKey(string userId, VerificationPurpose purpose) : IEquatable<TwoFactorTokenKey>
        {
            public string UserId { get; set; } = userId;
            public VerificationPurpose Purpose { get; set; } = purpose;
			public override bool Equals(object? obj)
			{
				return (obj is TwoFactorTokenKey twoFactorTokenKey) && Equals(twoFactorTokenKey);
			}
			public override int GetHashCode()
			{
				return UserId?.GetHashCode() ?? 0 ^ Purpose.GetHashCode();
			}
            public bool Equals(TwoFactorTokenKey? other)
            {
                return other != null && UserId?.Equals(other.UserId) != false && other.Purpose == Purpose;
            }
			public static bool operator ==(TwoFactorTokenKey twoFactorTokenKey1, TwoFactorTokenKey twoFactorTokenKey2) => twoFactorTokenKey1.Equals(twoFactorTokenKey2);
			public static bool operator !=(TwoFactorTokenKey twoFactorTokenKey1, TwoFactorTokenKey twoFactorTokenKey2) => !twoFactorTokenKey1.Equals(twoFactorTokenKey2);
        }
        internal class TwoFactorToken(string? token = null, DateTime creationDate = default)
        {
            public string Token { get; set; } = token;
			public DateTimeOffset ExpiresOn { get; set; } =
                (creationDate == default ? DateTimeOffset.Now : new DateTimeOffset(creationDate)) + Options.Value.TokenLifespan;
            public bool IsValid()
            {
                return ExpiresOn > DateTimeOffset.Now;
            }
			public override bool Equals(object obj)
			{
				return (obj is TwoFactorToken twoFactorToken) && twoFactorToken.ExpiresOn == ExpiresOn && twoFactorToken.Token == Token;
			}
			public override int GetHashCode()
			{
				return Token.GetHashCode() ^ ExpiresOn.GetHashCode();
			}
			public static bool operator ==(TwoFactorToken twoFactorToken1, TwoFactorToken twoFactorToken2) => twoFactorToken1.Equals(twoFactorToken2);
			public static bool operator !=(TwoFactorToken twoFactorToken1, TwoFactorToken twoFactorToken2) => !twoFactorToken1.Equals(twoFactorToken2);
        }
        private Dictionary<TwoFactorTokenKey, TwoFactorToken> TokenStore { get; }
        public static IOptions<MedServicesTokenProviderOptions> Options { get; set; }
        public MedServicesTokenProvider(IDataProtectionProvider provider, IOptions<MedServicesTokenProviderOptions> options, ILogger<DataProtectorTokenProvider<MedServicesUser>> logger)
            : base(provider, options,  logger)
        {
            Options = options;
            TokenStore = new Dictionary<TwoFactorTokenKey, TwoFactorToken>();
        }
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<MedServicesUser> manager, MedServicesUser user)
        {
            return Task.FromResult(manager.SupportsUserTwoFactor);
        }
        public Task<string> GenerateAsync(string strPurpose, UserManager<MedServicesUser> manager, MedServicesUser user)
        {
            string tokenEncoded = null;
            VerificationPurpose purpose = VerificationPurpose.None;
            if(Enum.TryParse(strPurpose, out purpose) && purpose != VerificationPurpose.None)
            {
                MedServicesUserOptions userOptions = Startup.Startup.UserOptions;
                if(!string.IsNullOrWhiteSpace(user?.Id) && userOptions != null)
                {
                    string token = SecurityHelper.GenerateRandomString(userOptions.VerificationCodeLength);
                    TokenStore[new TwoFactorTokenKey(user.Id, purpose)] = new TwoFactorToken(token);
                    tokenEncoded = Convert.ToBase64String(Encoding.Default.GetBytes(token));
                }
            }
            return Task.FromResult(tokenEncoded);
        }
        public Task<bool> ValidateAsync(string strPurpose, string token, UserManager<MedServicesUser> manager, MedServicesUser user)
        {
            bool isValidToken = false;
            if(!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(user?.Id))
            {
                VerificationPurpose purpose = VerificationPurpose.None;
                if(Enum.TryParse(strPurpose, out purpose) && purpose != VerificationPurpose.None)
                {
                    string tokenDecoded = Encoding.Default.GetString(Convert.FromBase64String(token.Trim()));
                    TwoFactorToken twoFactorToken = TokenStore[new TwoFactorTokenKey(user.Id, purpose)];
                    isValidToken = string.Equals(twoFactorToken.Token, tokenDecoded) && twoFactorToken.IsValid();
                }
            }
            return Task.FromResult(isValidToken);
        }
    }
    public interface IDataProtectionService
    {
        string? Protect(string? strUnprotectedData, string purpose = "password_protection");
        string? Unprotect(string? strProtectedData, string purpose = "password_protection");
    }
    public class DataProtectionService : IDataProtectionService
    {
        private readonly IDataProtectionProvider provider;
        public DataProtectionService(IServiceProvider sp)
        {
            provider = sp?.GetDataProtectionProvider();
        }
        public string? Protect(string? strUnprotectedData, string purpose = "password_protection")
        {
            IDataProtector protector = provider?.CreateProtector(purpose);
            return protector?.Protect(strUnprotectedData);
        }
        public string? Unprotect(string? strProtectedData, string purpose = "password_protection")
        {
            IDataProtector protector = provider?.CreateProtector(purpose);
            return protector?.Unprotect(strProtectedData);
        }
    }
    public interface IEmailConfirmationService
    {
        Task<bool> SendVerificationCode(HttpContext context, AccountViewModel model); 
    }
    public class EmailConfirmationService : IEmailConfirmationService
    {
        internal class MailKitSmtpResponse : SmtpResponse
        {
            public new SmtpStatusCode StatusCode
            { 
                get => base.StatusCode;
                set
                {
                    typeof(SmtpResponse).GetProperty("StatusCode")?.GetSetMethod(true)?.Invoke(this, new object[] { value });
                }
            }
            public string EnhancedStatusCode { get; set; }
            public MailKitSmtpResponse(SmtpStatusCode code, string response, string? enhancedCode = null) : base(code, response)
            {
            }
            public MailKitSmtpResponse(string response) : this(default, response)
            {
                Regex regex = new Regex("(?<StatusCodeEnh>\\d\\.\\d\\.\\d)\\s+(?<StatusCodeMain>\\w+)");
                Match match = regex.Match(response);
                if(match.Success)
                {
                    if(match.Groups["StatusCodeMain"].Success)
                    {
                        string[] arrNames =
                            (from curName in Enum.GetNames<SmtpStatusCode>()
                             select curName.ToUpper()).ToArray();
                        SmtpStatusCode[] arrValues = Enum.GetValues<SmtpStatusCode>();
                        int index = Array.IndexOf(arrNames, match.Groups["StatusCodeMain"].Value.ToUpper());
                        if(index >= 0)
                        {
                            StatusCode = arrValues[index];
                        }
                    }
                    if(match.Groups["StatusCodeEnh"].Success)
                    {
                        EnhancedStatusCode = match.Groups["StatusCodeEnh"].Value;
                    }
                }
            }
        }
        private UserManager<MedServicesUser> UserManager { get; }
        public EmailConfirmationService(IServiceProvider sp)
        {
            IServiceScope scope = sp.CreateScope();
            UserManager = scope.ServiceProvider.GetRequiredService<UserManager<MedServicesUser>>();
            Debug.WriteLine(UserManager);
        }
        public async Task<bool> SendVerificationCode(HttpContext context, AccountViewModel model)
        {
            bool result = false;
            if(!string.IsNullOrWhiteSpace(model?.Name) && model.TwoFactorMethod == TwoFactorMethod.TwoFactorByEmail
                && !string.IsNullOrWhiteSpace(model.Email) && Startup.Startup.MailKitOptions != null)
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
                    string strEmailMessagePath = Path.Combine(Startup.Startup.WebHostEnvironment.WebRootPath, "ConfirmEmailMessage.html");
                    Uri urlEmailMessage = new Uri(strEmailMessagePath);
                    TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
                    WebBrowser webBrowser = new WebBrowser();
                    webBrowser.DocumentCompleted += (object sender, WebBrowserDocumentCompletedEventArgs args) =>
                    {
                        string body = webBrowser.Document.Body.OuterHtml;
                        tcs.TrySetResult(body);
                    };
                    webBrowser.Navigate(urlEmailMessage);
                    string body = await tcs.Task;
                    result = await SendConfirmationMessage(context, model, user, body);
                }
            }
            return result;
        }
        private async Task<bool> SendConfirmationMessage(HttpContext context, AccountViewModel model, MedServicesUser user, string body)
        {
            bool result = false;
            EventHandler<MessageSentEventArgs> handler = delegate { };
            SmtpClient smtpClient = new SmtpClient();
            try
            {
                MailKitServiceOptions mailKitOptions = Startup.Startup.MailKitOptions;
                string code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                string query = $"{{ model: \"{JsonConvert.SerializeObject(model)}\", contactType: \"{Enum.GetName(ContactType.EMail)}\", code: \"{code}\"}}";
                string url = $"{context.Request.Scheme}://{context.Request.Host}/UserAccount/CompleteConfirmation/?{query}";
                body = body.Replace("@username", user.Name).Replace("@url", url);
                MimeMessage message = new MimeMessage();
                BodyBuilder builder = new BodyBuilder()
                {
                    HtmlBody = body
                };
                message.Body = builder.ToMessageBody();
                message.From.Add(new MailboxAddress(mailKitOptions.SenderName, mailKitOptions.SenderEmail));
                message.To.Add(new MailboxAddress(user.Name, user.Email));
                message.Subject = "Confirm your E-Mail on MedServices.";
                smtpClient.CheckCertificateRevocation = true;
                smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                //smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");
                await smtpClient.ConnectAsync(mailKitOptions.Server, mailKitOptions.Port, mailKitOptions.Security);
                await smtpClient.AuthenticateAsync(mailKitOptions.Account, mailKitOptions.Password);
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                handler = async (sender, args) =>
                {
                    smtpClient.MessageSent -= handler;
                    await Task.Yield();
                    MailKitSmtpResponse smtpResponse = new MailKitSmtpResponse(args.Response);
                    if(smtpResponse.StatusCode == SmtpStatusCode.Ok)
                    {
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        tcs.TrySetResult(false);
                    }
                };
                smtpClient.MessageSent += handler;
                await smtpClient.SendAsync(message);
                result = await tcs.Task;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
            finally
            {
                smtpClient.MessageSent -= handler;
            }
            return result;
        }
    }
    public interface IPhoneConfirmationService
    {
        Task<bool> SendVerificationCode(AccountViewModel model); 
    }
    public class PhoneConfirmationService : IPhoneConfirmationService
    {
        private UserManager<MedServicesUser> UserManager { get; }
        public PhoneConfirmationService(IServiceProvider sp)
        {
            IServiceScope scope = sp.CreateScope();
            UserManager = scope.ServiceProvider.GetRequiredService<UserManager<MedServicesUser>>();
            Debug.WriteLine(UserManager);
        }
        public async Task<bool> SendVerificationCode(AccountViewModel model)
        {
            bool result = false;
            MedServicesUser user = await UserManager.FindByIdAsync(model.UserId);
            if(!string.IsNullOrWhiteSpace(user?.Name) && model?.TwoFactorMethod == TwoFactorMethod.TwoFactorByPhoneNumber
                && !string.IsNullOrWhiteSpace(model.PhoneNumber) && Startup.Startup.SmsOptions != null)
            {
            }
            return result;
        }
    }
}
