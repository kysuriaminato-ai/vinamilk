using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Web.Controllers;

public sealed class RbacMatrixViewModel
{
    public IReadOnlyList<Role> Roles { get; init; } = Array.Empty<Role>();
    public IReadOnlyList<Permission> Permissions { get; init; } = Array.Empty<Permission>();
    public HashSet<string> GrantedPairs { get; init; } = new();
    public IReadOnlyList<User> Users { get; init; } = Array.Empty<User>();
    public IReadOnlyList<OrganizationUnit> Units { get; init; } = Array.Empty<OrganizationUnit>();
}

public sealed class PiiSampleDisplayItem
{
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public string CipherCitizenId { get; init; } = string.Empty;
    public string MaskedCitizenId { get; init; } = string.Empty;
    public string DecryptedCitizenId { get; init; } = string.Empty;
    public string CipherBankAccount { get; init; } = string.Empty;
    public string DecryptedBankAccount { get; init; } = string.Empty;
}

[Route("Security")]
[Authorize(Roles = "SUPER_ADMIN")]
public class SecurityController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;
    private readonly ICurrentUserService _currentUser;

    public SecurityController(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _encryptionService = encryptionService;
        _currentUser = currentUser;
    }

    /* ========================================================================
     * 1. MA TRẬN PHÂN QUYỀN ROLE × PERMISSION & GÁN ROLE / DATA SCOPE CHO USER
     * ======================================================================== */
    [HttpGet("")]
    [HttpGet("RbacMatrix")]
    public async Task<IActionResult> RbacMatrix(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Ma trận Phân quyền RBAC (Role × Permission) & Data Scope";
        ViewData["BreadcrumbParent"] = "Hệ thống & Bảo mật";

        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.PermissionCode)
            .ToListAsync(cancellationToken);

        var rolePerms = await _context.RolePermissions
            .AsNoTracking()
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken);

        var grantedSet = rolePerms
            .Select(rp => $"{rp.RoleId}_{rp.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Employee)
                .ThenInclude(e => e!.OrganizationUnit)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.DataScopes)
                .ThenInclude(ds => ds.OrganizationUnit)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.UnitLevel)
            .ThenBy(u => u.UnitCode)
            .ToListAsync(cancellationToken);

        var vm = new RbacMatrixViewModel
        {
            Roles = roles,
            Permissions = permissions,
            GrantedPairs = grantedSet,
            Users = users,
            Units = units
        };

        return View(vm);
    }

    [HttpPost("SaveRbacMatrix")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRbacMatrix(
        [FromForm] List<string>? grantedPairs,
        CancellationToken cancellationToken)
    {
        grantedPairs ??= new List<string>();
        var selectedSet = grantedPairs.ToHashSet(StringComparer.Ordinal);

        var superAdminRole = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleCode == "SUPER_ADMIN", cancellationToken);
        var allPermissions = await _context.Permissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var existing = await _context.RolePermissions.ToListAsync(cancellationToken);
        var beforeCount = existing.Count;

        // Xóa các quyền bị bỏ chọn (ngoại trừ SUPER_ADMIN luôn giữ đủ 62 quyền)
        var toRemove = existing
            .Where(rp =>
                (superAdminRole == null || rp.RoleId != superAdminRole.Id) &&
                !selectedSet.Contains($"{rp.RoleId}_{rp.PermissionId}"))
            .ToList();

        foreach (var rem in toRemove)
        {
            _context.RolePermissions.Remove(rem);
        }

        // Thêm các quyền mới được tích chọn
        var existingPairs = existing
            .Select(rp => $"{rp.RoleId}_{rp.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

        var addedCount = 0;
        foreach (var pair in selectedSet)
        {
            if (existingPairs.Contains(pair))
                continue;

            var parts = pair.Split('_');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var roleId) &&
                int.TryParse(parts[1], out var permId))
            {
                await _context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permId
                }, cancellationToken);
                addedCount++;
            }
        }

        // Ghi nhận tổng hợp thay đổi Ma trận RBAC vào AuditLog
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = _currentUser.UserId,
            Username = _currentUser.Username,
            ModuleName = "Identity",
            ActionType = "RbacMatrixUpdated",
            EntityName = "RolePermissionMatrix",
            EntityId = "ALL_ROLES",
            OldValuesJson = JsonSerializer.Serialize(new { TotalRolePermissions = beforeCount }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                TotalRolePermissions = beforeCount - toRemove.Count + addedCount,
                AddedPermissions = addedCount,
                RemovedPermissions = toRemove.Count
            }),
            AffectedColumns = "RoleId,PermissionId",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            UserAgent = Request.Headers["User-Agent"].ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["SecuritySuccess"] = $"Đã cập nhật Ma trận Phân quyền RBAC thành công (Thêm mới: {addedCount}, Thu hồi: {toRemove.Count} quyền) và ghi AuditLog!";
        return RedirectToAction(nameof(RbacMatrix));
    }

    [HttpPost("UpdateUserAccess")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserAccess(
        int userId,
        int roleId,
        string defaultDataScope,
        int? scopedUnitId,
        bool mustChangePassword,
        bool unlockAccount,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.DataScopes)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (user is null || role is null)
            return RedirectToAction(nameof(RbacMatrix));

        var oldRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var oldScope = role.DefaultDataScope;

        // Cập nhật Role cho User
        foreach (var ur in user.UserRoles.ToList())
        {
            _context.UserRoles.Remove(ur);
        }
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        // Cập nhật DefaultDataScope của Role (All / UnitTree / Department / SelfOnly)
        if (!string.IsNullOrWhiteSpace(defaultDataScope))
        {
            role.DefaultDataScope = defaultDataScope;
        }

        // Cập nhật UserDataScope theo đơn vị được chỉ định
        foreach (var ds in user.DataScopes.ToList())
        {
            _context.UserDataScopes.Remove(ds);
        }
        if (scopedUnitId.HasValue && scopedUnitId.Value > 0)
        {
            user.DataScopes.Add(new UserDataScope
            {
                UserId = user.Id,
                OrganizationUnitId = scopedUnitId.Value,
                IncludeSubUnits = defaultDataScope == "UnitTree"
            });
        }

        user.MustChangePassword = mustChangePassword;
        if (unlockAccount)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = _currentUser.UserId,
            Username = _currentUser.Username,
            ModuleName = "Identity",
            ActionType = "UserAccessUpdated",
            EntityName = "User",
            EntityId = user.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new
            {
                Username = user.Username,
                RoleIds = oldRoleIds,
                DataScope = oldScope
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Username = user.Username,
                NewRoleId = role.Id,
                NewRoleCode = role.RoleCode,
                NewDataScope = role.DefaultDataScope,
                ScopedUnitId = scopedUnitId,
                MustChangePassword = mustChangePassword,
                Unlocked = unlockAccount
            }),
            AffectedColumns = "RoleId,DefaultDataScope,MustChangePassword,LockoutEnd",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            UserAgent = Request.Headers["User-Agent"].ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        TempData["SecuritySuccess"] = $"Đã cập nhật Vai trò [{role.RoleName}] và Phạm vi dữ liệu [{role.DefaultDataScope}] cho tài khoản '{user.Username}'.";
        return RedirectToAction(nameof(RbacMatrix));
    }

    /* ========================================================================
     * 2. QUẢN LÝ PHIÊN ĐĂNG NHẬP (USERSESSIONS) & MÃ HÓA CỘT NHẠY CẢM (AES-256)
     * ======================================================================== */
    [HttpGet("Sessions")]
    public async Task<IActionResult> Sessions(bool decryptSample = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Quản lý Phiên Đăng nhập (Sessions) & Mã hóa Dữ liệu Nhạy cảm (PII)";
        ViewData["BreadcrumbParent"] = "Hệ thống & Bảo mật";

        var sessions = await _context.UserSessions
            .AsNoTracking()
            .Include(s => s.User)
                .ThenInclude(u => u.Employee)
            .OrderByDescending(s => s.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        // Nếu chưa có phiên nào trong DB, tạo 2 phiên mẫu để hiển thị trực quan
        if (sessions.Count == 0)
        {
            var adminUser = await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync(cancellationToken);
            if (adminUser is not null)
            {
                var seedSessions = new List<UserSession>
                {
                    new()
                    {
                        UserId = adminUser.Id,
                        RefreshTokenHash = "SESSION_TOKEN_DESKTOP_CHROME_WIN11",
                        IpAddress = "10.128.14.25",
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/130.0.0.0 Safari/537.36",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-25),
                        ExpiresAt = DateTime.UtcNow.AddDays(14),
                        CreatedBy = adminUser.Username
                    },
                    new()
                    {
                        UserId = adminUser.Id,
                        RefreshTokenHash = "SESSION_TOKEN_MOBILE_SAFARI_IOS",
                        IpAddress = "113.161.72.108",
                        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_6 like Mac OS X) Mobile/15E148 Safari/604.1",
                        CreatedAt = DateTime.UtcNow.AddHours(-4),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        CreatedBy = adminUser.Username
                    }
                };
                await _context.UserSessions.AddRangeAsync(seedSessions, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                sessions = await _context.UserSessions
                    .AsNoTracking()
                    .Include(s => s.User)
                        .ThenInclude(u => u.Employee)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(25)
                    .ToListAsync(cancellationToken);
            }
        }

        // Lấy 5 nhân viên tiêu biểu để minh họa Mã hóa cột nhạy cảm (AES-256-GCM)
        var sampleEmps = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .OrderBy(e => e.EmployeeCode)
            .Take(5)
            .ToListAsync(cancellationToken);

        var piiItems = sampleEmps.Select(e =>
        {
            var plainCccd = _encryptionService.Decrypt(e.CitizenIdEncrypted);
            var plainBank = _encryptionService.Decrypt(e.BankAccountEncrypted);
            var maskedCccd = plainCccd.Length >= 4 ? $"••••••••{plainCccd[^4..]}" : "••••••••••••";
            return new PiiSampleDisplayItem
            {
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                UnitName = e.OrganizationUnit.UnitName,
                CipherCitizenId = e.CitizenIdEncrypted.Length > 28 ? e.CitizenIdEncrypted[..28] + "..." : e.CitizenIdEncrypted,
                MaskedCitizenId = maskedCccd,
                DecryptedCitizenId = decryptSample ? plainCccd : "•••••••••••• (Nhấn 'Giải mã có Kiểm toán')",
                CipherBankAccount = (e.BankAccountEncrypted ?? "").Length > 24 ? e.BankAccountEncrypted![..24] + "..." : (e.BankAccountEncrypted ?? ""),
                DecryptedBankAccount = decryptSample ? $"{plainBank} ({e.BankName})" : "••••••••••"
            };
        }).ToList();

        if (decryptSample)
        {
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username,
                ModuleName = "CoreHR",
                ActionType = "PiiDecrypted",
                EntityName = "EmployeeSensitiveColumns",
                EntityId = "VNM-0001..VNM-0005",
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    Reason = "Kiểm tra giải mã cột nhạy cảm CCCD & Số tài khoản ngân hàng (AES-256-GCM)",
                    RecordCount = piiItems.Count
                }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        ViewBag.PiiItems = piiItems;
        ViewBag.DecryptSample = decryptSample;
        return View(sessions);
    }

    [HttpPost("RevokeSession")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession(long sessionId, CancellationToken cancellationToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is not null && !session.RevokedAt.HasValue)
        {
            session.RevokedAt = DateTime.UtcNow;

            await _context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUser.UserId,
                Username = _currentUser.Username,
                ModuleName = "Identity",
                ActionType = "SessionRevoked",
                EntityName = "UserSession",
                EntityId = session.Id.ToString(),
                OldValuesJson = JsonSerializer.Serialize(new { SessionId = session.Id, Status = "Active", Ip = session.IpAddress }),
                NewValuesJson = JsonSerializer.Serialize(new { SessionId = session.Id, Status = "Revoked", RevokedAtUtc = session.RevokedAt }),
                AffectedColumns = "RevokedAt",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            TempData["SecuritySuccess"] = $"Đã thu hồi và đăng xuất thiết bị Session #{session.Id} (IP: {session.IpAddress}).";
        }

        return RedirectToAction(nameof(Sessions));
    }

    /* ========================================================================
     * 3. TRA CỨU & LỌC NHẬT KÝ KIỂM TOÁN (AUDITLOG) + JSON DIFF VIEWER
     * ======================================================================== */
    [HttpGet("AuditLogs")]
    public async Task<IActionResult> AuditLogs(
        string? keyword = null,
        string? moduleName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Nhật ký Kiểm toán (AuditLog) & Truy vết Thay đổi Dữ liệu (JSON Diff)";
        ViewData["BreadcrumbParent"] = "Hệ thống & Bảo mật";

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(a =>
                a.Username.Contains(kw) ||
                a.EntityName.Contains(kw) ||
                a.EntityId.Contains(kw) ||
                (a.IpAddress != null && a.IpAddress.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            query = query.Where(a => a.ModuleName == moduleName);
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.ActionType == actionType);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(60)
            .ToListAsync(cancellationToken);

        // Nếu bảng AuditLogs còn ít bản ghi, tạo sẵn 1 bản ghi mẫu có đầy đủ OldValuesJson & NewValuesJson
        if (logs.Count == 0 && string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(moduleName) && string.IsNullOrWhiteSpace(actionType))
        {
            var initialAudit = new AuditLog
            {
                UserId = 1,
                Username = "huyen.ntt",
                ModuleName = "CoreHR",
                ActionType = "Update",
                EntityName = "Employee",
                EntityId = "VNM-0001",
                OldValuesJson = JsonSerializer.Serialize(new
                {
                    EmployeeCode = "VNM-0001",
                    BaseSalary = 35000000,
                    PositionAllowance = 10000000,
                    WorkStatus = "Official"
                }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    EmployeeCode = "VNM-0001",
                    BaseSalary = 38500000,
                    PositionAllowance = 12000000,
                    WorkStatus = "Official"
                }),
                AffectedColumns = "BaseSalary,PositionAllowance",
                IpAddress = "10.128.14.25",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/130.0.0.0",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                CreatedBy = "huyen.ntt"
            };
            await _context.AuditLogs.AddAsync(initialAudit, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            logs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .Take(60)
                .ToListAsync(cancellationToken);
        }

        ViewBag.Keyword = keyword ?? string.Empty;
        ViewBag.ModuleName = moduleName ?? string.Empty;
        ViewBag.ActionType = actionType ?? string.Empty;
        return View(logs);
    }

    [HttpPost("CreateSampleAuditDiff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSampleAuditDiff(CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .OrderBy(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (emp is not null)
        {
            // Thay đổi thực tế trên Entity để AuditableEntityInterceptor tự động bắt OldValuesJson & NewValuesJson
            emp.OtherAllowance += 500_000m;
            await _context.SaveChangesAsync(cancellationToken);

            TempData["SecuritySuccess"] =
                $"Đã thực hiện thay đổi thực tế trên Nhân sự {emp.EmployeeCode} ({emp.FullName}): Phụ cấp khác tăng thêm +500.000 VNĐ. Interceptor đã tự động ghi JSON Diff vào bảng system.AuditLogs!";
        }

        return RedirectToAction(nameof(AuditLogs));
    }
}
