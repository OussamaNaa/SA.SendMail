using MediatR;
using Microsoft.AspNetCore.Mvc;
using SA.SendEmails.ServiceEngines.Management.SendMail.Commands;
using SA.SendEmails.ServiceEngines.Management.SendMail.Responses;

namespace SA.SendEmails.Services.LateralService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SendMailController : ControllerBase
    {
        #region Fields

        private readonly IMediator mediator;

        #endregion Fields

        #region Constructors

        public SendMailController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        #endregion Constructors

        #region Methods

        [HttpPost]
        [Route(nameof(SendMail))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<SendMailResponse>> SendMail([FromForm] SendMailCommands command)
        {
            return  await this.mediator.Send(command);
        }

        #endregion Methods
    }
}
