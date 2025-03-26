using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using MaxSys.Models;


namespace MaxSys.Interface
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(Emai_Template_STMP model);
    }
}
