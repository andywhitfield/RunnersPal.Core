using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RunLog;

[Authorize]
public class ListModel(IUserAccountRepository userAccountRepository,
    IUserService userService,
    IPaceService paceService,
    IRunLogRepository runLogRepository)
    : PageModel
{
    private UserAccount? _userAccount;

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public string Date { get; set; } = "";
    public IEnumerable<Models.RunLog> Activities { get; set; } = [];
    public Pagination Pagination { get; private set; } = Pagination.Empty;

    public async Task OnGet()
    {
        var userAccount = await GetUserAccountAsync();
        var activities = await runLogRepository.GetAllLogRunsAsync(userAccount).OrderByDescending(r => r.Date).ThenBy(r => r.CreatedDate).ToListAsync();
        var runLogs = Pagination.Paginate(activities, PageNumber);
        Activities = runLogs.Items;
        Pagination = new(runLogs.Page, runLogs.PageCount);
        Date = (Activities.FirstOrDefault()?.Date ?? DateTime.Today).ToString("yyyy-MM-dd");
    }

    private async Task<UserAccount> GetUserAccountAsync() => _userAccount ??= await userAccountRepository.GetUserAccountAsync(User);

    public async Task<string> RunLogTitleAsync(Models.RunLog runLog) => $"{userService.ToUserDistance(runLog.Route.Distance, await GetUserAccountAsync())} in {paceService.TimeTakenDisplayFormat(runLog.TimeTaken)}";
    public async Task<string> RunLogPaceAsync(Models.RunLog runLog) => paceService.CalculatePace(await GetUserAccountAsync(), runLog);
}
