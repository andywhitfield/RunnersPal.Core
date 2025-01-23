using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Pages.User;

[Authorize]
public class ProfileModel(IUserAccountRepository userAccountRepository)
    : PageModel
{
    [BindProperty] public DistanceUnits Units { get; set; }

    public async Task OnGet()
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        Units = (DistanceUnits)userAccount.DistanceUnits;
    }

    public async Task<IActionResult> OnPost()
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        userAccount.DistanceUnits = (int)Units;
        await userAccountRepository.UpdateAsync(userAccount);
        return Redirect("/user/profile");
    }
}
