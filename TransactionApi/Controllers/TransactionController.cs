using Microsoft.AspNetCore.Mvc;
using TransactionApi.Models;
using TransactionApi.Services;

namespace TransactionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _transactionService;

        public TransactionController()
        {
            _transactionService = new TransactionService();
        }

        [HttpPost("submittrxmessage")]
        public IActionResult SubmitTransactionMessage([FromBody] TransactionRequest request)
        {
            var response = _transactionService.ValidateAndProcessTransaction(request);
            return Ok(response);
        }
    }
}
