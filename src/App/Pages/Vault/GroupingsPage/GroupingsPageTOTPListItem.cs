using System;
using System.Threading.Tasks;
using Bit.App.Resources;
using Bit.App.Utilities;
using Bit.Core.Abstractions;
using Bit.Core.Models.View;
using Bit.Core.Utilities;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public class GroupingsPageTOTPListItem : ExtendedViewModel, IGroupingsPageListItem
    {
        private readonly ITotpService _totpService;
        private readonly IPlatformUtilsService _platformUtilsService;
        private readonly IClipboardService _clipboardService;
        private CipherView _cipher;

        private bool _websiteIconsEnabled;
        private string _iconImageSource = string.Empty;

        public int interval { get; set; }
        private double _progress;
        private string _totpSec;
        private string _totpCode;
        private string _totpCodeFormatted = "938 928";


        public GroupingsPageTOTPListItem(CipherView cipherView, bool websiteIconsEnabled)
        {
            _totpService = ServiceContainer.Resolve<ITotpService>("totpService");
            _platformUtilsService = ServiceContainer.Resolve<IPlatformUtilsService>("platformUtilsService");
            _clipboardService = ServiceContainer.Resolve<IClipboardService>("clipboardService");

            Cipher = cipherView;
            WebsiteIconsEnabled = websiteIconsEnabled;
            interval = _totpService.GetTimeInterval(Cipher.Login.Totp);
            CopyCommand = new AsyncCommand(CopyToClipboardAsync,
                 onException: ex => _logger.Value.Exception(ex),
                 allowsMultipleExecutions: false);
        }

        readonly LazyResolve<ILogger> _logger = new LazyResolve<ILogger>("logger");

        public AsyncCommand CopyCommand { get; set; }

        public CipherView Cipher
        {
            get => _cipher;
            set => SetProperty(ref _cipher, value);
        }

        public string TotpCodeFormatted
        {
            get => _totpCodeFormatted;
            set => SetProperty(ref _totpCodeFormatted, value,
                additionalPropertyNames: new string[]
                {
                    nameof(TotpCodeFormattedStart),
                    nameof(TotpCodeFormattedEnd),
                });
        }
        
        public string TotpSec
        {
            get => _totpSec;
            set => SetProperty(ref _totpSec, value);
        }
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }
        public bool WebsiteIconsEnabled
        {
            get => _websiteIconsEnabled;
            set => SetProperty(ref _websiteIconsEnabled, value);
        }

        public bool ShowIconImage
        {
            get => WebsiteIconsEnabled
                && !string.IsNullOrWhiteSpace(Cipher.Login?.Uri)
                && IconImageSource != null;
        }

        public string IconImageSource
        {
            get
            {
                if (_iconImageSource == string.Empty) // default value since icon source can return null
                {
                    _iconImageSource = IconImageHelper.GetLoginIconImage(Cipher);
                }
                return _iconImageSource;
            }

        }

        public string TotpCodeFormattedStart => TotpCodeFormatted?.Split(' ')[0];
        
        public string TotpCodeFormattedEnd => TotpCodeFormatted?.Split(' ')[1];

        public async Task CopyToClipboardAsync()
        {
            await _clipboardService.CopyTextAsync(TotpCodeFormatted);
            _platformUtilsService.ShowToast("info", null, string.Format(AppResources.ValueHasBeenCopied, AppResources.VerificationCodeTotp));
        }

        public async Task TotpTickAsync()
        {
            var epoc = CoreHelpers.EpocUtcNow() / 1000;
            var mod = epoc % interval;
            var totpSec = interval - mod;
            TotpSec = totpSec.ToString();
            Progress = totpSec * 100 / 30;
            //TotpLow = totpSec < 7;
            if (mod == 0)
            {
                await TotpUpdateCodeAsync();
            }

        }

        public async Task TotpUpdateCodeAsync()
        {
            _totpCode = await _totpService.GetCodeAsync(Cipher.Login.Totp);
            if (_totpCode != null)
            {
                if (_totpCode.Length > 4)
                {
                    var half = (int)Math.Floor(_totpCode.Length / 2M);
                    TotpCodeFormatted = string.Format("{0} {1}", _totpCode.Substring(0, half),
                        _totpCode.Substring(half));
                }
                else
                {
                    TotpCodeFormatted = _totpCode;
                }
            }
            else
            {
                TotpCodeFormatted = null;
            }
        }
    }
}
