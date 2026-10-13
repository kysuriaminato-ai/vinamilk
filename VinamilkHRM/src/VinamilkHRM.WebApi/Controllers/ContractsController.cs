using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Contracts;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/contracts")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractManagementService _contractService;

    public ContractsController(IContractManagementService contractService)
    {
        _contractService = contractService;
    }

    /// <summary>
    /// Tạo hợp đồng lao động mới kèm kiểm soát giới hạn ký hợp đồng xác định thời hạn (BLLĐ 2019 Điều 20).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HR,HRAdmin,Admin")]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractDto dto, CancellationToken ct)
    {
        try
        {
            var contract = await _contractService.CreateContractAsync(dto, ct);
            return CreatedAtAction(nameof(GetContractDocument), new { id = contract.Id }, new
            {
                success = true,
                contractId = contract.Id,
                contractNumber = contract.ContractNumber,
                contractType = contract.ContractType.ToString(),
                message = "Khởi tạo hợp đồng lao động thành công."
            });
        }
        catch (DomainException ex) when (ex.ErrorCode == "ERR_CONTRACT_THIRD_DEFINITE")
        {
            return UnprocessableEntity(new
            {
                errorCode = ex.ErrorCode,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Kiểm tra và quét các hợp đồng lao động sắp hết hạn tại các mốc 60, 30, 7 ngày.
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Roles = "HR,HRAdmin,Admin,Manager")]
    public async Task<IActionResult> GetExpiringContracts([FromQuery] DateTime? checkDate, CancellationToken ct)
    {
        DateTime targetDate = checkDate ?? DateTime.Today;
        var notices = await _contractService.CheckAndNotifyExpiringContractsAsync(targetDate, ct);
        return Ok(new
        {
            success = true,
            checkDate = targetDate.ToString("yyyy-MM-dd"),
            totalExpiring = notices.Count,
            data = notices
        });
    }

    /// <summary>
    /// Xuất tài liệu hợp đồng định dạng DOCX chuẩn OpenXML với các trường trộn tự động.
    /// </summary>
    [HttpGet("{id:int}/document")]
    public async Task<IActionResult> GetContractDocument(int id, CancellationToken ct)
    {
        byte[] docBytes = await _contractService.GenerateContractDocxAsync(id, ct);
        return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"HopDongLaoDong_{id}.docx");
    }

    /// <summary>
    /// Gửi hợp đồng lao động qua cổng ký số điện tử (VNPT SmartCA / Viettel CA).
    /// </summary>
    [HttpPost("{id:int}/esign")]
    [Authorize(Roles = "HR,HRAdmin,Admin")]
    public async Task<IActionResult> SendForESign(int id, CancellationToken ct)
    {
        var result = await _contractService.SendContractForESignAsync(id, ct);
        return Ok(new
        {
            success = true,
            transactionId = result.TransactionId,
            signingUrl = result.SigningUrl,
            status = result.Status,
            message = "Đã gửi hợp đồng lên hệ thống ký số điện tử."
        });
    }
}