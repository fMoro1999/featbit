using Application.Bases.Models;
using Domain.AuditLogs;
using Domain.SemanticPatch;

namespace Application.AuditLogs;

public class GetAuditLogList : IRequest<PagedResult<AuditLogVm>>
{
    public Guid EnvId { get; set; }

    public AuditLogFilter Filter { get; set; }
}

public class GetAuditLogListHandler : IRequestHandler<GetAuditLogList, PagedResult<AuditLogVm>>
{
    private readonly IAuditLogService _auditLogService;
    private readonly IUserService _userService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IMapper _mapper;

    public GetAuditLogListHandler(
        IAuditLogService auditLogService,
        IUserService userService,
        IAccessTokenService accessTokenService,
        IMapper mapper)
    {
        _auditLogService = auditLogService;
        _userService = userService;
        _accessTokenService = accessTokenService;
        _mapper = mapper;
    }

    public async Task<PagedResult<AuditLogVm>> Handle(GetAuditLogList request, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetListAsync(request.EnvId, request.Filter);
        var logVms = _mapper.Map<PagedResult<AuditLogVm>>(logs);

        var users = await _userService.GetListAsync(logVms.Items.Select(x => x.CreatorId));
        foreach (var item in logVms.Items)
        {
            item.Instructions = item.RefType switch
            {
                AuditLogRefTypes.FeatureFlag => FlagComparer.Compare(item.DataChange),
                AuditLogRefTypes.Segment => SegmentComparer.Compare(item.DataChange),
                _ => Array.Empty<Instruction>()
            };

            var user = users.FirstOrDefault(x => x.Id == item.CreatorId);
            if (user != null)
            {
                item.CreatorName = user.Name;
                item.CreatorEmail = user.Email;
                continue;
            }

            // An audit log may also be created by an access token
            var accessToken = await _accessTokenService.GetAsync(item.CreatorId);
            if (accessToken != null)
            {
                item.CreatorName = accessToken.Name;
                item.CreatorEmail = "Access token";
                continue;
            }

            // If the creator id is empty, it means that the audit log is created by the system
            if (item.CreatorId == Guid.Empty)
            {
                item.CreatorName = "System";
            }
        }

        return logVms;
    }
}