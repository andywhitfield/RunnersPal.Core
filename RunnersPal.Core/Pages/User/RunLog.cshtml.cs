using System.Text;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Pages.ViewModels;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.User;

[Authorize]
public class RunLogModel(
    ILogger<RunLogModel> logger,
    IUserAccountRepository userAccountRepository,
    IRunLogRepository runLogRepository,
    IUserService userService,
    IPaceService paceService)
    : PageModel
{
    public async Task<FileResult> OnGet()
    {
        logger.LogInformation("Downloading all run events");
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);

        using MemoryStream stream = new();
        using (StreamWriter writer = new(stream, Encoding.UTF8))
        using (CsvWriter csv = CsvWriter.Create(writer))
        {
            csv.WriteHeader<DownloadRunLogEventViewModel>();
            await foreach (var runLog in runLogRepository.GetAllLogRunsAsync(userAccount, null).OrderBy(x => x.Date).ThenBy(x => x.CreatedDate))
            {
                await csv.WriteRecordAsync(new DownloadRunLogEventViewModel
                {
                    Date = runLog.Date.ToString("yyyy-MM-dd"),
                    Distance = userService.ToUserDistanceUnits(runLog.Route.Distance, userAccount).ToString("0.####"),
                    Time = (paceService.TimeTaken(runLog.TimeTaken) ?? TimeSpan.Zero).ToString("hh\\:mm\\:ss"),
                    Pace = (paceService.CalculatePaceAsTimeSpan(userAccount, runLog) ?? TimeSpan.Zero).ToString("mm\\:ss"),
                    RouteName = runLog.Route.Name,
                    Comment = runLog.Comment ?? ""
                });
            }
        }

        return File(stream.ToArray(), "text/csv", "runlogevents.csv");
    }
}