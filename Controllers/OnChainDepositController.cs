using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wihngo.Services.Interfaces;

namespace Wihngo.Controllers;

[ApiController]
[Route("api/deposits/onchain")]
[Authorize]
public class OnChainDepositController : ControllerBase
{
    private readonly IOnChainDepositService _depositService;
    private readonly ILogger<OnChainDepositController> _logger;

    public OnChainDepositController(
        IOnChainDepositService depositService,
        ILogger<OnChainDepositController> logger)
    {
        _depositService = depositService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's on-chain deposit history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepositHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetUserId();
            var deposits = await _depositService.GetUserDepositsAsync(userId, page, pageSize);

            return Ok(new
            {
                deposits = deposits.Select(d => new
                {
                    d.Id,
                    d.Chain,
                    d.Token,
                    d.AmountDecimal,
                    d.Status,
                    d.Confirmations,
                    d.TxHashOrSig,
                    d.FromAddress,
                    d.DetectedAt,
                    d.CreditedAt,
                    d.CreatedAt
                }),
                page,
                pageSize,
                total = deposits.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deposit history for user");
            return StatusCode(500, new { error = "Failed to get deposit history" });
        }
    }

    /// <summary>
    /// Get specific deposit by ID
    /// </summary>
    [HttpGet("{depositId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeposit(Guid depositId)
    {
        try
        {
            var userId = GetUserId();
            var deposits = await _depositService.GetUserDepositsAsync(userId, 1, 1000);
            var deposit = deposits.FirstOrDefault(d => d.Id == depositId);

            if (deposit == null)
            {
                return NotFound(new { error = "Deposit not found" });
            }

            return Ok(new
            {
                deposit.Id,
                deposit.Chain,
                deposit.Token,
                deposit.TokenId,
                deposit.AmountDecimal,
                deposit.RawAmount,
                deposit.Decimals,
                deposit.Status,
                deposit.Confirmations,
                deposit.TxHashOrSig,
                deposit.BlockNumberOrSlot,
                deposit.FromAddress,
                deposit.ToAddress,
                deposit.AddressOrAccount,
                deposit.Memo,
                deposit.DetectedAt,
                deposit.CreditedAt,
                deposit.CreatedAt,
                deposit.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deposit {DepositId}", depositId);
            return StatusCode(500, new { error = "Failed to get deposit" });
        }
    }

    /// <summary>
    /// Get user's derived address for a specific chain
    /// </summary>
    [HttpGet("address/{chain}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAddress(string chain)
    {
        try
        {
            var userId = GetUserId();
            var address = await _depositService.GetUserDerivedAddressAsync(userId, chain.ToLower());

            if (string.IsNullOrEmpty(address))
            {
                return NotFound(new { error = $"No address configured for chain: {chain}" });
            }

            return Ok(new
            {
                chain = chain.ToLower(),
                address
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user address for chain {Chain}", chain);
            return StatusCode(500, new { error = "Failed to get user address" });
        }
    }

    /// <summary>
    /// Register a user's derived address for a specific chain
    /// </summary>
    [HttpPost("address/register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAddress([FromBody] RegisterAddressDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _depositService.RegisterUserAddressAsync(
                userId, 
                dto.Chain.ToLower(), 
                dto.Address, 
                dto.DerivationPath);

            if (!result)
            {
                return BadRequest(new { error = "Failed to register address" });
            }

            return Ok(new
            {
                message = "Address registered successfully",
                chain = dto.Chain.ToLower(),
                address = dto.Address
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user address");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get supported token configurations
    /// </summary>
    [HttpGet("tokens")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupportedTokens()
    {
        try
        {
            var configs = await _depositService.GetActiveTokenConfigurationsAsync();

            return Ok(new
            {
                tokens = configs.Select(c => new
                {
                    c.Token,
                    c.Chain,
                    c.TokenAddress,
                    c.Issuer,
                    c.Decimals,
                    c.RequiredConfirmations,
                    c.DerivationPath
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported tokens");
            return StatusCode(500, new { error = "Failed to get supported tokens" });
        }
    }

    /// <summary>
    /// Get token configuration for specific token and chain
    /// </summary>
    [HttpGet("tokens/{token}/{chain}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTokenConfiguration(string token, string chain)
    {
        try
        {
            var config = await _depositService.GetTokenConfigurationAsync(token.ToUpper(), chain.ToLower());

            if (config == null)
            {
                return NotFound(new { error = $"Token configuration not found for {token} on {chain}" });
            }

            return Ok(new
            {
                config.Token,
                config.Chain,
                config.TokenAddress,
                config.Issuer,
                config.Decimals,
                config.RequiredConfirmations,
                config.DerivationPath,
                config.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token configuration for {Token} on {Chain}", token, chain);
            return StatusCode(500, new { error = "Failed to get token configuration" });
        }
    }

    /// <summary>
    /// Get pending deposits for current user
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingDeposits()
    {
        try
        {
            var userId = GetUserId();
            var allDeposits = await _depositService.GetUserDepositsAsync(userId, 1, 1000);
            var pendingDeposits = allDeposits.Where(d => d.Status == "pending" || d.Status == "confirmed").ToList();

            return Ok(new
            {
                deposits = pendingDeposits.Select(d => new
                {
                    d.Id,
                    d.Chain,
                    d.Token,
                    d.AmountDecimal,
                    d.Status,
                    d.Confirmations,
                    d.TxHashOrSig,
                    d.DetectedAt
                }),
                count = pendingDeposits.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending deposits");
            return StatusCode(500, new { error = "Failed to get pending deposits" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return Guid.Parse(userIdClaim);
    }
}

public class RegisterAddressDto
{
    public string Chain { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string DerivationPath { get; set; } = string.Empty;
}
