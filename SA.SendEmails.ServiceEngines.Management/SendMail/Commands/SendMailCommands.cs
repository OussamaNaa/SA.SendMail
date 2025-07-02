using MailKit.Net.Smtp;
using MediatR;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SA.SendEmails.ServiceEngines.Management.SendMail.Responses;
using System;
using System.Reflection;

namespace SA.SendEmails.ServiceEngines.Management.SendMail.Commands
{
    public class SendMailCommands : BaseRequest<SendMailResponse>
    {
        #region Properties
        public List<string> Emails { get; set; }
        public string From { get; set; }
        public string? DisplayNAme { get; set; }
        public string Subject { get; set; }
        public string body { get; set; }
        public bool? IsBodyHtml { get; set; }
        public IEnumerable<ElectronicMailAttachment>? ElectronicMailAttachments { get; set; }

        #endregion Properties
    }


    public class SendMailCommandsHandler : IRequestHandler<SendMailCommands, SendMailResponse>
    {
        #region Fields

        private readonly IElectronicMailSender electronicMailSender;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        #endregion Fields

        #region Constructors

        public SendMailCommandsHandler(IElectronicMailSender electronicMailSender, ILogger logger, IConfiguration configuration)
        {
            this.electronicMailSender = electronicMailSender;
            this.logger = logger;
            this.configuration = configuration;
        }

        #endregion Constructors

        #region Methods

        public async Task<SendMailResponse> Handle(SendMailCommands request, CancellationToken cancellationToken)
        {
            return await ExecutionHelper.Proceed(async () =>
            {
                #region Declarations

                SendMailResponse response = new SendMailResponse();

                #endregion Declarations

                #region Validations

                if (request.IsNotValid())
                {
                    response.IsSuccess = false;
                    response.WarningMessage = "Error";

                    return response;
                }

                #endregion Validations

                #region Operations

                if (response.IsSuccess)
                {

                    IEnumerable<ElectronicMailAttachment> electronicMailAttachments = new List<ElectronicMailAttachment>();
                    bool mail = false;
                    if (request.Emails.IsNotNull())
                    {
                        try
                        {
                            foreach (var item in request.Emails)
                            {
                               bool test =  FunctionSendMail("", request.From, request.DisplayNAme, item, request.Subject, request.IsBodyHtml ?? false, request.body);
                                mail = electronicMailSender.Send(item, request.DisplayNAme, request.Subject, request.body, request.IsBodyHtml ?? false, request.ElectronicMailAttachments);
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            logger.Fatal("Send error message : " + e.Message);
                        }
                    }

                    response.isSended = mail;

                    response.IsSuccess = true;
                    response.IsPopulated = mail;
                    response.InformationMessage = "Operation succes";
                }

                #endregion Operations

                return response;
            }, MethodBase.GetCurrentMethod().ReflectedType.FullName, Assembly.GetExecutingAssembly().FullName, Guid.NewGuid().ToString(), request.CallerId);
        }


        public bool FunctionSendMail(string FromDisplayName, string From, string ToDisplayName, string To, string Subject, bool isBodyHtml, string Body)
        {
            try
            {
                using MimeMessage mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(FromDisplayName, From));
                mimeMessage.To.Add(new MailboxAddress(ToDisplayName, To));
                mimeMessage.Subject = Subject;
                BodyBuilder bodyBuilder = new BodyBuilder();
                if (isBodyHtml)
                {
                    bodyBuilder.HtmlBody = Body;
                }
                else
                {
                    bodyBuilder.TextBody = Body;
                }

                //if (!electronicMailAttachments.IsNullOrEmpty())
                //{
                //    foreach (ElectronicMailAttachment electronicMailAttachment in electronicMailAttachments)
                //    {
                //        bodyBuilder.Attachments.Add(electronicMailAttachment.Name, electronicMailAttachment.Content);
                //    }
                //}

                mimeMessage.Body = bodyBuilder.ToMessageBody();
                using SmtpClient smtpClient = new SmtpClient();
                smtpClient.Connect(configuration["SmtpClient:Host"], int.Parse(configuration["SmtpClient:Port"]));
                if (bool.Parse(configuration["SmtpClient:IsAuthenticationEnabled"]))
                {
                    if (bool.Parse(configuration["SmtpClient:IsEncryptionEnabled"]))
                    {
                        smtpClient.Authenticate(configuration["SmtpClient:UserName"].Decrypt("24Wf7S9h$6iRB"), configuration["SmtpClient:Password"].Decrypt("24Wf7S9h$6iRB"));
                    }
                    else
                    {
                        smtpClient.Authenticate(configuration["SmtpClient:UserName"], configuration["SmtpClient:Password"]);
                    }
                }

                smtpClient.DeliveryStatusNotificationType = DeliveryStatusNotificationType.Full;
                if (!bool.Parse(configuration["SmtpClient:IsAuthenticationEnabled"]) || smtpClient.IsAuthenticated)
                {
                    int num = 1;
                    int num2 = int.Parse(configuration["SmtpClient:MaximumSendAttempts"]);
                    while (num <= num2)
                    {
                        try
                        {
                            num++;
                            smtpClient.Send(mimeMessage);
                            smtpClient.Disconnect(quit: true);
                            return true;
                        }
                        catch
                        {
                            Thread.Sleep(int.Parse(configuration["SmtpClient:NextSendAttemptSleepIntervalByMilliseconds"]));
                        }
                    }
                }

                smtpClient.Disconnect(quit: true);
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion Methods
    }
}
